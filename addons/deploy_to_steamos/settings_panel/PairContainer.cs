using System;
using Godot;
using Zeroconf;

[Tool]
public partial class PairContainer : VBoxContainer
{
	[ExportGroup("Scan Pairing")]
	[Export] private VBoxContainer _scanDeviceParent;
	[Export] private PackedScene _scanDeviceItemPrefab;
	
	[ExportGroup("Manual Pairing")]
	[Export] private LineEdit _manualPairingInputIp;
	[Export] private LineEdit _manualPairingInputPort;
	
	public async void OnScanPressed()
	{
		// TODO: Clear Devices
		
		GD.Print("Scanning Devices");

		var devices = await SteamOSDevkitManager.ScanDevices();
		GD.Print($"Scanned {devices.Count} devices in network.");

		GD.Print("Running PrepareUpload");
		var result2 = await SteamOSDevkitManager.PrepareUpload(devices[0], "test2");
		GD.Print(result2);

		GD.Print("Running Test ls");
		var result = await SteamOSDevkitManager.RunSSHCommand(devices[0], "ls /home/deck/devkit-game");
		GD.Print(result);

		// TODO: Update UI

		// TODO: Add Pair Button with IP/Port
	}

	public void OnManualPairPressed()
	{
		GD.Print("Manual Pairing to Device");
		
		// TODO: Connect to Device
	}
}
