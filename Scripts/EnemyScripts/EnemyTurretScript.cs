using Godot;
using System;

public partial class EnemyTurretScript : Node2D
{
	[Export] public PackedScene BulletScene { get; set; }
	private CharacterBody2D enemyBody;
	private Node2D enemyBulletParent;
	
	private void Shoot()
	{
		Node2D bullet = (Node2D)BulletScene.Instantiate();
		bullet.GlobalPosition = GlobalPosition;
		bullet.Rotation = enemyBody.Rotation;
		enemyBulletParent.AddChild(bullet);
	}

	public override void _Ready()
	{
		base._Ready();
		enemyBody = GetNode<CharacterBody2D>("CharacterBody2D");
		
		enemyBulletParent = GetTree().GetRoot().GetNode<Node2D>("Dungeon/EnemyBulletParent");
		if (enemyBulletParent is null)
		{
			enemyBulletParent = GetTree().GetRoot().GetNode<Node2D>("TestScene/EnemyBulletParent");
		}
	}

	public override void _PhysicsProcess(double delta)
	{
		enemyBody.LookAt(PlayerVars.Instance.Position);
	}

	private void _OnTimerTimeout()
	{
		Shoot();
	}

	private void _OnArea2DAreaEntered(Node2D area)
	{
		if (area.IsInGroup("playerAttack"))
		{
			CallDeferred(MethodName.Free);
		}
	}
}
