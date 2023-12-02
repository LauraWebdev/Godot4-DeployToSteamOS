using Godot;

[Tool]
public partial class DeployButton : Button
{
	public void Deploy()
	{
		// TODO: Build Project
		GD.Print("Building Project");
		
		// TODO: Deploy with SteamOS Dev Client
		GD.Print("Deploying to SteamOS");
	}
}
