using Godot;
using System;

public partial class CameraController : Camera2D
{
    public override void _Ready()
    {
        MakeCurrent();
    }
    
    public override void _Process(double delta)
    {
        Position = PlayerVars.Instance.Position;
    }
}