using Godot;
using System;
using System.Collections.Generic;

public enum AbilityType
{
    Fixed,
    Charge/*,
    Variable*/
}

public enum Mode
{
    Melee,
    Ranged,
    Projectile
}

public partial class PlayerAbilities : Node2D
{
    [Export] public PackedScene BulletScene { get; set; }

    private Node2D Prop { get; set; }
    private CollisionShape2D Hitbox { get; set; }
    private AnimatedSprite2D Item { get; set; }

    private Polygon2D Beam { get; set; }

    private AnimatedSprite2D ReloadBar { get; set; }
    private Timer ReloadTimer { get; set; }

    private Mode playerMode = Mode.Melee;

    private int TimeToAbility { get; set; } = 0;

    private int abilityCooldown = 30; // match to modeCooldownsDict value for playerMode variable
    private int abilityLength = 12;

    private float abilityAmmo = Mathf.Inf; // match to modeAmmoCountsDict value for playerMode variable
    private float[] ammoValues = [Mathf.Inf, 5, 7];// match to modeCooldownsDict values (Modes)0, (Modes)1, and (Modes)2


    private readonly Dictionary<Mode, AbilityType> modeTypesDict = new() {
        { Mode.Melee, AbilityType.Fixed },
        { Mode.Ranged, AbilityType.Charge },
        { Mode.Projectile, AbilityType.Fixed }
    };

    private readonly Dictionary<Mode, int> modeCooldownsDict = new() {
        { Mode.Melee, 30 },
        { Mode.Ranged, 120 },
        { Mode.Projectile, 45 }
    };

    private readonly Dictionary<Mode, float> modeReloadTimesDict = new() {
        { Mode.Melee, 0 },
        { Mode.Ranged, 4 },
        { Mode.Projectile, 3f }
    };

    private readonly Dictionary<Mode, float> modeAmmoCountsDict = new() {
        { Mode.Melee, Mathf.Inf },
        { Mode.Ranged, 5 },
        { Mode.Projectile, 7 }
    };

    private static Godot.Collections.Array<Godot.Collections.Dictionary> IntersectRays(PhysicsRayQueryParameters2D query, PhysicsDirectSpaceState2D spaceState, int maxIterations = 25)
    {
        Godot.Collections.Array<Godot.Collections.Dictionary> hits = [];
        Godot.Collections.Array<Rid> exculsions = [];

        var hit = spaceState.IntersectRay(query);
        int iterations = 1;

        while (hit.Count != 0 && iterations <= maxIterations)
        {
            hits.Add(hit);
            exculsions.Add((Rid)hit["rid"]);
            query = PhysicsRayQueryParameters2D.Create(query.From, query.To, query.CollisionMask, exculsions);
            query.CollideWithAreas = true;

            hit = spaceState.IntersectRay(query);
            iterations++;
        }

        return hits;
    }

    private void FireRanged()
    {
        PhysicsDirectSpaceState2D spaceState = GetWorld2D().DirectSpaceState;

        PhysicsRayQueryParameters2D query = PhysicsRayQueryParameters2D.Create(GlobalPosition, GetGlobalMousePosition(), collisionMask: 2);
        query.CollideWithAreas = true;

        var collisions = IntersectRays(query, spaceState);
        foreach (var collision in collisions)
        {
            Node2D collider = (Node2D)collision["collider"];

            collider.GetParent()?.GetParent()?.Free();
        }

        Polygon2D laser = (Polygon2D)Beam.Duplicate();
        laser.GlobalTransform = Beam.GlobalTransform;
        laser.Visible = true;

        laser.ProcessMode = ProcessModeEnum.Inherit;
        GetTree().GetRoot().AddChild(laser);
    }

    private void FireProjectile()
    {
        Node2D bullet = (Node2D)BulletScene.Instantiate();
        bullet.GlobalPosition = Prop.GlobalPosition;
        bullet.Rotation = Prop.GlobalRotation;

        GetTree().GetRoot().GetNode("TestScene/EnemyBulletParent").AddChild(bullet);
    }

    private void StartReload()
    {
        float reloadTime = modeReloadTimesDict[playerMode];
        ReloadTimer.Start(reloadTime);
        ReloadBar.Animation = "Active";
        ReloadBar.SpeedScale = ReloadBar.SpriteFrames.GetFrameCount("Active") / reloadTime;
    }

    private void StartAbility(Mode ability)
    {
        if (modeTypesDict[playerMode] == AbilityType.Fixed)
        {
            if (TimeToAbility > 0)
            {
                return;
            }

            TimeToAbility = abilityCooldown;
        }
        else
        {
            TimeToAbility = 0;
        }

        switch (ability)
        {
            case Mode.Melee:
                // enable hitbox for abilityCooldown frames
                Hitbox.Disabled = false;
                Item.Animation = "MeleeFire";
                break;

            case Mode.Ranged:
                if (abilityAmmo > 0)
                {
                    Item.Animation = "RangedFire";
                }
                break;

            case Mode.Projectile:
                // spawn bullet pea / child projectile
                if (abilityAmmo > 0)
                {
                    FireProjectile();
                    abilityAmmo--;

                    Item.Animation = "ProjectileFire";
                }
                break;
        }
    }

    private void FinishAbility(Mode ability)
    {
        if (modeTypesDict[playerMode] == AbilityType.Charge && TimeToAbility < abilityCooldown)
        {
            TimeToAbility = 0;
            Item.Animation = "RangedIdle";
            return;
        }

        switch (ability)
        {
            case Mode.Melee:
                // disable hitbox
                Hitbox.Disabled = true;
                Item.Animation = "MeleeIdle";
                break;

            case Mode.Ranged:
                // use raycast to create attack
                if (abilityAmmo > 0)
                {
                    FireRanged();
                    abilityAmmo--;
                }

                TimeToAbility = 0;
                Item.Animation = "RangedIdle";
                break;

            case Mode.Projectile:
                Item.Animation = "ProjectileIdle";
                break;
        }
    }

