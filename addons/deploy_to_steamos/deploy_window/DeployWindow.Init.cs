using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Godot;

public partial class DeployWindow
{
    private async Task DeployInit()
    {
        CurrentStep = DeployStep.Init;
        CurrentProgress = StepProgress.Running;
        UpdateUI();
        
        _gameId = ProjectSettings.GetSetting("application/config/name", "game").AsString();
        _gameId = Regex.Replace(_gameId, @"[^a-zA-Z0-9]", string.Empty);
		
        GD.Print($"[DeployToSteamOS] Deploying '{_gameId}' to '{_device.DisplayName} ({_device.Login}@{_device.IPAdress})'");
		
        // TODO: Don't Hardcode
        _localPath = @"C:\Users\hello\Development\Godot4-DeployToSteamOS-Builds";
        
        CurrentProgress = StepProgress.Succeeded;
        UpdateUI();
    }
}