using Godot;
using System;

public partial class CameraController : Camera2D
{
    public enum CameraMode {
	    Follow = 0,
        Lock = 1,
	}
    CameraMode cameraMode = CameraMode.Lock;
    public override void _Ready()
    {
        MakeCurrent();
    }
    
    public override void _Process(double delta)
    {
        if(cameraMode == CameraMode.Lock) Position = Position.MoveToward(PlayerVars.Instance.CamPos, 150);
        else if(cameraMode == CameraMode.Follow) Position = Position.MoveToward(PlayerVars.Instance.Position, 150);
    }
    private void _on_area_entered(Node2D area)
    {
        if (area.IsInGroup("lock")) cameraMode = CameraMode.Lock;
        else if (area.IsInGroup("follow")) cameraMode = CameraMode.Follow;
    }
}