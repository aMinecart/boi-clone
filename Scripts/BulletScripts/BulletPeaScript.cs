using Godot;
using System;
using System.Text.RegularExpressions;

public partial class BulletPeaScript : Node2D
{
	[Export] private int speedStat { get; set; } = 200;

	private CharacterBody2D bulletBody;

    public override void _Ready()
    {
        base._Ready();
		bulletBody = GetChild<CharacterBody2D>(0);
		bulletBody.Velocity = new Vector2((float)Math.Cos(Rotation), (float)Math.Sin(Rotation));
		bulletBody.Velocity = bulletBody.Velocity.Normalized() * speedStat;
    }

	public override void _PhysicsProcess(double delta)
	{
		bulletBody.MoveAndSlide();
	}

	private void _on_area_2d_area_entered(Node2D area)
	{
		if(area.IsInGroup("player"))CallDeferred("free");
	}

	private void _on_timer_timeout()
	{
		CallDeferred("free");
	}
}
