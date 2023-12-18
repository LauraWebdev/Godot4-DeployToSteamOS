using System.Collections.Generic;
using Godot;

namespace Laura.DeployToSteamOS;

[Tool]
public partial class AddDeviceWindow : Window
{
	private bool _isVisible;
	private bool _isUpdatingDevices;
	private float _scanCooldown;
	private List<SteamOSDevkitManager.Device> _scannedDevices = new();
	private List<SteamOSDevkitManager.Device> _pairedDevices = new();

	public delegate void DevicesChangedDelegate(List<SteamOSDevkitManager.Device> devices);
	public event DevicesChangedDelegate OnDevicesChanged;
	
	[ExportGroup("References")]
	[Export] private VBoxContainer _devicesContainer;
	[Export] private PackedScene _deviceItemPrefab;
	[Export] private Button _refreshButton;

	public override void _Process(double delta)
	{
		if (!Visible) return;
		
		_scanCooldown -= (float)delta;
		if (_scanCooldown < 0f)
		{
			_scanCooldown = 10f;
			UpdateDevices();
			UpdateDeviceList();
		}

		_refreshButton.Disabled = _isUpdatingDevices;
	}

	private async void UpdateDevices()
	{
		if (!Visible || _isUpdatingDevices) return;

		_isUpdatingDevices = true;
		
		var devices = await SteamOSDevkitManager.ScanDevices();
		if (devices != _scannedDevices)
		{
			foreach (var device in devices)
			{
				if (
					_scannedDevices.Exists(x => x.IPAdress == device.IPAdress && x.Login == device.Login)
					|| _pairedDevices.Exists(x => x.IPAdress == device.IPAdress && x.Login == device.Login)
				) continue;
				
				_scannedDevices.Add(device);
			}
			
			UpdateDeviceList();
		}
		
		_isUpdatingDevices = false;
	}

	public void UpdateDeviceList()
	{
		// Clear List
		foreach (var childNode in _devicesContainer.GetChildren())
		{
			childNode.QueueFree();
		}

		var devices = new List<SteamOSDevkitManager.Device>();
		devices.AddRange(_pairedDevices);
		foreach (var scannedDevice in _scannedDevices)
		{
			if (devices.Exists(x => x.IPAdress == scannedDevice.IPAdress && x.Login == scannedDevice.Login))
			{
				continue;
			}
			
			devices.Add(scannedDevice);
		}
		
		foreach (var scannedDevice in devices)
		{
			var deviceItem = _deviceItemPrefab.Instantiate<DeviceItemPrefab>();
			deviceItem.SetUI(scannedDevice);
			deviceItem.OnDevicePair += device =>
			{
				// TODO: Connect to device and run a random ssh command to check communication
				_pairedDevices.Add(device);
				UpdateDeviceList();
			};
			deviceItem.OnDeviceUnpair += device =>
			{
				_pairedDevices.Remove(device);
				UpdateDeviceList();
			};
			_devicesContainer.AddChild(deviceItem);
		}
	}

	public void Close()
	{
		Hide();

		if (_pairedDevices != SettingsManager.Instance.Devices)
		{
			OnDevicesChanged?.Invoke(_pairedDevices);
		}
	}

	public void OnVisibilityChanged()
	{
		if (Visible)
		{
			_isVisible = true;

			// Prepopulate device list with saved devices
			_pairedDevices = new List<SteamOSDevkitManager.Device>(SettingsManager.Instance.Devices);
			UpdateDeviceList();
		}
		else
		{
			_isVisible = false;
		}
	}
}
