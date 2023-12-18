using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using Newtonsoft.Json;

namespace Laura.DeployToSteamOS;

[Tool]
public partial class DeployDock : PanelContainer
{
    private int _selectedId = 10000;
    
    [Export] private OptionButton _deployTargetButton;
    [Export] private Button _deployButton;
    [Export] private AddDeviceWindow _addDeviceWindow;
    [Export] private DeployWindow _deployWindow;

    public override void _EnterTree()
    {
        _addDeviceWindow.OnDevicesChanged += OnDevicesChanged;
    }

    public override void _ExitTree()
    {
        _addDeviceWindow.OnDevicesChanged -= OnDevicesChanged;
    }

    public override void _Ready()
    {
        UpdateDropdown();
    }

    public void OnDeployTargetItemSelected(int index)
    {
        var itemId = _deployTargetButton.GetItemId(index);
        if (_selectedId == itemId) return;
        
        if (itemId <= 10000)
        {
            _selectedId = itemId;
        }
        else if (itemId == 10001)
        {
            _addDeviceWindow.Show();
        }

        _deployTargetButton.Select(_deployTargetButton.GetItemIndex(_selectedId));
        _deployButton.Disabled = _selectedId >= 10000;
    }

    public async void Deploy()
    {
        var device = SettingsManager.Instance.Devices.ElementAtOrDefault(_selectedId);
        if (device == null)
        {
            GD.PrintErr("[DeployToSteamOS] Unknown deploy target.");
            return;
        }
        
        // TODO: Detect Export Presets and stop if no Linux preset named "Steamdeck" exists
        
        _deployWindow.Deploy(device);
    }

    private void OnDevicesChanged(List<SteamOSDevkitManager.Device> devices)
    {
        // Adding Devices
        var devicesToAdd = devices.Where(device => !SettingsManager.Instance.Devices.Exists(x => x.IPAdress == device.IPAdress && x.Login == device.Login)).ToList();
        foreach (var device in devicesToAdd)
        {
            SettingsManager.Instance.Devices.Add(device);
        }
        
        // Removing Devices
        var devicesToRemove = SettingsManager.Instance.Devices.Where(savedDevice => !devices.Exists(x => x.IPAdress == savedDevice.IPAdress && x.Login == savedDevice.Login)).ToList();
        foreach (var savedDevice in devicesToRemove)
        {
            SettingsManager.Instance.Devices.Remove(savedDevice);
        }
        
        SettingsManager.Instance.Save();
        UpdateDropdown();
    }

    private async void UpdateDropdown()
    {
        // Hack to prevent console error
        // Godot apparently calls this before the settings are loaded
        while (SettingsManager.Instance == null)
        {
            await Task.Delay(10);
        }
        
        _deployTargetButton.Clear();
        _deployTargetButton.AddItem("None", 10000);
        
        for (var index = 0; index < SettingsManager.Instance.Devices.Count; index++)
        {
            var savedDevice = SettingsManager.Instance.Devices.ElementAtOrDefault(index);
            if(savedDevice == null) continue;
            
            _deployTargetButton.AddItem($"{savedDevice.DisplayName} ({savedDevice.Login}@{savedDevice.IPAdress})", index);
        }

        _deployTargetButton.AddSeparator();
        _deployTargetButton.AddItem("Add devkit device", 10001);
    }
}
