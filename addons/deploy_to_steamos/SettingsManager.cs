using System.Collections.Generic;
using Godot;
using Newtonsoft.Json;

namespace Laura.DeployToSteamOS;

[Tool]
public partial class SettingsManager : Node
{
	public static SettingsManager Instance;

	private const string FolderPath = "res://.deploy_to_steamos";
	private const string SettingsPath = FolderPath + "/settings.json";
	private const string DevicesPath = FolderPath + "/devices.json";

	public bool IsDirty = false;
	public SettingsFile Settings;
	public List<SteamOSDevkitManager.Device> Devices;

	public override void _EnterTree()
	{
		Instance = this;
		
		Load();
	}

	public void Load()
	{
		if (!FileAccess.FileExists(SettingsPath))
		{
			Settings = new SettingsFile();
			Save();
		}
		else
		{
			using var settingsFile = FileAccess.Open(SettingsPath, FileAccess.ModeFlags.Read);
			var settingsFileContent = settingsFile.GetAsText();

			Settings = JsonConvert.DeserializeObject<SettingsFile>(settingsFileContent);
		}
		if (!FileAccess.FileExists(SettingsPath))
		{
			Devices = new List<SteamOSDevkitManager.Device>();
			Save();
		}
		else
		{
			using var devicesFile = FileAccess.Open(DevicesPath, FileAccess.ModeFlags.Read);
			var devicesFileContent = devicesFile.GetAsText();

			Devices = JsonConvert.DeserializeObject<List<SteamOSDevkitManager.Device>>(devicesFileContent);
		}
	}

	public void Save()
	{
		// Check if directory exists and create if it doesn't
		var dirAccess = DirAccess.Open(FolderPath);
		if (dirAccess == null)
		{
			DirAccess.MakeDirRecursiveAbsolute(FolderPath);
		}
		
		// Save Settings
		var jsonSettings = JsonConvert.SerializeObject(Settings);
		using var settingsFile = FileAccess.Open(SettingsPath, FileAccess.ModeFlags.Write);
		settingsFile.StoreString(jsonSettings);
		
		// Save Devices
		var jsonDevices = JsonConvert.SerializeObject(Devices);
		using var devicesFile = FileAccess.Open(DevicesPath, FileAccess.ModeFlags.Write);
		devicesFile.StoreString(jsonDevices);

		IsDirty = false;
	}
}