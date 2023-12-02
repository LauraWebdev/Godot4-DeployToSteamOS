using Godot;

public partial class DemoMovement : CharacterBody2D
{
	public const float Speed = 300.0f;

	public override void _PhysicsProcess(double delta)
	{
		var direction = Input.GetVector("ui_left", "ui_right", "ui_up", "ui_down");

		Velocity = direction * Speed;
		MoveAndSlide();
	}
}
