using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Godot;

[Tool]
public partial class DeployWindow : Window
{
	public enum DeployStep
	{
		Init,
		Building,
		PrepareUpload,
		Uploading,
		CreateShortcut,
		Done
	}

	public DeployStep CurrentStep = DeployStep.Init;

	public async void Deploy(SteamOSDevkitManager.Device device)
	{
		if (device == null)
		{
			GD.PrintErr("[DeployToSteamOS] Device is not available.");
			return;
		}
		
		Show();

		var gameId = ProjectSettings.GetSetting("application/config/name", "game").AsString();
		gameId = Regex.Replace(gameId, @"[^a-zA-Z0-9]", string.Empty);
		
		GD.Print($"[DeployToSteamOS] Deploying '{gameId}' to '{device.DisplayName} ({device.Login}@{device.IPAdress})'");
		
		// TODO: make temp folder
		var localPath = @"C:\Users\hello\Development\Godot4-DeployToSteamOS-Builds";

		// BUILDING PROJECT
		CurrentStep = DeployStep.Building;
		UpdateUI();
		
		// Adding a 2 second delay so the UI can update
		await ToSignal(GetTree().CreateTimer(2), "timeout");
		var buildTask = new TaskCompletionSource<bool>();
		GodotExportManager.ExportProject(
			ProjectSettings.GlobalizePath("res://"),
			Path.Join(localPath, "game.x86_64"),
			false, 
			data => { GD.Print(data); },
			() => { buildTask.SetResult(true); }
		);
		await buildTask.Task;

		// PREPARING UPLOAD
		CurrentStep = DeployStep.PrepareUpload;
		UpdateUI();
		var prepareResult = await SteamOSDevkitManager.PrepareUpload(device, gameId);

		// UPLOADING
		CurrentStep = DeployStep.Uploading;
		UpdateUI();
		// TODO: Update while copying
		await SteamOSDevkitManager.CopyFiles(device, localPath, prepareResult.Directory);

		// CREATING SHORTCUT
		CurrentStep = DeployStep.CreateShortcut;
		UpdateUI();
		var createShortcutParameters = new SteamOSDevkitManager.CreateShortcutParameters
		{
			gameid = gameId,
			directory = prepareResult.Directory,
			argv = new[] { "game.x86_64" },
			settings = new Dictionary<string, string>
			{
				{ "steam_play", "0" }
			},
		};
		// TODO: Fix Result
		var createShortcutResult = await SteamOSDevkitManager.CreateShortcut(device, createShortcutParameters);

		// DONE
		CurrentStep = DeployStep.Done;
		UpdateUI();
	}

	public void Close()
	{
		GD.Print("Close@" + CurrentStep);
		if (CurrentStep != DeployStep.Init && CurrentStep != DeployStep.Done) return;
		Hide();
	}

	private void UpdateUI()
	{
		// TODO: UpdateUI
		GD.Print(CurrentStep);
	}
}
