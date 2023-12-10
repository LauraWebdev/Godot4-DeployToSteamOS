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
		
		GD.Print("Scanning Devices");

		var devices = await SteamOSDevkitManager.ScanDevices();
		GD.Print($"Scanned {devices.Count} devices in network.");

		// DEBUG
		GD.Print("Prepare for Debug");
		var device = devices.FirstOrDefault(x => x.DisplayName == "taradeck");
		var deleteResult = await SteamOSDevkitManager.RunSSHCommand(device, "rm -rf /home/deck/devkit-game/*");
		GD.Print(deleteResult);
		// /DEBUG

		GD.Print("Running PrepareUpload");
		var uploadResult = await SteamOSDevkitManager.PrepareUpload(device, "test3");
		GD.Print(uploadResult);

		var lsResult = await SteamOSDevkitManager.RunSSHCommand(device, "ls /home/deck/devkit-game");
		GD.Print(lsResult);
		
		GD.Print("Copying folder");
		await SteamOSDevkitManager.CopyFiles(device, "/Users/laura/Development/godot4-deploytosteamos_tests/test1", uploadResult.Directory);

		var lsGameResult = await SteamOSDevkitManager.RunSSHCommand(device, "ls " + uploadResult.Directory);
		GD.Print(lsGameResult);

		var createShortcutParameters = new SteamOSDevkitManager.CreateShortcutParameters
		{
			gameid = "test3",
			directory = uploadResult.Directory,
			argv = new[] { "demo1.x86_64" },
			settings = new Dictionary<string, string>
			{
				{ "steam_play", "0" }
			},
		};
		var createShortcutResult = await SteamOSDevkitManager.CreateShortcut(device, createShortcutParameters);
		GD.Print(createShortcutResult);

		// TODO: Update UI

		// TODO: Add Pair Button with IP/Port
	}

	public void OnManualPairPressed()
	{
		GD.Print("Manual Pairing to Device");
		
		// TODO: Connect to Device
	}
}
