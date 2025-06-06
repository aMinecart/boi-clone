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

	private Mode playerMode = Mode.Melee;

	private int TimeToAbility { get; set; } = 0;

	private int abilityCooldown = 90; // match to modeCooldownsDict value for playerMode variable
	private int abilityLength = 12;

	/*
	private int meleeCooldown = 180;
	private int rangeCooldown = 300;    
	private int projectileCooldown = 120;
	*/

	private readonly Dictionary<Mode, AbilityType> modeTypesDict = new() {
		{ Mode.Melee, AbilityType.Fixed },
		{ Mode.Ranged, AbilityType.Charge },
		{ Mode.Projectile, AbilityType.Fixed }
	};

	private readonly Dictionary<Mode, int> modeCooldownsDict = new() {
		{ Mode.Melee, 90 },
		{ Mode.Ranged, 300 },
		{ Mode.Projectile, 36 }
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

	/*private bool IsAttacking()
	{
		if (modeTypesDict[playerMode] == AbilityType.Fixed)
		{
			return TimeToAbility <= 0;
		}
		else
		{
			return TimeToAbility >= abilityCooldown;
		}
	}*/

	private void FireRanged()
	{
		PhysicsDirectSpaceState2D spaceState = GetWorld2D().DirectSpaceState;

		/*
		var query = PhysicsRayQueryParameters2D.Create(GlobalPosition, GetGlobalMousePosition());
		query.CollideWithAreas = true;

		var collisionInfo = spaceState.IntersectRay(query);
		if (collisionInfo == null)
		{
			break;
		}

		Node2D collision = (Node2D)collisionInfo["collider"];
		if (collision.GetParent() == null)
		{
			break;
		}

		collision.GetParent().CallDeferred(MethodName.Free);
		*/

		PhysicsRayQueryParameters2D query = PhysicsRayQueryParameters2D.Create(GlobalPosition, GetGlobalMousePosition(), collisionMask: 2);
		query.CollideWithAreas = true;

		var collisions = IntersectRays(query, spaceState);
		foreach (var collision in collisions)
		{
			// GD.Print((Node2D)collision["collider"]);
			Node2D collider = (Node2D)collision["collider"];

			collider.GetParent()?.GetParent()?.Free();
		}

		// GD.Print("Hitscan fired");
	}

	private void FireProjectile()
	{
		Node2D bullet = (Node2D)BulletScene.Instantiate();
		bullet.GlobalPosition = Prop.GlobalPosition;
		bullet.Rotation = Prop.GlobalRotation;
		
		enemyBulletParent = GetTree().GetRoot().GetNode<Node2D>("Dungeon/EnemyBulletParent");
		if (enemyBulletParent is null)
		{
			enemyBulletParent = GetTree().GetRoot().GetNode<Node2D>("TestScene/EnemyBulletParent");
		}
		
		enemyBulletParent.AddChild(bullet);
	}
	
	private void StartAbility(Mode abilityType)
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

		switch (abilityType)
		{
			case Mode.Melee:
				// enable hitbox for abilityCooldown frames
				Hitbox.Disabled = false;
				Item.Animation = "MeleeFire";
				break;

			case Mode.Ranged:
				Item.Animation = "RangedFire";
				break;

			case Mode.Projectile:
				// spawn bullet pea / child projectile
				FireProjectile();
				// GD.Print("IT WORKS");
				Item.Animation = "ProjectileFire";
				break;
		}
	}

	private void FinishAbility(Mode abilityType)
	{
		if (modeTypesDict[playerMode] == AbilityType.Charge && TimeToAbility < abilityCooldown)
		{
			TimeToAbility = 0;
			Item.Animation = "RangedIdle";
			return;
		}

		switch (abilityType)
		{
			case Mode.Melee:
				// disable hitbox
				Hitbox.Disabled = true;
				Item.Animation = "MeleeIdle";
				break;

			case Mode.Ranged:
				// use raycast to create attack
				FireRanged();
				Item.Animation = "RangedIdle";
				break;

			case Mode.Projectile:
				Item.Animation = "ProjectileIdle";
				break;
		}
	}

	private void SwitchAbility(Mode currentMode)
	{
		Mode updatedMode;

		if ((int)currentMode == Enum.GetValues(typeof(Mode)).Length - 1)
		{
			updatedMode = (Mode)0;
		}
		else
		{
			updatedMode = (Mode)((int)currentMode + 1);
		}

		abilityCooldown = modeCooldownsDict[updatedMode];

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

		// GD.Print($"switched: playerMode {playerMode}, abilityCooldown {abilityCooldown}, TimeToAbility {TimeToAbility}");
	}

	private void HandleTimers()
	{
		if (modeTypesDict[playerMode] == AbilityType.Fixed)
		{
			TimeToAbility--;
		}
		else
		{
			TimeToAbility++;
		}
	}

	public override void _Ready()
	{
		Prop = GetNode<Node2D>("SlashBox");
		Hitbox = GetNode<CollisionShape2D>("SlashBox/Area2D/CollisionShape2D");
		Item = GetNode<AnimatedSprite2D>("SlashBox/AnimatedSprite2D");
	}

	public override void _PhysicsProcess(double delta)
	{
		/* Item.Animation = playerMode.ToString() + (IsAttacking() ? "Idle" : "Fire");*/

		// change position of prop
		// Prop.Position = GetLocalMousePosition().Normalized() * 100;

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

		// check for mode switch
		if (Input.IsActionJustPressed("switch_mode"))
		{
			SwitchAbility(playerMode);
		}

		// check for lmb
		if (Input.IsActionJustPressed("ability"))
		{
			StartAbility(playerMode);
		}

		// check if any abilities need to complete
		if ((modeTypesDict[playerMode] == AbilityType.Charge && Input.IsActionJustReleased("ability")) ||
			(modeTypesDict[playerMode] == AbilityType.Fixed && TimeToAbility <= abilityCooldown - abilityLength))
		{
			FinishAbility(playerMode);
		}
		
		HandleTimers();

		// GD.Print(TimeToAbility);
		// GD.Print(Item.Animation);
		// GD.Print(Item.Frame);
		// GD.Print(!Hitbox.Disabled);
	}
}

/*
public class Ability
{
	public Mode Type {  get; set; }
	public bool ChargeRequired { get; set; }
	public int Cooldown { get; set; }
}
*/
