using Godot;

namespace Laura.DeployToSteamOS;

[Tool]
public partial class DeviceItemPrefab : PanelContainer
{
	private SteamOSDevkitManager.Device _device;

	public delegate void DevicePairDelegate(SteamOSDevkitManager.Device device);
	public event DevicePairDelegate OnDevicePair;
	
	[ExportGroup("References")]
	[Export] private Label _deviceNameLabel;

	public void SetUI(SteamOSDevkitManager.Device device)
	{
		_device = device;
		_deviceNameLabel.Text = $"{device.DisplayName} ({device.Login}@{device.IPAdress})";
	}

	public void Pair()
	{
		OnDevicePair?.Invoke(_device);
	}
}