    private void CancelAbility(Mode ability)
    {
        if (modeTypesDict[ability] == AbilityType.Fixed)
        {
            TimeToAbility = abilityCooldown;
        }
        else /* if (modeTypesDict[updatedMode] == AbilityType.Charge || modeTypesDict[updatedMode] == AbilityType.Variable) */
        {
            TimeToAbility = 0;
        }

        switch (ability)
        {
            case Mode.Melee:
                // disable hitbox
                Hitbox.Disabled = true;
                Item.Animation = "MeleeIdle";
                break;

            case Mode.Ranged:
                Item.Animation = "RangedIdle";
                break;

            case Mode.Projectile:
                Item.Animation = "ProjectileIdle";
                break;
        }
    }

    private void SwitchAbility(Mode currentMode, bool invert = false)
    {
        Mode updatedMode;

        if (!invert)
        {
            if ((int)currentMode == Enum.GetValues(typeof(Mode)).Length - 1)
            {
                updatedMode = (Mode)0;
            }
            else
            {
                updatedMode = (Mode)((int)currentMode + 1);
            }
        }
        else
        {
            if ((int)currentMode == 0)
            {
                updatedMode = (Mode)(Enum.GetValues(typeof(Mode)).Length - 1);
            }
            else
            {
                updatedMode = (Mode)((int)currentMode - 1);
            }
        }

        abilityCooldown = modeCooldownsDict[updatedMode];

        ammoValues[(int)playerMode] = abilityAmmo; // record current mode's ammo count
        abilityAmmo = ammoValues[(int)updatedMode]; // restore updated mode's ammo count
        // abilityAmmo = modeAmmoCountsDict[updatedMode];

        if (modeTypesDict[updatedMode] == AbilityType.Fixed)
        {
            TimeToAbility = abilityCooldown;
        }
        else /* if (modeTypesDict[updatedMode] == AbilityType.Charge || modeTypesDict[updatedMode] == AbilityType.Variable) */
        {
            TimeToAbility = 0;
        }

        switch (updatedMode)
        {
            case Mode.Melee:
                Item.Animation = "MeleeIdle";
                break;

            case Mode.Ranged:
                Item.Animation = "RangedIdle";
                break;

            case Mode.Projectile:
                Item.Animation = "ProjectileIdle";
                break;
        }

        playerMode = updatedMode;
        Hitbox.Disabled = true; // check that attack hitbox is not still active
    }

    private void HandleTimers()
    {
        AbilityType abilityMode = modeTypesDict[playerMode];

        if (abilityMode == AbilityType.Fixed && TimeToAbility > 0)
        {
            TimeToAbility--;
        }
        else if (abilityMode == AbilityType.Charge && Input.IsActionPressed("use_ability"))
        {
            TimeToAbility++;
        }
    }

    public void UpdateGlobalVars()
    {
        PlayerVars.Instance.Ammo = abilityAmmo;
    }

    public override void _Ready()
    {
        Prop = GetNode<Node2D>("SlashBox");
        Hitbox = GetNode<CollisionShape2D>("SlashBox/ItemArea/ItemHitbox");
        Item = GetNode<AnimatedSprite2D>("SlashBox/ItemSprite");
        Beam = GetNode<Polygon2D>("SlashBox/Beamer");
        ReloadBar = GetNode<AnimatedSprite2D>("ReloadBar");
        ReloadTimer = GetNode<Timer>("ReloadTimer");

    }

    public override void _PhysicsProcess(double delta)
    {
        // change rotation of prop to follow the player's mouse
        Prop.Rotation = GetLocalMousePosition().Angle();

        if (Mathf.Abs(Mathf.RadToDeg(Prop.Rotation)) > 90)
        {
            Prop.Scale = new Vector2(0.9f, -0.9f);
        }
        else
        {
            Prop.Scale = new Vector2(0.9f, 0.9f);
        }

        // check for reload
        if (Input.IsActionJustPressed("reload") && abilityAmmo != modeAmmoCountsDict[playerMode]/* && maxAmmo != Mathf.Inf*/)
        {
            CancelAbility(playerMode);
            StartReload();
        }

        // enable starting/switching abilities while not reloading
        if (ReloadTimer.TimeLeft == 0)
        {
            // check for mode switch
            if (Input.IsActionJustPressed("switch_mode"))
            {
                SwitchAbility(playerMode, false);
            }
            else if (Input.IsActionJustPressed("reverse_switch_mode"))
            {
                SwitchAbility(playerMode, true);
            }

            // check for ability input
            if (Input.IsActionJustPressed("use_ability"))
            {
                StartAbility(playerMode);
            }
        }

        // check if any abilities need to complete
        if ((modeTypesDict[playerMode] == AbilityType.Charge && Input.IsActionJustReleased("use_ability")) ||
            (modeTypesDict[playerMode] == AbilityType.Fixed && TimeToAbility <= abilityCooldown - abilityLength))
        {
            FinishAbility(playerMode);
        }

        UpdateGlobalVars();
        HandleTimers();

        GD.Print(TimeToAbility);
        // GD.Print($"Animation: {Item.Animation}; Frame: {Item.Frame}");
    }

    private void _OnReloadTimerTimeout()
    {
        abilityAmmo = modeAmmoCountsDict[playerMode];

        ReloadBar.Animation = "Idle";
        ReloadBar.SpeedScale = 1;
    }
}