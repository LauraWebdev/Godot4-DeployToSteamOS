using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Godot;
using Renci.SshNet;
using Zeroconf;

namespace Laura.DeployToSteamOS;

/// <summary>
/// Scans the network for SteamOS Devkit devices via the ZeroConf / Bonjour protocol
/// </summary>
public class SteamOSDevkitManager
{
    public const string SteamOSProtocol = "_steamos-devkit._tcp.local.";
    public const string CompatibleTextVersion = "1";
    
    /// <summary>
    /// Scans the network for valid SteamOS devkit devices
    /// </summary>
    /// <returns>A list of valid SteamOS devkit devices</returns>
    public static async Task<List<Device>> ScanDevices()
    {
        var networkDevices = await ZeroconfResolver.ResolveAsync(SteamOSProtocol);
        List<Device> devices = new();
        
        // Iterate through all network devices and request further connection info from the service
        foreach (var networkDevice in networkDevices)
        {
            var device = new Device
            {
                DisplayName = networkDevice.DisplayName,
                IPAdress = networkDevice.IPAddress,
                ServiceName = networkDevice.DisplayName + "." + SteamOSProtocol,
            };

            var hasServiceData = networkDevice.Services.TryGetValue(device.ServiceName, out var serviceData);
            
            // This device is not a proper SteamOS device
            if(!hasServiceData) continue;

            var properties = serviceData.Properties.FirstOrDefault();
            if (properties == null) continue;

            // Device is not compatible (version mismatch)
            // String"1" is for some reason not equal to String"1"
            //if (properties["txtvers"] != CompatibleTextVersion) continue;

            device.Settings = properties["settings"];
            device.Login = properties["login"];
            device.Devkit1 = properties["devkit1"];
            
            devices.Add(device);
        }

        return devices;
    }

    /// <summary>
    /// Creates an SSH connection and runs a command
    /// </summary>
    /// <param name="device">A SteamOS devkit device</param>
    /// <param name="command">The SSH command to run</param>
    /// <param name="logCallable">A callable for logging</param>
    /// <returns>The SSH CLI output</returns>
    public static async Task<string> RunSSHCommand(Device device, string command, Callable logCallable)
    {
        logCallable.CallDeferred($"Connecting to {device.Login}@{device.IPAdress}");
        using var client = new SshClient(GetSSHConnectionInfo(device));
        await client.ConnectAsync(CancellationToken.None);
        
        logCallable.CallDeferred($"Command: '{command}'");
        var sshCommand = client.CreateCommand(command);
        var result = await Task.Factory.FromAsync(sshCommand.BeginExecute(), sshCommand.EndExecute);

        client.Disconnect();

        return result;
    }

    /// <summary>
    /// Creates a new SCP connection and copies all local files to a remote path
    /// </summary>
    /// <param name="device">A SteamOS devkit device</param>
    /// <param name="localPath">The path on the host</param>
    /// <param name="remotePath">The path on the device</param>
    /// <param name="logCallable">A callable for logging</param>
    public static async Task CopyFiles(Device device, string localPath, string remotePath, Callable logCallable)
    {
        logCallable.CallDeferred($"Connecting to {device.Login}@{device.IPAdress}");
        using var client = new ScpClient(GetSSHConnectionInfo(device));
        await client.ConnectAsync(CancellationToken.None);
        
        logCallable.CallDeferred($"Uploading files");
        
        // Run async method until upload is done
        // TODO: Set Progress based on files
        var lastUploadedFilename = "";
        var uploadProgress = 0;
        var taskCompletion = new TaskCompletionSource<bool>();
        client.Uploading += (sender, e) =>
        {
            if (e.Filename != lastUploadedFilename)
            {
                lastUploadedFilename = e.Filename;
                uploadProgress = 0;
            }
            var progressPercentage = Mathf.CeilToInt((double)e.Uploaded / e.Size * 100);

            if (progressPercentage != uploadProgress)
            {
                uploadProgress = progressPercentage;
                logCallable.CallDeferred($"Uploading {lastUploadedFilename} ({progressPercentage}%)");
            }

            if (e.Uploaded == e.Size)
            {
                taskCompletion.TrySetResult(true);
            }
        };
        client.ErrorOccurred += (sender, args) => throw new Exception("Error while uploading build.");
        
        await Task.Run(() => client.Upload(new DirectoryInfo(localPath), remotePath));
        await taskCompletion.Task;
        client.Disconnect();
        
        logCallable.CallDeferred($"Fixing file permissions");
        await RunSSHCommand(device, $"chmod +x -R {remotePath}", logCallable);
    }

