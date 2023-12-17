using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Godot;

namespace Laura.DeployToSteamOS;

public partial class DeployWindow
{
    private async Task DeployInit()
    {
        CurrentStep = DeployStep.Init;
        CurrentProgress = StepProgress.Running;
        UpdateUI();

        await Task.Delay(0);
        
        _gameId = ProjectSettings.GetSetting("application/config/name", "game").AsString();
        _gameId = Regex.Replace(_gameId, @"[^a-zA-Z0-9]", string.Empty);

        // Add current timestamp to gameid for incremental builds
        if (SettingsManager.Instance.Settings.UploadMethod == SettingsFile.UploadMethods.Incremental)
        {
            _gameId += "_" + DateTimeOffset.Now.ToUnixTimeSeconds();
        }
		
        GD.Print($"[DeployToSteamOS] Deploying '{_gameId}' to '{_device.DisplayName} ({_device.Login}@{_device.IPAdress})'");
		
        _localPath = SettingsManager.Instance.Settings.BuildPath;
        if (DirAccess.Open(_localPath) == null)
        {
            GD.PrintErr($"[DeployToSteamOS] Build path '{_localPath}' does not exist.");
            UpdateUIToFail();
            return;
        }
        
        CurrentProgress = StepProgress.Succeeded;
        UpdateUI();
    }
}