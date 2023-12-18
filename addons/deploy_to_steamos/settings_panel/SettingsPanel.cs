using Godot;

namespace Laura.DeployToSteamOS;

[Tool]
public partial class SettingsPanel : PanelContainer
{
	[ExportGroup("References")]
	[Export] private Label _versionLabel;
	[Export] private AddDeviceWindow _addDeviceWindow;
	[Export] private LineEdit _buildPathLineEdit;
	[Export] private FileDialog _buildPathFileDialog;
	[Export] private LineEdit _startParametersLineEdit;
	[Export] private OptionButton _uploadMethodOptionButton;
	[Export] private Label _uploadMethodDifferentialHintLabel;
	[Export] private Label _uploadMethodIncrementalHintLabel;
	[Export] private Label _uploadMethodCleanReplaceHintLabel;

	private float _saveCooldown = 2f;

	public override void _Process(double delta)
	{
		if (SettingsManager.Instance.IsDirty && _saveCooldown < 0f)
		{
			_saveCooldown = 2f;
			SettingsManager.Instance.Save();
		}

		if (SettingsManager.Instance.IsDirty && Visible)
		{
			_saveCooldown -= (float)delta;
		}
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

	private void ShowPanel()
	{
		_buildPathLineEdit.Text = SettingsManager.Instance.Settings.BuildPath;
		_startParametersLineEdit.Text = SettingsManager.Instance.Settings.StartParameters;
		_uploadMethodOptionButton.Selected = (int)SettingsManager.Instance.Settings.UploadMethod;

		_saveCooldown = 2f;
	}

	private void HidePanel()
	{
	}

	public void BuildPathTextChanged(string newBuildPath)
	{
		SettingsManager.Instance.Settings.BuildPath = newBuildPath;
		_buildPathLineEdit.Text = newBuildPath;
		
		_saveCooldown = 2f;
		SettingsManager.Instance.IsDirty = true;
	}
	
	public void BuildPathOpenFileDialog()
	{
		_buildPathFileDialog.Show();
	}
	
	public void StartParametersTextChanged(string newStartParameters)
	{
		SettingsManager.Instance.Settings.StartParameters = newStartParameters;
		_startParametersLineEdit.Text = newStartParameters;
		
		_saveCooldown = 2f;
		SettingsManager.Instance.IsDirty = true;
	}
	
	public void UploadMethodItemSelected(int newItemIndex)
	{
		var newUploadMethod = (SettingsFile.UploadMethods)newItemIndex;
		SettingsManager.Instance.Settings.UploadMethod = newUploadMethod;
		_uploadMethodOptionButton.Selected = newItemIndex;
		
		_saveCooldown = 2f;
		SettingsManager.Instance.IsDirty = true;

		_uploadMethodDifferentialHintLabel.Visible = newUploadMethod == SettingsFile.UploadMethods.Differential;
		_uploadMethodIncrementalHintLabel.Visible = newUploadMethod == SettingsFile.UploadMethods.Incremental;
		_uploadMethodCleanReplaceHintLabel.Visible = newUploadMethod == SettingsFile.UploadMethods.CleanReplace;
	}

	public void PairDevices()
	{
		_addDeviceWindow.Show();
	}
}
