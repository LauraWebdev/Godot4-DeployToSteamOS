using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using Zeroconf;

/// <summary>
/// Scans the network for SteamOS Devkit devices via the ZeroConf / Bonjour protocol
/// </summary>
public class ScanManager
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
        
        GD.Print($"Found {networkDevices.Count} devices in network.");
        
        foreach (var networkDevice in networkDevices)
        {
            GD.Print($"Device: {networkDevice.DisplayName}@{networkDevice.IPAddress}");

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
            
            GD.Print($"-> ServiceName: {device.ServiceName}");
            GD.Print($"-> Settings: {device.Settings}");
            GD.Print($"-> Login: {device.Login}");
            GD.Print($"-> Devkit1: {device.Devkit1}");
            
            devices.Add(device);
        }

        return devices;
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
}