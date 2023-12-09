using Godot;

[Tool]
public partial class SettingsPanel : PanelContainer
{
	public override void _Process(double delta)
	{
	}

	public void OnVisibilityChanged()
	{
		if (Visible)
		{
			ShowPanel();
		}
		else
		{
			HidePanel();
		}
	}

	void ShowPanel()
	{
		//GD.Print("Panel is now Visible");
		
		// TODO: See if there is a paired Device
		// TODO: Setup Pairing / Settings
	}

	void HidePanel()
	{
		//GD.Print("Panel is now Hidden");
	}
}
