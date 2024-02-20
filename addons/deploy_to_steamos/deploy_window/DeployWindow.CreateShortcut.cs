using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;

namespace Laura.DeployToSteamOS;

public partial class DeployWindow
{
    private async Task DeployCreateShortcut(Callable logCallable)
    {
        CurrentStep = DeployStep.CreateShortcut;
        CurrentProgress = StepProgress.Running;
        UpdateUI();
        
        var createShortcutParameters = new SteamOSDevkitManager.CreateShortcutParameters
        {
            gameid = _gameId,
            directory = _prepareUploadResult.Directory,
            argv = new[] { "game.x86_64", SettingsManager.Instance.Settings.StartParameters },
            settings = new Dictionary<string, string>
            {
                { "steam_play", "0" }
            },
        };
        
        // TODO: Fix Result, success/error are not filled in response but exist/dont
        _createShortcutResult = await SteamOSDevkitManager.CreateShortcut(
            _device,
            createShortcutParameters,
            logCallable
        );
        
        CurrentProgress = StepProgress.Succeeded;
        UpdateUI();
    }
}