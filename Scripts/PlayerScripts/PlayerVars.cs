using Godot;

public partial class PlayerVars : Node
{
	public static PlayerVars Instance { get; private set; }

	public Vector2 Position { get; set; }
	public float Health { get; set; }
	public float Ammo { get; set; }

	public Vector2 CamPos { get; set; }

	public override void _Ready()
	{
		Instance = this;
	}
}