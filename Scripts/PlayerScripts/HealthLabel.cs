using Godot;
using System;

public partial class HealthLabel : RichTextLabel
{
    public override void _Process(double delta)
    {
        base._Process(delta);
        Text = PlayerVars.Instance.Health.ToString();
    }
}