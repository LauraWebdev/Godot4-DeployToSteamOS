using System.Collections.Generic;
using Godot;

[Tool]
public partial class AddDeviceWindow : Window
{
	private bool _isUpdatingDevices;
	private float _scanCooldown;
	private List<SteamOSDevkitManager.Device> _scannedDevices = new();
	
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
		}

		_refreshButton.Disabled = _isUpdatingDevices;
	}

	private async void UpdateDevices()
	{
		if (!Visible || _isUpdatingDevices) return;

		_isUpdatingDevices = true;
		
		var devices = await SteamOSDevkitManager.ScanDevices();
		GD.Print($"Scanned {devices.Count} devices in network.");

		if (devices != _scannedDevices)
		{
			_scannedDevices = devices;
			// TODO: Update UI

			// Clear List
			foreach (var childNode in _devicesContainer.GetChildren())
			{
				childNode.QueueFree();
			}
			
			foreach (var scannedDevice in _scannedDevices)
			{
				var deviceItem = _deviceItemPrefab.Instantiate<DeviceItemPrefab>();
				deviceItem.SetUI(scannedDevice);
				deviceItem.OnDevicePair += device =>
				{
					GD.Print($"Pairing with {device.DisplayName} ({device.Login}@{device.IPAdress})");
				};
				_devicesContainer.AddChild(deviceItem);
			}
		}
		
		_isUpdatingDevices = false;
	}

	public void Close()
	{
		// TODO: Notify DeployDock to Update the Dropdown
		Hide();
	}
}
