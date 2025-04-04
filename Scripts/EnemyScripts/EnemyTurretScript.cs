using Godot;
using System;

public partial class EnemyTurretScript : Node2D
{
	[Export] public PackedScene BulletScene { get; set; }
	 private CharacterBody2D enemyBody;
	private void Shoot()
	{
		Node2D bullet = (Node2D)BulletScene.Instantiate();
        bullet.GlobalPosition = GlobalPosition;
		bullet.Rotation = Rotation;
		GetTree().GetRoot().GetNode("TestScene/EnemyBulletParent").AddChild(bullet);
	}
	public override void _PhysicsProcess(double delta)
	{
		LookAt(PlayerVars.Instance.Position);
	}

	private void _on_timer_timeout()
	{
		Shoot();
	}
}
