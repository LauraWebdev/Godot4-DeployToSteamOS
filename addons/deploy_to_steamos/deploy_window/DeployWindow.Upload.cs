using System.Threading.Tasks;
using Godot;

namespace Laura.DeployToSteamOS;

public partial class DeployWindow
{
    private async Task DeployUpload(Callable logCallable)
    {
        CurrentStep = DeployStep.Uploading;
        CurrentProgress = StepProgress.Running;
        UpdateUI();
        
        await SteamOSDevkitManager.CopyFiles(
            _device,
            _localPath, 
            _prepareUploadResult.Directory,
            logCallable
        );
        
        CurrentProgress = StepProgress.Succeeded;
        UpdateUI();
    }
}