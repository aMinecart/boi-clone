using Godot;
using System;

public partial class Beamer : Polygon2D
{
    public override void _Process(double delta)
    {
        SelfModulate = new Color(SelfModulate, SelfModulate.A - (float)delta);

        if (SelfModulate.A <= 0)
        {
            Free();
        }
    }
}