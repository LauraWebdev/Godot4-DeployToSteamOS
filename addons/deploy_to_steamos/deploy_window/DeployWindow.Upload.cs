using System;
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

        try
        {
            await SteamOSDevkitManager.CopyFiles(
                _device,
                _localPath,
                _prepareUploadResult.Directory,
                logCallable
            );
        }
        catch (Exception e)
        {
            AddToConsole(DeployStep.Uploading, e.Message);
            UpdateUIToFail();
        }

        CurrentProgress = StepProgress.Succeeded;
        UpdateUI();
    }
}