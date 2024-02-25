using System.Collections.Generic;
using System.Text.Json;
using Godot;

namespace Laura.DeployToSteamOS;

[Tool]
public partial class SettingsManager : Node
{
	public static SettingsManager Instance;

	private const string FolderPath = "res://.deploy_to_steamos";
	private const string SettingsPath = FolderPath + "/settings.json";
	private const string DevicesPath = FolderPath + "/devices.json";

	public bool IsDirty = false;
	public SettingsFile Settings = new();
	public List<SteamOSDevkitManager.Device> Devices = new();

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
			IsDirty = true;
		}
		else
		{
			using var settingsFile = FileAccess.Open(SettingsPath, FileAccess.ModeFlags.Read);
			var settingsFileContent = settingsFile.GetAsText();

			Settings = JsonSerializer.Deserialize<SettingsFile>(settingsFileContent);
			
			// Failsafe if settings file is corrupt
			if (Settings == null)
			{
				Settings = new SettingsFile();
				IsDirty = true;
			}
		}
		if (!FileAccess.FileExists(SettingsPath))
		{
			Devices = new List<SteamOSDevkitManager.Device>();
			IsDirty = true;
		}
		else
		{
			using var devicesFile = FileAccess.Open(DevicesPath, FileAccess.ModeFlags.Read);
			var devicesFileContent = devicesFile.GetAsText();

			Devices = JsonSerializer.Deserialize<List<SteamOSDevkitManager.Device>>(devicesFileContent);
			
			// Failsafe if device file is corrupt
			if (Devices == null)
			{
				Devices = new List<SteamOSDevkitManager.Device>();
				IsDirty = true;
			}
		}

		if (IsDirty)
		{
			Save();
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
		var jsonSettings = JsonSerializer.Serialize(Settings);
		using var settingsFile = FileAccess.Open(SettingsPath, FileAccess.ModeFlags.Write);
		settingsFile.StoreString(jsonSettings);
		
		// Save Devices
		var jsonDevices = JsonSerializer.Serialize(Devices);
		using var devicesFile = FileAccess.Open(DevicesPath, FileAccess.ModeFlags.Write);
		devicesFile.StoreString(jsonDevices);

		IsDirty = false;
	}
}