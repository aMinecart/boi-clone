using Godot;

public partial class PlayerMovement : CharacterBody2D
{
	[Export]
	private int Speed { get; set; } = 400;
	private int Health = 4; 

	public void GetInput()
	{

		Vector2 inputDirection = Input.GetVector("left", "right", "up", "down");
		Velocity = inputDirection * Speed;
	}
	
	public void UpdateGlobalVars()
	{
		PlayerVars.Instance.Position = GlobalPosition;
		PlayerVars.Instance.Health = Health;
	}

	public override void _PhysicsProcess(double delta)
	{
		GetInput();
		MoveAndSlide();
		UpdateGlobalVars();
	}

	private void _on_area_2d_area_entered(Node2D area)
	{
		if(area.IsInGroup("enemy"))Health--;
	}
}
