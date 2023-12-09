using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Godot;
using Renci.SshNet;
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
        GD.Print("[RunSSHCommand] Starting");
        
        GD.Print($"[RunSSHCommand] Connecting to {device.Login}@{device.IPAdress}");
        using var client = new SshClient(GetSSHConnectionInfo(device));
        await client.ConnectAsync(CancellationToken.None);
        
        GD.Print($"[RunSSHCommand] Command: '{command}'");
        var sshCommand = client.CreateCommand(command);
        var result = await Task.Factory.FromAsync(sshCommand.BeginExecute(), sshCommand.EndExecute);

        client.Disconnect();

        return result;
    }

    /// <summary>
    /// Runs an SSH command on the device that runs the steamos-prepare-upload script
    /// </summary>
    /// <param name="device">A SteamOS devkit device</param>
    /// <param name="gameId">An ID for the game</param>
    /// <returns>The CLI result</returns>
    public static async Task<string> PrepareUpload(Device device, string gameId)
    {
        GD.Print("[PrepareUpload] Starting");

        var command = "python3 ~/devkit-utils/steamos-prepare-upload --gameid ";
        command += gameId + " ";
        command += "{\"user\": \"" + device.Login + "\", \"directory\": \"/home/" + device.Login + "/devkit-game/" + gameId + "\"}";

        // TODO: This doesn't seem to work
        // TODO: python3 ~/devkit-utils/steamos-prepare-upload --gameid test1 {"user": "deck", "directory": "/home/deck/devkit-game/test1"}
        var result = await RunSSHCommand(device, command);

        GD.Print(result);

        return result;
    }

    public static async void Upload(Device device, string path)
    {
        GD.Print("[Upload] Starting");
        
        // TODO: RSync files
    }

    
    public static async void CreateShortcut(Device device, CreateShortcutParameters parameters)
    {
        GD.Print("[CreateShortcut] Starting");
        
        // TODO: python3 ~/devkit-utils/steam-client-create-shortcut --parms '{"gameid": "test1", "directory": "/home/deck/devkit-game/test1", "argv": ["demo1.x86_64"], "settings": {"steam_play": "0"}}'
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

    /// <summary>
    /// Parameters for the CreateShortcut method
    /// </summary>
    public struct CreateShortcutParameters
    {
        public string GameId;
        public string Path;
        public string[] Arguments;
        public Dictionary<string, string> Settings;
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
                applicationDataPath = System.Environment.GetFolderPath(System.Environment.SpecialFolder.ApplicationData);
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
        GD.Print(privateKeyPath);
        if (!File.Exists(privateKeyPath)) throw new Exception("devkit_rsa key is missing. Have you connected to your device via the official devkit UI yet?");
        
        var privateKeyFile = new PrivateKeyFile(privateKeyPath);
        return new ConnectionInfo(device.IPAdress, device.Login, new PrivateKeyAuthenticationMethod(device.Login, privateKeyFile));
    }
}