using Godot;
using System;
using System.Numerics;

public partial class EnemyChaseScript : Node2D
{
    private CharacterBody2D enemyBody;
    [Export] private int speedStat = 500;

    public override void _Ready()
    {
        base._Ready();
        enemyBody = GetNode<CharacterBody2D>("CharacterBody2D");
    }

    private void TurnToPlayer()
    {
        enemyBody.LookAt(PlayerVars.Instance.Position);
    }

    private void SetVelocity()
    {
        enemyBody.Velocity = Godot.Vector2.FromAngle(enemyBody.Rotation);
		enemyBody.Velocity = enemyBody.Velocity.Normalized() * speedStat;
    }

    public override void _PhysicsProcess(double delta)
	{
        TurnToPlayer();
        SetVelocity();
        enemyBody.MoveAndSlide();
	}
}