    /// <summary>
    /// Runs an SSH command on the device that runs the steamos-prepare-upload script
    /// </summary>
    /// <param name="device">A SteamOS devkit device</param>
    /// <param name="gameId">An ID for the game</param>
    /// <param name="logCallable">A callable for logging</param>
    /// <returns>The CLI result</returns>
    public static async Task<PrepareUploadResult> PrepareUpload(Device device, string gameId, Callable logCallable)
    {
        logCallable.CallDeferred("Preparing upload");
        
        var resultRaw = await RunSSHCommand(device, "python3 ~/devkit-utils/steamos-prepare-upload --gameid " + gameId, logCallable);
        var result = JsonSerializer.Deserialize<PrepareUploadResult>(resultRaw);

        return result;
    }
    
    /// <summary>
    /// Runs an SSH command on the device that runs the steamos-create-shortcut script
    /// </summary>
    /// <param name="device">A SteamOS devkit device</param>
    /// <param name="parameters">Parameters for the shortcut</param>
    /// <param name="logCallable">A callable for logging</param>
    /// <returns>The CLI result</returns>
    public static async Task<CreateShortcutResult> CreateShortcut(Device device, CreateShortcutParameters parameters, Callable logCallable)
    {
        var parametersJson = JsonSerializer.Serialize(parameters);
        var command = $"python3 ~/devkit-utils/steam-client-create-shortcut --parms '{parametersJson}'";
        
        var resultRaw = await RunSSHCommand(device, command, logCallable);
        var result = JsonSerializer.Deserialize<CreateShortcutResult>(resultRaw);

        return result;
    }

    /// <summary>
    /// A SteamOS devkit device
    /// </summary>
    public class Device
    {
        public string DisplayName { get; set; }
        public string IPAdress { get; set; }
        public int Port { get; set; }
        public string ServiceName { get; set; }
        public string Settings { get; set; }
        public string Login { get; set; }
        public string Devkit1 { get; set; }
    }

    public class PrepareUploadResult
    {
        public string User { get; set; }
        public string Directory { get; set; }
    }

    public class CreateShortcutResult
    {
        public string Error { get; set; }
        public string Success { get; set; }
    }

    /// <summary>
    /// Parameters for the CreateShortcut method
    /// </summary>
    public struct CreateShortcutParameters
    {
        public string gameid { get; set; }
        public string directory { get; set; }
        public string[] argv { get; set; }
        public Dictionary<string, string> settings { get; set; }
    }

    /// <summary>
    /// Returns the path to the devkit_rsa key generated by the official SteamOS devkit client
    /// </summary>
    /// <returns>The path to the "devkit_rsa" key</returns>
    public static string GetPrivateKeyPath()
    {
        string applicationDataPath;
        switch (System.Environment.OSVersion.Platform)
        {
            // TODO: Linux Support
            case PlatformID.Win32NT:
                applicationDataPath = Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData), "steamos-devkit");
                break;
            case PlatformID.Unix:
                applicationDataPath = Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.Personal), "Library", "Application Support"); 
                break;
            default:
                applicationDataPath = "";
                break;
        }
        
        var keyFolder = Path.Combine(applicationDataPath, "steamos-devkit");
        return Path.Combine(keyFolder, "devkit_rsa");
    }

    /// <summary>
    /// Creates a SSH Connection Info for a SteamOS devkit device
    /// </summary>
    /// <param name="device">A SteamOS devkit device</param>
    /// <returns>An SSH ConnectionInfo</returns>
    /// <exception cref="Exception">Throws if there is no private key present</exception>
    public static ConnectionInfo GetSSHConnectionInfo(Device device)
    {
        var privateKeyPath = GetPrivateKeyPath();
        if (!File.Exists(privateKeyPath)) throw new Exception("devkit_rsa key is missing. Have you connected to your device via the official devkit UI yet?");
        
        var privateKeyFile = new PrivateKeyFile(privateKeyPath);
        return new ConnectionInfo(device.IPAdress, device.Login, new PrivateKeyAuthenticationMethod(device.Login, privateKeyFile));
    }
}