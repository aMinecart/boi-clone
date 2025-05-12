using Godot;
using System;
using System.Reflection.PortableExecutable;

public partial class BossSprayerScript : Node2D
{
	[Export] public PackedScene BulletScene { get; set; }
	private Node2D enemyBulletParent;
	private CharacterBody2D enemyBody;
	private CharacterBody2D leftGunBody;
	private CharacterBody2D rightGunBody;
	private Timer shotTimer;
	private Timer zoneTimer;
	private Timer overrideTimer;
	private bool playerInInclusionZone = false;
	private bool playerInExclusionZone = false;
	private bool stateChangeReady = false;
	private int speedStat = 150;

	public enum BossState {
		Idle = 1,
		Shotgun = 2,
		SprayLeft = 3,
		SprayRight = 4,
		Funnel = 5,
	}

	public enum BossSubstate {
		Startup = 1,
		Active = 2,
		Recovery = 3,
	}

	public enum PlayerArea
	{
		Far = 1,
		Goldilocks = 2,
		Close = 3,
	}

	BossState bossState = BossState.Idle;
	BossSubstate subState = BossSubstate.Startup;
	PlayerArea playerArea = PlayerArea.Far;
	int shotsFired = 0;
	int shotsToFire = 10;
	int leftGunRotationSpeed = 1;
	int rightGunRotationSpeed = 1;

	private void ResetVars(int shotsToFire, int leftGunRotationSpeed, int rightGunRotationSpeed)
	{
		shotsFired = 0;
		this.shotsToFire = shotsToFire;
		this.leftGunRotationSpeed = leftGunRotationSpeed;
		this.rightGunRotationSpeed = rightGunRotationSpeed;
	}
	private void LeftGunShoot()
	{
		Node2D bullet = (Node2D)BulletScene.Instantiate();
		bullet.GlobalPosition = leftGunBody.GlobalPosition;
		bullet.Rotation = leftGunBody.Rotation + enemyBody.Rotation;
		enemyBulletParent.AddChild(bullet);
		shotsFired++;
	}
	private void RightGunShoot()
	{
		Node2D bullet = (Node2D)BulletScene.Instantiate();
		bullet.GlobalPosition = rightGunBody.GlobalPosition + (Godot.Vector2.FromAngle(rightGunBody.Rotation + enemyBody.Rotation).Normalized() * 250);
		bullet.Rotation = rightGunBody.Rotation + enemyBody.Rotation;
		enemyBulletParent.AddChild(bullet);
		shotsFired++;
	}

	private void BothGunShoot()
	{
		LeftGunShoot();
		RightGunShoot();
	}

	private void ZoneDetection()
	{
		if (playerInInclusionZone && !playerInExclusionZone && !(playerArea == PlayerArea.Goldilocks))
		{
			playerArea = PlayerArea.Goldilocks;
			ResetZoneTimer();
		}
		else if (playerInInclusionZone && playerInExclusionZone && !(playerArea == PlayerArea.Close))
		{
			playerArea = PlayerArea.Close;
			ResetZoneTimer();
		}
		else if (!(playerArea == PlayerArea.Far))
		{
			playerArea = PlayerArea.Far;
			ResetZoneTimer();
		}
	}

	private void ZoneMovement()
	{
		if (playerArea == PlayerArea.Goldilocks)
		{
			enemyBody.Velocity = new Vector2(0, 0);
		}
		else if (playerArea == PlayerArea.Close)
		{
			enemyBody.Velocity = Godot.Vector2.FromAngle(enemyBody.Rotation);
			enemyBody.Velocity = enemyBody.Velocity.Normalized() * speedStat * -1;
		}
		else
		{
			enemyBody.Velocity = Godot.Vector2.FromAngle(enemyBody.Rotation);
			enemyBody.Velocity = enemyBody.Velocity.Normalized() * speedStat;
		}
	}

	private void ResetZoneTimer()
	{
		if (playerArea == PlayerArea.Goldilocks)
		{
			zoneTimer.Start(1);
		}
		else if (playerArea == PlayerArea.Close)
		{
			zoneTimer.Start(0.75f);
		}
		else
		{
			zoneTimer.Start(3);
		}

		if (bossState != BossState.Idle)
			zoneTimer.Paused = true;
	}

