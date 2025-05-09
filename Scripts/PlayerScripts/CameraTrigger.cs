using Godot;
using System;

public partial class CameraTrigger : Area2D
{
    private void _on_area_entered(Node2D area)
    {
        if (area.IsInGroup("player"))
        {
            PlayerVars.Instance.CamPos = Position;
        }
    }
}