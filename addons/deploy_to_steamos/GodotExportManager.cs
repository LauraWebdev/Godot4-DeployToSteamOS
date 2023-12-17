using System;
using System.Diagnostics;
using Godot;

namespace Laura.DeployToSteamOS;

public class GodotExportManager
{
    public static void ExportProject(
        string projectPath,
        string outputPath,
        bool isReleaseBuild,
        Callable logCallable,
        Action OnProcessExited = null)
    {
        logCallable.CallDeferred("Starting project export, this may take a while.");
        logCallable.CallDeferred($"Output path: {outputPath}");
        logCallable.CallDeferred($"Is Release Build: {(isReleaseBuild ? "Yes" : "No")}");
        
        Process exportProcess = new Process();
        
        exportProcess.StartInfo.FileName = OS.GetExecutablePath();

        var arguments = $"--headless --path \"{projectPath}\" ";
        arguments += isReleaseBuild ? "--export-release " : "--export-debug ";
        arguments += "\"Steamdeck\" ";
        arguments += $"\"{outputPath}\"";
        
        exportProcess.StartInfo.Arguments = arguments;
        exportProcess.StartInfo.UseShellExecute = false;
        exportProcess.StartInfo.RedirectStandardOutput = true;
        exportProcess.EnableRaisingEvents = true;

        exportProcess.ErrorDataReceived += (sender, args) =>
        {
            throw new Exception("Error while building project: " + args.Data);
        }; 
        
        exportProcess.OutputDataReceived += (sender, args) =>
        {
            logCallable.CallDeferred(args.Data);
        };

        exportProcess.Exited += (sender, args) =>
        {
            OnProcessExited?.Invoke();
            exportProcess.WaitForExit();
        };

        exportProcess.Start();
        exportProcess.BeginOutputReadLine();
    }
}