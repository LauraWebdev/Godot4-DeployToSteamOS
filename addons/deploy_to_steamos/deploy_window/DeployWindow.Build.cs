using System.IO;
using System.Threading.Tasks;
using Godot;

namespace Laura.DeployToSteamOS;

public partial class DeployWindow
{
    private async Task DeployBuild(Callable logCallable)
    {
        CurrentStep = DeployStep.Building;
        CurrentProgress = StepProgress.Running;
        UpdateUI();
        
        // Adding a 2 second delay so the UI can update
        await ToSignal(GetTree().CreateTimer(2), "timeout");
        var buildTask = new TaskCompletionSource<bool>();
        
        GodotExportManager.ExportProject(
            ProjectSettings.GlobalizePath("res://"),
            Path.Join(_localPath, "game.x86_64"),
            false,
            logCallable,
            () => { buildTask.SetResult(true); }
        );
        
        await buildTask.Task;
        
        CurrentProgress = StepProgress.Succeeded;
        UpdateUI();
    }
}