	private void Idle()
	{
		enemyBody.LookAt(PlayerVars.Instance.Position);
		leftGunBody.LookAt(PlayerVars.Instance.Position);
		rightGunBody.LookAt(PlayerVars.Instance.Position);
		ZoneMovement();

		if(playerInInclusionZone && playerInExclusionZone && stateChangeReady)
		{
			bossState = BossState.Shotgun;
		}
		else if (playerInInclusionZone && !playerInExclusionZone && stateChangeReady)
		{
			bossState = BossState.SprayLeft;
		}           
		else if (!playerInInclusionZone && !playerInExclusionZone && stateChangeReady)
		{
			bossState = BossState.Funnel;
		}
	}

	private void ShotGun()
	{
		if (subState == BossSubstate.Startup)
		{
			ResetVars(23, -2, 2);
			enemyBody.LookAt(PlayerVars.Instance.Position);
			leftGunBody.LookAt(PlayerVars.Instance.Position);
			rightGunBody.LookAt(PlayerVars.Instance.Position);
			enemyBody.Velocity = Godot.Vector2.FromAngle(enemyBody.Rotation);
			enemyBody.Velocity = enemyBody.Velocity.Normalized() * speedStat * -7;
			zoneTimer.Paused = true;

			if (!playerInInclusionZone && !playerInExclusionZone){   
				shotTimer.Start(0.09);
				shotTimer.Paused = false;
				enemyBody.Velocity = new Vector2(0, 0);
				subState = BossSubstate.Active;
			}
		}
		else if (subState == BossSubstate.Active)
		{
			if(leftGunBody.RotationDegrees >= 60) leftGunRotationSpeed = -2;
			else if(leftGunBody.RotationDegrees <= -60) leftGunRotationSpeed = 2;
			if(rightGunBody.RotationDegrees >= 60) rightGunRotationSpeed = -2;
			else if(rightGunBody.RotationDegrees <= -60) rightGunRotationSpeed = 2;
			leftGunBody.RotationDegrees += leftGunRotationSpeed;
			rightGunBody.RotationDegrees += rightGunRotationSpeed;
			if(shotsFired >= shotsToFire) subState = BossSubstate.Recovery;
		}
		else if (subState == BossSubstate.Recovery)
		{
			shotTimer.Start(0.15);
			shotTimer.Paused = true;
			zoneTimer.Paused = false;
			overrideTimer.Paused = false;
			stateChangeReady = false;
			subState = BossSubstate.Startup;
			bossState = BossState.Idle;
		}
	}

	private void SprayLeft()
	{
		if (subState == BossSubstate.Startup)
		{
			ResetVars(0, -4, 0);
			enemyBody.Velocity = new Vector2(0, 0);
			leftGunBody.RotationDegrees += leftGunRotationSpeed;
			if (leftGunBody.RotationDegrees <= -120)
			{
				leftGunRotationSpeed = 2;
				shotTimer.Start(0.15);
				shotTimer.Paused = false;
				subState = BossSubstate.Active;
				zoneTimer.Paused = true;
			}
		}
		else if (subState == BossSubstate.Active)
		{
			leftGunBody.RotationDegrees += leftGunRotationSpeed;
			if (leftGunBody.RotationDegrees >= 45)
			{
				shotTimer.Paused = true;
				subState = BossSubstate.Recovery;
			}
		}
		else if (subState == BossSubstate.Recovery)
		{
			shotTimer.Start(0.15);
			shotTimer.Paused = true;
			zoneTimer.Paused = false;
			overrideTimer.Paused = false;
			stateChangeReady = false;
			subState = BossSubstate.Startup;
			bossState = BossState.SprayRight;
		}
	}

	private void SprayRight()
	{
		if (subState == BossSubstate.Startup)
		{
			ResetVars(0, 0, 4);
			enemyBody.Velocity = new Vector2(0, 0);
			rightGunBody.RotationDegrees += rightGunRotationSpeed;
			if (rightGunBody.RotationDegrees >= 120)
			{
				rightGunRotationSpeed = -2;
				shotTimer.Start(0.15);
				shotTimer.Paused = false;
				subState = BossSubstate.Active;
				zoneTimer.Paused = true;
			}
		}
		else if (subState == BossSubstate.Active)
		{
			rightGunBody.RotationDegrees += rightGunRotationSpeed;
			if (rightGunBody.RotationDegrees <= -45)
			{
				shotTimer.Paused = true;
				subState = BossSubstate.Recovery;
			}
		}
		else if (subState == BossSubstate.Recovery)
		{
			shotTimer.Start(0.25);
			shotTimer.Paused = true;
			zoneTimer.Paused = false;
			overrideTimer.Paused = false;
			stateChangeReady = false;
			subState = BossSubstate.Startup;
			bossState = BossState.Idle;
		}
	}

