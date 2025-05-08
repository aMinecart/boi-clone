using Godot;
using System.Collections.Generic;

public partial class PlayerMovement : CharacterBody2D
{
	// player's maximum velocity (in either the X or Y direction) (units per second)
	[Export] private float Speed { get; set; } = 400.0f;

    // player's minimum speed, calling applyFriction below this speed will set the speed equal to 0
    [Export] private float StopSpeed { get; set; } = 10.0f;

    // player's acceleration value (units per second squared)
    [Export] private float Acceleration { get; set; } = 2000.0f;

    // amount the player slows down by each second
    // multiplied by 6 when the player's desired move direction is not equal to the direction the player is moving in
    [Export] private float Friction { get; set; } = 160.0f;

    // the player's current HP
    [Export] private int Health { get; set; } = 4;

	private bool Vulnerable = true;

	// the direction the user currently wants to move the player in
	private Vector2 InputDirection { get; set; }
    private AnimatedSprite2D sprite;
    private CollisionShape2D collisionShape;
	private Timer timer;


	/***********************************************************************************************************************/


    // how many values should be stored in [speeds] before the oldest is deleted
    private int maxSpeedsStored = 5;

    // a list of the player's speeds on the past (maxSpeedsStored) physics frames
    private List<float> speeds = [];

	// whether the player is dashing or not
	// note: friction and user acceleration disabled for the player during dash
    private bool dashing = false;

	// time remaining (in physics frames) until the player can dash again
    private int TimeToDash { get; set; } = 0;

    // dashing parameters
    private float dashStrength = 2.5f;
	private int dashLength = 20;
	private int dashCooldown = 120;


    /***********************************************************************************************************************/


    // player collison shape used for detecting collisons and taking damage
    private CollisionShape2D hurtbox;

    // whether the player is phasing or not
    // note: player hurtbox is disabled for the player during phase
	private bool phasing = false;

    // time remaining (in physics frames) until the player can phase again
    private int TimeToPhase { get; set; } = 0;

    // phasing parameters
    private int phaseLength = 30;
    private int phaseCooldown = 300;

    private static float FindAvg(List<float> nums)
    {
        float avg = 0.0f;

        foreach (float num in nums)
        {
            avg += num;
        }

        avg /= nums.Count;
        return avg;
    }

    private static float CalcRampup(float f)
    {
        return 2 * f / (f * f + 1);
    }

    private static Vector2 ApplyAcceleration(Vector2 vel, Vector2 accelDir, float accelRate, float maxVel, float fixedDeltaTime)
	{
		if (accelDir == Vector2.Zero)
		{
			return vel;
		}

		// multiplying the acceleration by the max velocity makes it so that the player accelerates up to max velocity in the same amount of time, not matter how big it is
		float accelX = accelDir.X * accelRate * fixedDeltaTime /*  * maxVel  */;

		if (Mathf.Sign(vel.X) == Mathf.Sign(accelDir.X))
		{
            // float speedToMaxSpeedRatio = Mathf.Clamp01((maxVel - Mathf.Abs(vel.X)) / maxVel);
            // print("slowing (X)");
            accelX *= CalcRampup(Mathf.Clamp((maxVel - Mathf.Abs(vel.X)) / maxVel, 0, 1)); // lower acceleration as current X speed gets closer to max speed
		}
		// print(accelX);

		float accelY = accelDir.Y * accelRate * fixedDeltaTime /*  * maxVel  */;

        if (Mathf.Sign(vel.Y) == Mathf.Sign(accelDir.Y))
		{
			// print("slowing (Y)");
			accelY *= CalcRampup(Mathf.Clamp(((maxVel - Mathf.Abs(vel.Y)) / maxVel), 0, 1)); // lower acceleration as current Y speed gets closer to max speed
		}
		// print(accelY);

		return vel + new Vector2(accelX, accelY);
	}

    private static Vector2 ApplyDash(Vector2 vel, Vector2 accelDir, float avgSpeed, float strength)
    {
        float dashSpeed = Mathf.Max(vel.Length(), avgSpeed);

        return accelDir != Vector2.Zero ? accelDir * dashSpeed * strength : vel.Normalized() * dashSpeed * strength;
    }

