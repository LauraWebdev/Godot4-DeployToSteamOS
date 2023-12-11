using Godot;
using System;

[Tool]
public partial class DeployDock : PanelContainer
{
    private int _selectedId = 0;
    
    [Export] private OptionButton _deployTargetButton;
    [Export] private Button _deployButton;
    [Export] private Window _addDeviceWindow;

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

    public void Deploy()
    {
        GD.Print("Deploying Project to: " + _selectedId);
    }
}
