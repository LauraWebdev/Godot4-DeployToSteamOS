using System.Threading.Tasks;
using Godot;

namespace Laura.DeployToSteamOS;

public partial class DeployWindow
{
    private async Task DeployPrepareUpload(Callable logCallable)
    {
        CurrentStep = DeployStep.PrepareUpload;
        CurrentProgress = StepProgress.Running;
        UpdateUI();
        
        _prepareUploadResult = await SteamOSDevkitManager.PrepareUpload(
            _device,
            _gameId,
            logCallable
        );
        
        AddToConsole(DeployStep.PrepareUpload, $"User: {_prepareUploadResult.User}");
        AddToConsole(DeployStep.PrepareUpload, $"Directory: {_prepareUploadResult.Directory}");
        
        CurrentProgress = StepProgress.Succeeded;
        UpdateUI();
    }
}