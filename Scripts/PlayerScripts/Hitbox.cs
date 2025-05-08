using Godot;
using System;

public partial class Hitbox : Area2D
{
    private void _OnArea2DAreaEntered(Area2D area)
    {
        if (area.IsInGroup("enemy"))
        {
            area.GetParent()?.GetParent()?.Free();
        }
    }
}