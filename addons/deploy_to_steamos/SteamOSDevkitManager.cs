using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Godot;
using Newtonsoft.Json;
using Renci.SshNet;
using Renci.SshNet.Common;
using Zeroconf;

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
        GD.Print("[ScanDevices] Starting");
        
        var networkDevices = await ZeroconfResolver.ResolveAsync(SteamOSProtocol);
        List<Device> devices = new();
        
        GD.Print($"[ScanDevices] Found {networkDevices.Count} devices in network.");
        
        // Iterate through all network devices and request further connection info from the service
        foreach (var networkDevice in networkDevices)
        {
            GD.Print($"[ScanDevices] Device: {networkDevice.DisplayName}@{networkDevice.IPAddress}");

            var device = new Device
            {
                DisplayName = networkDevice.DisplayName,
                IPAdress = networkDevice.IPAddress,
                ServiceName = networkDevice.DisplayName + "." + SteamOSProtocol,
            };

            IService serviceData;
            var hasServiceData = networkDevice.Services.TryGetValue(device.ServiceName, out serviceData);
            
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
    /// <returns>The SSH CLI output</returns>
    public static async Task<string> RunSSHCommand(Device device, string command)
    {
        GD.Print($"[RunSSHCommand] Connecting to {device.Login}@{device.IPAdress}");
        using var client = new SshClient(GetSSHConnectionInfo(device));
        await client.ConnectAsync(CancellationToken.None);
        
        GD.Print($"[RunSSHCommand] Command: '{command}'");
        var sshCommand = client.CreateCommand(command);
        var result = await Task.Factory.FromAsync(sshCommand.BeginExecute(), sshCommand.EndExecute);

        client.Disconnect();

        return result;
    }

    public static async Task CopyFiles(Device device, string localPath, string remotePath)
    {
        GD.Print($"[CopyFiles] Connecting to {device.Login}@{device.IPAdress}");
        using var client = new ScpClient(GetSSHConnectionInfo(device));
        await client.ConnectAsync(CancellationToken.None);
        
        GD.Print("[CopyFiles] Uploading files...");
        
        // Run async method until upload is done
        // TODO: Cleanup Progress
        var taskCompletion = new TaskCompletionSource<bool>();
        client.Uploading += (sender, e) =>
        {
            var progressPercentage = (double)e.Uploaded / e.Size * 100;
            GD.Print($"[CopyFiles] Uploading {e.Filename}, progress: {progressPercentage}%");
            if (e.Uploaded == e.Size)
            {
                taskCompletion.TrySetResult(true);
            }
        };
        
        await Task.Run(() => client.Upload(new DirectoryInfo(localPath), remotePath));
        await taskCompletion.Task;
        client.Disconnect();
        
        GD.Print("[CopyFiles] Fixing file permissions");
        await RunSSHCommand(device, $"chmod +x -R {remotePath}");
    }

    /// <summary>
    /// Runs an SSH command on the device that runs the steamos-prepare-upload script
    /// </summary>
    /// <param name="device">A SteamOS devkit device</param>
    /// <param name="gameId">An ID for the game</param>
    /// <returns>The CLI result</returns>
    public static async Task<PrepareUploadResult> PrepareUpload(Device device, string gameId)
    {
        GD.Print("[PrepareUpload] Preparing Upload");
        
        var resultRaw = await RunSSHCommand(device, "python3 ~/devkit-utils/steamos-prepare-upload --gameid " + gameId);
        var result = JsonConvert.DeserializeObject<PrepareUploadResult>(resultRaw);

        return result;
    }

    
    public static async Task<CreateShortcutResult> CreateShortcut(Device device, CreateShortcutParameters parameters)
    {
        GD.Print("[CreateShortcut] Creating Shortcut");
        
        // TODO: python3 ~/devkit-utils/steam-client-create-shortcut --parms '{"gameid": "test1", "directory": "/home/deck/devkit-game/test1", "argv": ["demo1.x86_64"], "settings": {"steam_play": "0"}}'
        var parametersJson = JsonConvert.SerializeObject(parameters);
        var command = $"python3 ~/devkit-utils/steam-client-create-shortcut --parms '{parametersJson}'";
        
        var resultRaw = await RunSSHCommand(device, command);
        var result = JsonConvert.DeserializeObject<CreateShortcutResult>(resultRaw);

        return result;
    }

    /// <summary>
    /// A SteamOS devkit device
    /// </summary>
    public class Device
    {
        public string DisplayName;
        public string IPAdress;
        public int Port;
        public string ServiceName;
        public string Settings;
        public string Login;
        public string Devkit1;
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
        public string gameid;
        public string directory;
        public string[] argv;
        public Dictionary<string, string> settings;
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
                // TODO: Test for Windows
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