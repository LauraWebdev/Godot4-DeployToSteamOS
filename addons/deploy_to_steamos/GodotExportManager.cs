using System;
using System.Diagnostics;
using Godot;

public class GodotExportManager
{
    public static void ExportProject(
        string godotExecutablePath,
        string projectPath,
        string outputPath,
        bool isReleaseBuild,
        Action<string> onOutputDataReceived = null,
        Action onProcessExited = null)
    {
        GD.Print("[GodotExportManager] Starting Project Export");
        
        GD.Print("[GodotExportManager] Output: " + outputPath);
        GD.Print("[GodotExportManager] Release: " + (isReleaseBuild ? "Yes" : "No"));
        
        Process exportProcess = new Process();
        
        exportProcess.StartInfo.FileName = godotExecutablePath;

        var arguments = $"--headless --path \"{projectPath}\" ";
        arguments += isReleaseBuild ? "--export-release " : "--export-debug ";
        arguments += "\"Steamdeck\" ";
        arguments += $"\"{outputPath}\"";
        
        exportProcess.StartInfo.Arguments = arguments;
        exportProcess.StartInfo.UseShellExecute = false;
        exportProcess.StartInfo.RedirectStandardOutput = true;
        exportProcess.EnableRaisingEvents = true;
        
        exportProcess.OutputDataReceived += (sender, args) =>
        {
            onOutputDataReceived?.Invoke(args.Data);
        };

        exportProcess.Exited += (sender, args) =>
        {
            onProcessExited?.Invoke();
        };

        exportProcess.Start();
        exportProcess.BeginOutputReadLine();
        exportProcess.WaitForExit();
    }
}