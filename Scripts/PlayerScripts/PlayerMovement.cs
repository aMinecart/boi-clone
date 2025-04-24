using Godot;
using System.Collections.Generic;

public partial class PlayerMovement : CharacterBody2D
{
  // player's maximum velocity (in either the X or Y direction) (units per second)
	private float Speed { get; set; } = 700.0f;

	// player's minimum speed, calling applyFriction below this speed will set the speed equal to 0
	private float StopSpeed { get; set; } = 10.0f;

	// player's acceleration value (units per second squared)
    private float Acceleration { get; set; } = 1500.0f;

    // amount the player slows down by each second
	// multiplied by 6 when the player's desired move direction is not equal to the direction the player is moving in
    private float Friction { get; set; } = 160.0f;

	// the player's current HP
    private int Health { get; set; } = 4;

	// the direction the user currently wants to move the player in
    private Vector2 InputDirection;


	/***********************************************************************************************************************/


    // how many values should be stored in [speeds] before the oldest is deleted
    private int maxSpeedsStored = 5;

    // a list of the player's speeds on the past (maxSpeedsStored) physics frames
    private List<float> speeds = [];

	// whether the player is dashing or not
	// note: friction and player acceleration disabled during dash
    private bool dashing = false;

	// time remaining (in physics frames) until the player can dash again
    private int timeToDash { get; set; } = 0;

    // dashing parameters
    private float dashStrength = 1.5f;
	private int dashLength = 20;
	private int dashCooldown = 120;


	private CollisionShape2D collisionShape;

	private bool phasing = false;

    // time remaining (in physics frames) until the player can phase again
    private int timeToPhase { get; set; } = 0;

    private int phaseLength = 30;
    private int phaseCooldown = 120;

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

		float accelX, accelY;
		
        float angle = vel.Angle();
		float reduction = friction * fixedDeltaTime;
		
		accelX = (Mathf.Sign(vel.X) != Mathf.Sign(accelDir.X) ? 6 : 1) * reduction * Mathf.Cos(angle);
        accelY = (Mathf.Sign(vel.Y) != Mathf.Sign(accelDir.Y) ? 6 : 1) * reduction * Mathf.Sin(angle);

        /* 
		 * multiply current velocity by new speed and divide by current speed to set magnitude equal to the new value (cannot subtract from the vector's magnitude) 
		 * 
		 * float speed = vel.Length();
		 * vel *= Mathf.Max(speed - reduction, 0) / speed;
		 */

        return vel - new Vector2(accelX, accelY);
	}

    private void ApplyPhase()
    {
        collisionShape.Disabled = true;
    }

    private void StopPhase()
    {
        collisionShape.Disabled = false;
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
		if (timeToDash > 0)
		{
			timeToDash--;
		}

		if (timeToPhase > 0)
		{
			timeToPhase--;
		}

        if (dashing && timeToDash < dashCooldown - dashLength)
        {
            dashing = false;

			/*
            if (phasingInDash)
            {
                phasingInDash = false;
                StopPhase();
            }
			*/
        }

        if (phasing && timeToPhase < phaseCooldown - phaseLength)
        {
			StopPhase();
            phasing = false;
        }
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


    public override void _Ready()
    {
		collisionShape = GetNode<Area2D>("Area2D").GetNode<CollisionShape2D>("CollisionShape2D");
    }

    public override void _PhysicsProcess(double delta)
	{
		HandleTimers();
		StoreCurrSpeed();

        GetInput();

        if (!dashing)
		{
            Velocity = ApplyFriction(Velocity, InputDirection, Friction, StopSpeed, (float)delta);
            Velocity = ApplyAcceleration(Velocity, InputDirection, Acceleration, Speed, (float)delta);
        }

		if (Input.IsActionJustPressed("dash") && timeToDash <= 0)
		{
			Velocity = ApplyDash(Velocity, InputDirection, FindAvg(speeds), dashStrength);
			timeToDash = dashCooldown; // reset dash timer to prevent infinite dashing
			dashing = true;
		}

        if (Input.IsActionJustPressed("phase") && timeToPhase <= 0)
        {
			ApplyPhase();
			timeToPhase = phaseCooldown;
			phasing = true;
		}

		GD.Print("Time to phase: ", timeToPhase, ", phasing: ", phasing);
		
		MoveAndSlide();
		UpdateGlobalVars();
	}

    private void OnArea2DAreaEntered(Node2D area)
	{
		if (area.IsInGroup("enemy"))
		{
			Health--;
		}
	}
}