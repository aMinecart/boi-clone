using Godot;
using System;

public enum Mode
{
    Melee,
    Ranged
}

public partial class PlayerAbilities : Node2D
{
    private Node2D Prop { get; set; }
    private CollisionShape2D Hitbox { get; set; }

    private Mode playerMode = Mode.Melee;

    private int TimeToAbility { get; set; } = 0;
    
    private int abilityCooldown = 60;
    private int abilityLength = 2;
    
    private void UseAbility(Mode abilityType)
    {
        switch (abilityType)
        {
            case Mode.Melee:
                // change position of prop
                Prop.Position = GetLocalMousePosition().Normalized() * 100;

                // enable hitbox for abilityCooldown frames
                Hitbox.Disabled = false;
                break;
        }

        TimeToAbility = abilityCooldown;
    }

    private void EndAbility(Mode abilityType)
    {
        switch (abilityType)
        {
            case Mode.Melee:
                // disable hitbox
                Hitbox.Disabled = true;
                break;
        }
    }

    public override void _Ready()
    {
        Prop = GetNode<Node2D>("CharacterBody2D/SlashBox");
        Hitbox = GetNode<CollisionShape2D>("CharacterBody2D/SlashBox/Area2D/CollisionShape2D");
    }

    public override void _PhysicsProcess(double delta)
    {
        if (TimeToAbility > 0)
        {
            TimeToAbility--;
        }

        // check for mode switch
        if (Input.IsActionJustPressed("switch_mode"))
        {
            switch (playerMode)
            {
                case Mode.Melee:
                    playerMode = Mode.Ranged;
                    break;

                case Mode.Ranged:
                    playerMode = Mode.Melee;
                    break;

                default:
                    GD.PushWarning($"Player mode {playerMode} does not have a mode to switch into");
                    break;
            }
        }

        // check for lmb
        if (Input.IsActionJustPressed("ability"))
        {
            UseAbility(playerMode);
        }

        // check if hitbox needs disabling
        if (TimeToAbility < abilityCooldown - abilityLength)
        {
            EndAbility(playerMode);
        }
    }
}