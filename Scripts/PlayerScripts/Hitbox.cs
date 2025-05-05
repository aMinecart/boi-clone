using Godot;
using System;

public partial class Hitbox : Area2D
{
    public void _OnArea2DAreaEntered(Area2D area)
    {
        if (area.CollisionLayer == 2)
        {
            area.GetParent()?.GetParent()?.Free();
        }
    }
}