    private static Vector2 ApplyFriction(Vector2 vel, Vector2 accelDir, float friction, float stopSpeed, float fixedDeltaTime)
	{
		// add scaling based on changes to maximum speed (Speed is currently the max speed in the X or Y direction, not both)

		if (vel.Length() < stopSpeed)
		{
			return Vector2.Zero;
		}
		
        float angle = vel.Angle();
		float reduction = friction * fixedDeltaTime;
		
		float accelX = (Mathf.Sign(vel.X) != Mathf.Sign(accelDir.X) ? 2 * 6 : 1) * reduction * Mathf.Cos(angle);
        float accelY = (Mathf.Sign(vel.Y) != Mathf.Sign(accelDir.Y) ? 2 * 6 : 1) * reduction * Mathf.Sin(angle);

        /* 
		 * multiply current velocity by new speed and divide by current speed to set magnitude equal to the new value (cannot subtract from the vector's magnitude) 
		 * 
		 * float speed = vel.Length();
		 * vel *= Mathf.Max(speed - reduction, 0) / speed;
		 */

        return vel - new Vector2(accelX, accelY);
	}

    private static void ApplyPhase(CollisionShape2D collisionShape2D)
    {
        collisionShape2D.Disabled = true;
    }

    private static void StopPhase(CollisionShape2D collisionShape2D)
    {
        collisionShape2D.Disabled = false;
    }

    private void StoreCurrSpeed()
    {
        speeds.Add(Velocity.Length());

        if (speeds.Count > maxSpeedsStored)
        {
            speeds.RemoveAt(0);
        }
    }

	private void HandleTimers()
	{
		if (TimeToDash > 0)
		{
			TimeToDash--;
		}

		if (TimeToPhase > 0)
		{
			TimeToPhase--;
		}

        if (dashing && TimeToDash < dashCooldown - dashLength)
        {
            dashing = false;
        }

        if (phasing && TimeToPhase < phaseCooldown - phaseLength)
        {
            Unphase();
        }
    }

    private void Dash()
    {
        Velocity = ApplyDash(Velocity, InputDirection, FindAvg(speeds), dashStrength);
        TimeToDash = dashCooldown; // reset dash timer to prevent infinite dashing
        dashing = true;
    }

    private void Phase()
    {
        ApplyPhase(hurtbox);
        TimeToPhase = phaseCooldown; // reset phase timer to prevent infinite phasing
        phasing = true;
    }

    private void Unphase()
    {
        StopPhase(hurtbox);
        phasing = false;
    }

    public void GetInput()
	{
        InputDirection = Input.GetVector("left", "right", "up", "down");
	}
	
	public void UpdateGlobalVars()
	{
		PlayerVars.Instance.Position = GlobalPosition;
		PlayerVars.Instance.Health = Health;
	}

	public void IFramesFlash()
	{
		if (!Vulnerable)
        {
            sprite.Visible = !sprite.Visible; // toggle sprite visibility every frame to create a flash
        }
		else if (Vulnerable && !sprite.Visible)
        {
            sprite.Visible = true;
        }
	}

    public override void _Ready()
    {
		sprite = GetNode<AnimatedSprite2D>("AnimatedSprite2D");
		timer = GetNode<Timer>("AnimationTimer");
        hurtbox = GetNode<Area2D>("Area2D").GetNode<CollisionShape2D>("CollisionShape2D");
    }

    public override void _PhysicsProcess(double delta)
	{
		StoreCurrSpeed();
        GetInput();

        if (!dashing)
		{
            Velocity = ApplyFriction(Velocity, InputDirection, Friction, StopSpeed, (float)delta);
            Velocity = ApplyAcceleration(Velocity, InputDirection, Acceleration, Speed, (float)delta);
        }

        if (Input.IsActionJustPressed("phase_dash") && TimeToDash <= 0 && TimeToPhase <= 0)
        {
            Dash();
            Phase();
        }
        else if (Input.IsActionJustPressed("dash") && TimeToDash <= 0)
		{
            Dash();
		}
        /*
        else if(Input.IsActionJustPressed("phase") && TimeToPhase <= 0)
        {
            Phase();			
		}
		
        GD.Print("Time to dash: ", TimeToDash, ", dashing: ", dashing);
        GD.Print("Time to phase: ", TimeToPhase, ", phasing: ", phasing);
        */

        IFramesFlash();
        MoveAndSlide();

        HandleTimers();
        UpdateGlobalVars();
	}

    private void _OnArea2DAreaEntered(Node2D area)
	{
		if (area.IsInGroup("enemyAttack") && Vulnerable)
		{
			Health--;
			if (Health <= 0)
			{
				CallDeferred(MethodName.Free);
				SceneManager.instance.ChangeScene(eSceneNames.GameOver);
			}
			else
			{
				timer.Start(1);
				Vulnerable = false;
			}
		}
	}

	private void _OnTimerTimeout()
	{
		Vulnerable = true;
	}
}