	private void Funnel()
	{
		enemyBody.Velocity = Godot.Vector2.FromAngle(enemyBody.Rotation);
		enemyBody.Velocity = enemyBody.Velocity.Normalized() * speedStat;
		if (subState == BossSubstate.Startup)
		{
			ResetVars(18, -4, 4);
			rightGunBody.RotationDegrees += rightGunRotationSpeed;
			leftGunBody.RotationDegrees += leftGunRotationSpeed;
			if (rightGunBody.RotationDegrees >= 70 && leftGunBody.RotationDegrees <= -70)
			{
				rightGunRotationSpeed = -1;
				leftGunRotationSpeed = 1;
				shotTimer.Start(0.12);
				shotTimer.Paused = false;
				subState = BossSubstate.Active;
				zoneTimer.Paused = true;
			}
		}
		else if (subState == BossSubstate.Active)
		{
			leftGunBody.RotationDegrees += leftGunRotationSpeed;
			rightGunBody.RotationDegrees += rightGunRotationSpeed;
			if (shotsFired >= shotsToFire) subState = BossSubstate.Recovery;
		}
		else if (subState == BossSubstate.Recovery)
		{
			shotTimer.Start(0.15);
			shotTimer.Paused = true;
			zoneTimer.Paused = false;
			overrideTimer.Paused = false;
			stateChangeReady = false;
			subState = BossSubstate.Startup;
			bossState = BossState.Idle;
		}
	}

	public override void _Ready()
	{
		base._Ready();
		enemyBody = GetNode<CharacterBody2D>("EnemyBody");
		leftGunBody = GetNode<CharacterBody2D>("EnemyBody/LeftGunBody");
		rightGunBody = GetNode<CharacterBody2D>("EnemyBody/RightGunBody");
		shotTimer = GetNode<Timer>("ShotTimer");
		zoneTimer = GetNode<Timer>("ZoneTimer");
		overrideTimer = GetNode<Timer>("OverrideTimer");
		shotTimer.Paused = true;
		
		enemyBulletParent = GetTree().GetRoot().GetNode<Node2D>("Dungeon/EnemyBulletParent");
		if (enemyBulletParent is null)
		{
			enemyBulletParent = GetTree().GetRoot().GetNode<Node2D>("TestScene/EnemyBulletParent");
		}
	}

	public override void _PhysicsProcess(double delta)
	{
		base._PhysicsProcess(delta);
		if(bossState == BossState.Idle) Idle();
		else if (bossState == BossState.Shotgun) ShotGun();
		else if (bossState == BossState.SprayLeft) SprayLeft();
		else if (bossState == BossState.SprayRight) SprayRight();
		else if (bossState == BossState.Funnel) Funnel();
		enemyBody.MoveAndSlide();
	}


	private void _on_shot_timer_timeout()
	{
		if(bossState == BossState.Shotgun || bossState == BossState.Funnel) BothGunShoot();
		if(bossState == BossState.SprayLeft) LeftGunShoot();
		if(bossState == BossState.SprayRight) RightGunShoot();
	}

	private void _on_zone_timer_timeout()
	{
		stateChangeReady = true;
		zoneTimer.Paused = true;
		overrideTimer.Paused = true;
	}

	private void _on_override_timer_timeout()
	{
		stateChangeReady = true;
		zoneTimer.Paused = true;
		overrideTimer.Paused = true;
	}

	private void _on_inclusion_zone_area_entered(Node2D area)
	{
		
		if(area.IsInGroup("player"))
		{
			playerInInclusionZone = true;
			ZoneDetection();
		}
	}

	private void _on_inclusion_zone_area_exited(Node2D area)
	{
	   
		if(area.IsInGroup("player"))
		{
			playerInInclusionZone = false;
			ZoneDetection();
		}
	}

	private void _on_exclusion_zone_area_entered(Node2D area)
	{
		
		if(area.IsInGroup("player"))
		{
			playerInExclusionZone = true;
			ZoneDetection();
		}

	}

	private void _on_exclusion_zone_area_exited(Node2D area)
	{
		
		if(area.IsInGroup("player"))
		{
			playerInExclusionZone = false;
			ZoneDetection();
		}
	}

	private void _OnBossBodyAreaEntered(Node2D area)
	{
		if (area.IsInGroup("playerAttack"))
		{
			CallDeferred(MethodName.Free);
		}
	}
}
