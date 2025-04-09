using System;
using Godot;

public partial class PlayerMovement : CharacterBody2D
{
    private Vector2 InputDirection;

    [Export]
    public float Speed { get; set; } = 400.0f;

    private float Acceleration = 5.0f;

    private float friction = 40 * 4.0f; // amount the player slows down by each second (doubled when the player's desired move direction is equal to Vector2.zero) 

    private float calcRampup(float f)
    {
        return 2 * f / (f * f + 1);
    }

    private Vector2 accelerate(Vector2 currVel, Vector2 accelDir, float accelRate, float maxVel, float fixedDeltaTime)
    {
        if (accelDir == Vector2.Zero)
        {
            return currVel;
        }

        // multiplying the acceleration by the max velocity makes it so that the player accelerates up to max velocity in the same amount of time, not matter how big it is
        float accelX = accelDir.X * accelRate * maxVel * fixedDeltaTime;

        if (Mathf.Sign(currVel.X) == Mathf.Sign(accelDir.X))
        {
            // float speedToMaxSpeedRatio = Mathf.Clamp01((maxVel - Mathf.Abs(currVel.X)) / maxVel);
            accelX *= calcRampup(Mathf.Clamp((maxVel - Mathf.Abs(currVel.X)) / maxVel, 0, 1)); // lower acceleration as current X speed gets closer to max speed
        }
        // print(accelX);

        float accelY = accelDir.Y * accelRate * maxVel * fixedDeltaTime;

        if (Mathf.Sign(currVel.Y) == Mathf.Sign(accelDir.Y))
        {
            accelY *= calcRampup(Mathf.Clamp((maxVel - Mathf.Abs(currVel.Y)) / maxVel, 0, 1)); // lower acceleration as current Y speed gets closer to max speed
        }

        return currVel + new Vector2(accelX, accelY);
    }

    private Vector2 applyFriction(Vector2 currVel, Vector2 accelDir, float friction, float fixedDeltaTime)
    {
        // add scaling based on changes to maximum speed (maxSpeed is currently the max speed in the X or Y direction, not both)

        if (currVel.Length() == 0)
        {
            return currVel;
        }

        Vector2 newVel = currVel;

        float speed = currVel.Length();
        float angle = currVel.Angle();

        float reduction = friction * fixedDeltaTime;

        newVel.X -= (accelDir.X == 0 ? 4 : 1) * reduction * Mathf.Cos(angle);
        newVel.Y -= (accelDir.Y == 0 ? 4 : 1) * reduction * Mathf.Sin(angle);

        /* 
		 * multiply current velocity by new speed and divide by current speed to set magnitude equal to the new value (cannot subtract from the vector's magnitude) 
		 * currVel *= Mathf.Max(speed - reduction, 0) / speed;
		*/

        return newVel;
    }

    public void GetInput()
    {
        InputDirection = Input.GetVector("left", "right", "up", "down");
    }

    public void UpdateGlobalVars()
    {
        PlayerVars.Instance.Position = Position;
    }

    public override void _PhysicsProcess(double delta)
    {
        Velocity = applyFriction(Velocity, InputDirection, friction, (float)delta);
        Velocity = accelerate(Velocity, InputDirection, Acceleration, Speed, (float)delta);

        GetInput();
        MoveAndSlide();
        UpdateGlobalVars();
    }
}