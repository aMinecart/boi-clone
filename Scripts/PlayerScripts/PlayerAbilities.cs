using Godot;
using System;

public enum Mode
{
    Melee,
    Ranged,
    Projectile
}

public partial class PlayerAbilities : Node2D
{
    private Node2D Prop { get; set; }
    private CollisionShape2D Hitbox { get; set; }

    private Mode playerMode = Mode.Melee;

    private int TimeToAbility { get; set; } = 0;

    private int abilityTimer = 180;
    private int abilityLength = 12;

    private int meleeCooldown = 180;
    private int rangeCooldown = 300;
    private int projectileCooldown = 120;

    private void UseAbility(Mode abilityType)
    {
        switch (abilityType)
        {
            case Mode.Melee:
                // change position of prop
                Prop.Position = GetLocalMousePosition().Normalized() * 100;
                
                // enable hitbox for abilityTimer frames
                Hitbox.Disabled = false;
                TimeToAbility = abilityTimer;
                break;

            case Mode.Ranged:

                TimeToAbility = 0;
                break;

            case Mode.Projectile:

                TimeToAbility = abilityTimer;
                break;
        }
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

    private void SwitchAbility(Mode abilityType)
    {
        switch (abilityType)
        {
            case Mode.Melee:
                // change to the next player ability
                playerMode = Mode.Ranged;

                // change cooldown to match the new mode
                abilityTimer = meleeCooldown;
                TimeToAbility = 0;
                break;

            case Mode.Ranged:
                // change to the next player ability
                playerMode = Mode.Projectile;

                // change cooldown to match the new mode
                abilityTimer = projectileCooldown;
                TimeToAbility = 60;
                break;

            case Mode.Projectile:
                // change to the next player ability
                playerMode = Mode.Melee;

                // change cooldown to match the new mode
                abilityTimer = meleeCooldown;
                TimeToAbility = 60;
                break;

            default:
                GD.PushWarning($"Player mode {playerMode} does not have a mode to switch into");
                break;
        }
    }

    private void HandleTimers()
    {
        switch (playerMode)
        {
            case Mode.Melee:
            case Mode.Projectile:
                TimeToAbility--;
                break;

            case Mode.Ranged:
                TimeToAbility++;
                break;
        }
        
    }

    public override void _Ready()
    {
        Prop = GetNode<Node2D>("SlashBox");
        Hitbox = GetNode<CollisionShape2D>("SlashBox/Area2D/CollisionShape2D");
    }

    public override void _PhysicsProcess(double delta)
    {
        // check for mode switch
        if (Input.IsActionJustPressed("switch_mode"))
        {
            SwitchAbility(playerMode);
        }

        // check for lmb
        if (Input.IsActionJustPressed("ability"))
        {
            UseAbility(playerMode);
        }

        if (Input.IsActionJustReleased("ability"))
        {
            ReleaseAbility(playerMode);
        }

        if (TimeToAbility <= abilityTimer - abilityLength)
        {
            EndAbility(playerMode);
        }

        // check if hitbox needs disabling
        HandleTimers();

        GD.Print(!Hitbox.Disabled);
    }
}