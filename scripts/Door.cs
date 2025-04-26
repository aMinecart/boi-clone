/*
Owen Colley
4/3/2025

Requires:
Marker2Ds to be places and labeled ("leftOpening", "rightOpening", "topOpening", "bottomOpening") at both openings
An Area2D in a circle around the door to automatically open on contact
*/

using Godot;

namespace boiclone.Scripts.DungeonScripts.Generation
{
	public partial class Door : Node
	{
		private CharacterBody2D topDoor;
		private CharacterBody2D bottomDoor;
		private Vector2 topDoorInitPos;
		private Vector2 bottomDoorInitPos;

		public override void _Ready()
		{
			topDoor = this.GetNode<CharacterBody2D>("topDoor");
			bottomDoor = this.GetNode<CharacterBody2D>("bottomDoor");
			topDoorInitPos = topDoor.Position;
			bottomDoorInitPos = bottomDoor.Position;
		}

		public override void _PhysicsProcess(double delta)
		{
			SlideDoors(slideRange: 50, shiftSpeed: 70);
		}

		private void SlideDoors(int slideRange, int shiftSpeed)
		{
			Vector2 upVel = new Vector2(0, -shiftSpeed);
			Vector2 downVel = new Vector2(0, shiftSpeed);

			if (IsOverlapping())
			{
				float topDoorMinY = topDoorInitPos.Y - slideRange;
				if (topDoor.Position.Y < topDoorMinY) return;
				topDoor.Velocity = upVel;
				bottomDoor.Velocity = downVel;
			}
			else
			{
				float topDoorMaxY = topDoorInitPos.Y;
				if (topDoor.Position.Y > topDoorMaxY) return;
				topDoor.Velocity = downVel;
				bottomDoor.Velocity = upVel;
			}

			topDoor.MoveAndSlide();
			bottomDoor.MoveAndSlide();
		}

		// returns if anything is overlapping with the passed scene's area2D
		private bool IsOverlapping()
		{
			Area2D area = GetNode<Area2D>("Area2D");
			return area.HasOverlappingBodies();
		}
	}
}
