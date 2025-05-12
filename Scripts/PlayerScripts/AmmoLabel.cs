using Godot;
using System;

public partial class AmmoLabel : RichTextLabel
{
    public override void _Process(double delta)
    {
        base._Process(delta);
        Text = PlayerVars.Instance.Ammo.ToString();
    }
}