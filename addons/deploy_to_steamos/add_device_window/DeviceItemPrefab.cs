using Godot;

namespace Laura.DeployToSteamOS;

[Tool]
public partial class DeviceItemPrefab : PanelContainer
{
	private SteamOSDevkitManager.Device _device;

	public delegate void DeviceDelegate(SteamOSDevkitManager.Device device);
	public event DeviceDelegate OnDevicePair;
	public event DeviceDelegate OnDeviceUnpair;
	
	[ExportGroup("References")]
	[Export] private Label _deviceNameLabel;
	[Export] private Label _deviceConnectionLabel;
	[Export] private Button _devicePairButton;
	[Export] private Button _deviceUnpairButton;

	public void SetUI(SteamOSDevkitManager.Device device)
	{
		_device = device;
		_deviceNameLabel.Text = device.DisplayName;
		_deviceConnectionLabel.Text = $"{device.Login}@{device.IPAdress}";

		if (SettingsManager.Instance.Devices.Exists(x => x.IPAdress == device.IPAdress && x.Login == device.Login))
		{
			_devicePairButton.Visible = false;
			_deviceUnpairButton.Visible = true;
		}
	}

	public void Pair()
	{
		OnDevicePair?.Invoke(_device);
	}

	public void Unpair()
	{
		OnDeviceUnpair?.Invoke(_device);
	}
}
