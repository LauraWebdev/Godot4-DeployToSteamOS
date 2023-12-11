using System.Collections.Generic;
using System.Linq;
using Godot;

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
		
		var devices = await SteamOSDevkitManager.ScanDevices();
		GD.Print($"Scanned {devices.Count} devices in network.");

		// DEBUG
		GD.Print("Prepare for Debug");
		var device = devices.FirstOrDefault();
		var deleteResult = await SteamOSDevkitManager.RunSSHCommand(device, "rm -rf /home/deck/devkit-game/*");
		GD.Print(deleteResult);
		// /DEBUG

		var uploadResult = await SteamOSDevkitManager.PrepareUpload(device, "test5");
		GD.Print(uploadResult.Directory);
		
		await SteamOSDevkitManager.CopyFiles(device, @"C:\Users\hello\Development\Godot4-DeployToSteamOS-Builds\", uploadResult.Directory);

		var createShortcutParameters = new SteamOSDevkitManager.CreateShortcutParameters
		{
			gameid = "test5",
			directory = uploadResult.Directory,
			argv = new[] { "game.x86_64" },
			settings = new Dictionary<string, string>
			{
				{ "steam_play", "0" }
			},
		};
		var createShortcutResult = await SteamOSDevkitManager.CreateShortcut(device, createShortcutParameters);
		GD.Print(createShortcutResult.Success);
		GD.Print(createShortcutResult.Error);
		
		GD.Print("Done.");

		// TODO: Update UI

		// TODO: Add Pair Button with IP/Port
	}

	public void OnManualPairPressed()
	{
		GD.Print("Manual Pairing to Device");
		
		// TODO: Connect to Device
	}
}
