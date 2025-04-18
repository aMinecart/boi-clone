using Godot;
using System;

public enum Orientation
{
    North,
    Northwest,
    Northeast,
    South,
    Southwest,
    Southeast
}

public enum Action
{
    Idle,
    Running
}

public partial class PlayerAnimation : AnimatedSprite2D
{
    private PlayerMovement player;
    private Action playerState;

    private Vector2 mousePos;
    private Orientation lookPos;

    public override void _Ready()
    {
        player = GetParent<PlayerMovement>();
    }

    public override void _Process(double delta)
    {
        mousePos = GetLocalMousePosition();

        if (player.Velocity.Length() > 0)
        {
            playerState = Action.Running;

            // only change the direction the player is facing if the player is moving
            switch (Mathf.RadToDeg(player.Velocity.Angle()))
            {
                case >= -180 and < -120:
                    lookPos = Orientation.Northwest;
                    break;
                case >= -120 and < -60:
                    lookPos = Orientation.North;
                    break;
                case >= -60 and < 0:
                    lookPos = Orientation.Northeast;
                    break;
                case >= 0 and < 60:
                    lookPos = Orientation.Southeast;
                    break;
                case >= 60 and < 120:
                    lookPos = Orientation.South;
                    break;
                case >= 120 and <= 180:
                    lookPos = Orientation.Southwest;
                    break;
                default:
                    GD.PushWarning("Player orientation did not change, angle was: ", Mathf.RadToDeg(player.Velocity.Angle()));
                    break;
            }
        }
        else
        {
            playerState = Action.Idle;
        }

        Animation = playerState.ToString() + lookPos.ToString();
    }
}