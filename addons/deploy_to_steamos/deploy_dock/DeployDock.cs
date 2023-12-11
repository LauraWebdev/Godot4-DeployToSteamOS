using Godot;
using System;
using System.Linq;

[Tool]
public partial class DeployDock : PanelContainer
{
    private int _selectedId = 0;
    
    [Export] private OptionButton _deployTargetButton;
    [Export] private Button _deployButton;
    [Export] private AddDeviceWindow _addDeviceWindow;
    [Export] private DeployWindow _deployWindow;

    public void OnDeployTargetItemSelected(int index)
    {
        var itemId = _deployTargetButton.GetItemId(index);
        if (_selectedId == itemId) return;
        
        if (itemId != 10000)
        {
            _selectedId = itemId;
        }
        else
        {
            _addDeviceWindow.Show();
        }

        _deployTargetButton.Select(_deployTargetButton.GetItemIndex(_selectedId));
        _deployButton.Disabled = _selectedId is < 1 or > 9998;
    }

    public async void Deploy()
    {
        GD.Print("Deploying Project to: " + _selectedId);

        // DEBUG
        var devices = await SteamOSDevkitManager.ScanDevices();
        _deployWindow.Deploy(devices.FirstOrDefault());
        // /DEBUG

        // TODO: Connect to DeployWindow.Deploy(device)
    }
}
