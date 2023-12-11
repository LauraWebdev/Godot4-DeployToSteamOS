using Godot;

[Tool]
public partial class DeployButton : Button
{
	public void Deploy()
	{
		// TODO: Build Project
		GD.Print("Building Project");
		
		// TODO: Deploy with SteamOS Dev Client
		//GD.Print("Deploying to SteamOS");
		
		GodotExportManager.ExportProject(
			@"C:\Users\hello\Development\Godot\Godot_v4.2-stable_mono_win64_console.exe",
			@"C:\Users\hello\Development\Godot4-DeployToSteamOS",
			@"C:\Users\hello\Development\Godot4-DeployToSteamOS-Builds\game.x86_64",
			true,
			data => GD.Print(data),
			() => GD.Print("Process completed.")
		);
	}
}
