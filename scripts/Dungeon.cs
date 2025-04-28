/*
Owen Colley
3/29/2025

Requires:
Folder paths to be updated for room and hallway scenes
Marker2Ds to be places and labeled ("leftDoor", "rightDoor", "topDoor", "bottomDoor") at every door
Rooms and hallways need collisionShapes to see if they're overlapping

TODO:
Fix the IsOverlapping function
Add special rooms like entrance exit
add chests and random enemy spawns
*/

using Godot;
using System.Collections.Generic;
using System.Linq;

namespace boiclone.Scripts.DungeonScripts
{
	using Door = Marker2D;
	using Room = Node2D;

	public partial class Dungeon : Room
	{
		private readonly List<PackedScene> _loadedRooms = [];
		private readonly List<PackedScene> _loadedHallways = [];
		private readonly RandomNumberGenerator _rng = new();
		private int _roomC = 0;

		/// <summary>
		/// Inits random
		/// Sets the class lists to the scenes from the given links
		/// Begins the recursion algorith to make the dungeon
		/// </summary>
		public override void _Ready()
		{
			_rng.Randomize();

			SetScenesFromFolder("res://Scenes/DungeonScenes/Rooms/", _loadedRooms);
			SetScenesFromFolder("res://Scenes/DungeonScenes/Hallways/", _loadedHallways);

			GrowTreeDungeon(position: new Vector2(600, 300), depthC: 5, prevDoorName: "leftDoor");
		}

		/// <summary>sets passed list to all scenes from the specified path</summary>
		private static void SetScenesFromFolder(string folderPath, List<PackedScene> scenes)
		{
			DirAccess dir = DirAccess.Open(folderPath);
			if (dir == null)
			{
				GD.PrintErr("Please fix the path to room and/or hallway scenes found at the top of RoomManager script");
				return;
			}

			dir.ListDirBegin();

			string fileName;
			while ((fileName = dir.GetNext()) != "") // loop while files exist
			{
				PackedScene scene = GD.Load<PackedScene>(folderPath + fileName);
				scenes.Add(scene);
			}

			dir.ListDirEnd();
		}

		/// <summary>Recursion algorithm to add connecting rooms to the dungeon</summary>
		private void GrowTreeDungeon(Vector2 position, int depthC, string prevDoorName)
		{
			if (depthC == 0 || GetRoomWithEntrance(depthC, prevDoorName) is not (Room room, Door entrance))
			{
				AddDeadEnd(position);
				return;
			}

			position -= entrance.Position;
			room.Position = position;

			if (IsOverlapping(room))
			{
				AddDeadEnd(position + entrance.Position);
				return;
			}

			AddChild(room);

			IEnumerable<Door> doors = [.. room.GetChildren().OfType<Marker2D>()];

			foreach (Door door in doors)
			{
				if (door.Name == entrance.Name) continue;

				_roomC++;
				GrowTreeDungeon(position + door.Position, depthC - 1, door.Name);
			}
		}

		/// <summary>get a working scene and its entrance door</summary>
		/// <returns>room and door or null if no possible options are found</returns>
		private (Room room, Door entrance)? GetRoomWithEntrance(int depthC, string prevDoorName)
		{
			List<Room> rooms = (depthC % 2 == 0)
				? [.. _loadedRooms.Select(scene => scene.Instantiate<Room>())]
				: [.. _loadedHallways.Select(scene => scene.Instantiate<Room>())];
			List<Room> shuffledRooms = [.. rooms.OrderBy(_ => _rng.Randf())];

			foreach (Room room in shuffledRooms)
			{
				if (GetOppDoor(room, prevDoorName) is Door door)
					return (room, door);
			}

			return null;
		}

		/// <summary>get a random door other than prev door</summary>
		/// <returns>Door or null if no possible options are found</returns>
		private Door GetRandDoor(Room room, string prevDoorName)
		{
			List<string> doorNames = ["leftDoor", "rightDoor", "topDoor", "bottomDoor"];
			List<string> shuffledDoorNames = [.. doorNames.OrderBy(_ => _rng.Randf())];

			foreach (string doorName in shuffledDoorNames)
			{
				if (doorName == prevDoorName) continue;

				if (room.GetNodeOrNull<Door>(doorName) is Door door)
					return door;
			}

			return null;
		}

		/// <summary>get opposite door to passed doorName</summary>
		/// <returns>Door or null if opposite door not found</returns>
		private static Door GetOppDoor(Room room, string doorName)
		{
			string oppDoorName;
			if (doorName == "leftDoor") oppDoorName = "rightDoor";
			else if (doorName == "rightDoor") oppDoorName = "leftDoor";
			else if (doorName == "topDoor") oppDoorName = "bottomDoor";
			else if (doorName == "bottomDoor") oppDoorName = "topDoor";
			else return null;

			return room.GetNodeOrNull<Marker2D>(oppDoorName);
		}

		/// <summary>add a wall at a passed position</summary>
		private void AddDeadEnd(Vector2 position)
		{
			Room deadEnd = GD.Load<PackedScene>("res://Scenes/DungeonScenes/DeadEnd.tscn").Instantiate<Room>();
			deadEnd.Position = position;
			AddChild(deadEnd);
		}

		/// <summary>returns if any area2D is overlapping with the passed scene's area2D</summary>
		/// <returns>bool, true if there's any overlap</returns>
		private bool IsOverlapping(Room currRoom)
		{
			IEnumerable<Room> rooms = GetChildren().OfType<Room>();

			foreach (Room room in rooms)
			{
				if (currRoom != room && IsColliding(currRoom, room)) return true;
			}
			return false;
		}

		/// <summary>returns if the 2 rooms have an overlapping area2D</summary>
		/// <returns>bool, true if there's any overlap</returns>
		private static bool IsColliding(Room room1, Room room2)
		{
			Sprite2D sprite1 = room1.GetNodeOrNull<Sprite2D>("room");
			Sprite2D sprite2 = room2.GetNodeOrNull<Sprite2D>("room");

			if (sprite1 == null || sprite2 == null || sprite1.Texture == null || sprite2.Texture == null)
			{
				GD.PrintErr("Can't find Sprite2D named 'room' with a valid texture in one or both rooms.");
				return false;
			}

			float myLeft = room1.Position.X;
			float myRight = room1.Position.X + sprite1.Texture.GetWidth();
			float myTop = room1.Position.Y;
			float myBottom = room1.Position.Y + sprite1.Texture.GetHeight();

			float otherLeft = room2.Position.X;
			float otherRight = room2.Position.X + sprite2.Texture.GetWidth();
			float otherTop = room2.Position.Y;
			float otherBottom = room2.Position.Y + sprite2.Texture.GetHeight();

			const int padding = 20;

			bool crash = !(myBottom - padding < otherTop || myTop + padding > otherBottom
				|| myRight - padding < otherLeft || myLeft + padding > otherRight);
			return crash;
		}
	}
}
