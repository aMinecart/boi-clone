/*
Owen Colley
3/29/2025

TODO:
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
        private readonly List<PackedScene> _loadedBossRooms = [];
        private readonly RandomNumberGenerator _rng = new();
		private int _roomCounter = 0;
		private bool _needToAddBossRoom = true;
		private Vector2 _lastRoomPosition = Vector2.Zero;

        /// <summary>
        /// Inits random
        /// Sets the class lists to the scenes from the given links
        /// Begins the recursion algorith to make the dungeon
        /// </summary>
        public override void _Ready()
		{
			_rng.Randomize();

			SetScenesFromFolder("res://Scenes/DungeonScenes/Rooms/", _loadedRooms);
            SetScenesFromFolder("res://Scenes/DungeonScenes/BossRooms/", _loadedBossRooms);

            BeginGrowDungeon(position: Vector2.Zero, depthC: 15, scale: new Vector2(3.5f, 3.5f),
				roomName: "Square_all");
			SwapDeadEndRooms();

            GetNode<Node2D>("Player").GlobalPosition = _lastRoomPosition + new Vector2(500, 500);
        }

		/// <summary>sets passed list to all scenes from the specified path</summary>
		private static void SetScenesFromFolder(string folderPath, List<PackedScene> scenes)
		{
			DirAccess dir = DirAccess.Open(folderPath);
			if (dir == null)
			{
				GD.PrintErr("Please fix the path to room scenes found at the top of RoomManager script.");
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

		/// <summary>create spawn room and begin dungeon recursion</summary>
		private void BeginGrowDungeon(Vector2 position, int depthC, Vector2 scale, string roomName)
		{
            Room room = GD.Load<PackedScene>($"res://Scenes/DungeonScenes/Rooms/Square_all.tscn").Instantiate<Node2D>();
			room.Name = $"{room.Name}_{_roomCounter++}";
			room.Scale = scale;
			room.Position = position;
			GetNode<Node>("RoomParent").AddChild(room);

            IEnumerable<Door> doors = [.. room.GetChildren().OfType<Door>()];

            foreach (Door door in doors)
            {
                if (!GrowDungeon(position + door.Position * room.Scale, depthC - 1, door.Name, scale))
					door.Name = "deadEnd";
			}
        }

		/// <summary>Recursion algorithm to add connecting rooms to the dungeon</summary>
		private bool GrowDungeon(Vector2 position, int depthC, string prevDoorName, Vector2 scale)
		{
            if (depthC == 0 || GetRoomWithEntrance(depthC, prevDoorName) is not (Room room, Door entrance))
                return false;

			room.Scale = scale;
			position -= entrance.Position * room.Scale;
			room.Position = position;

			if (IsOverlappingAnyRoom(room))
				return false;

            room.Name = $"{room.Name}_{_roomCounter++}";
            GetNode<Node>("RoomParent").AddChild(room);

            IEnumerable<Door> doors = [.. room.GetChildren().OfType<Door>()];

			foreach (Door door in doors)
			{
				if (door.Name == entrance.Name) continue;

				if (!GrowDungeon(position + door.Position * room.Scale, depthC - 1, door.Name, scale))
					door.Name = "deadEnd";
			}

			_lastRoomPosition = position;

            return true;
		}

		/// <summary>get a working scene and its entrance door</summary>
		/// <returns>room and door or null if no possible options are found</returns>
		private (Room room, Door entrance)? GetRoomWithEntrance(int depthC, string prevDoorName)
		{
			List<Room> rooms = [.. (_needToAddBossRoom ? _loadedBossRooms : _loadedRooms)
				.Select(scene => scene.Instantiate<Room>())];
            _needToAddBossRoom = false;
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
				if (doorName != prevDoorName && room.GetNodeOrNull<Door>(doorName) is Door door)
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
			Room deadEnd = GD.Load<PackedScene>("res://Scenes/DungeonScenes/deadEnd.tscn").Instantiate<Room>();
			deadEnd.Position = position;
			AddChild(deadEnd);
		}

		/// <summary>returns if any area2D is overlapping with the passed scene's area2D</summary>
		/// <returns>bool, true if there's any overlap</returns>
		private bool IsOverlappingAnyRoom(Room currRoom)
		{
			Node RoomParent = GetNode<Node>("RoomParent");
            IEnumerable<Room> rooms = [.. RoomParent.GetChildren().OfType<Room>()];

			foreach (Room room in rooms)
			{
				if (room != currRoom && IsColliding(currRoom, room)) return true;
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
			float myRight = room1.Position.X + sprite1.Texture.GetWidth() * room1.Scale.X;
			float myTop = room1.Position.Y;
			float myBottom = room1.Position.Y + sprite1.Texture.GetHeight() * room1.Scale.Y;

			float otherLeft = room2.Position.X;
			float otherRight = room2.Position.X + sprite2.Texture.GetWidth() * room2.Scale.X;
			float otherTop = room2.Position.Y;
			float otherBottom = room2.Position.Y + sprite2.Texture.GetHeight() * room2.Scale.Y;

			const int padding = 5;

			bool crash = !(myBottom - padding < otherTop || myTop + padding > otherBottom
				|| myRight - padding < otherLeft || myLeft + padding > otherRight);
			return crash;
		}

        /// <summary>swap out rooms with dead end markers with one that doesn't use those sides</summary>
        private void SwapDeadEndRooms()
		{
			Node RoomParent = GetNode<Node>("RoomParent");
            List<Room> roomsToSwap = [.. RoomParent.GetChildren().OfType<Room>()];

            foreach (Room oldRoom in roomsToSwap)
			{
                IEnumerable<Door> doors = [.. oldRoom.GetChildren().OfType<Door>()];

				bool hasDeadEnd = doors.Any(door => door.Name == "deadEnd");
				if (!hasDeadEnd) continue;

                IEnumerable<Door> usedDoors = [.. doors.Where(door => door.Name != "deadEnd")];

				string oldRoomName = oldRoom.Name;
                int underscoreIndex = oldRoomName.IndexOf('_');
				string pathStart = "res://Scenes/DungeonScenes/Rooms/" +
					oldRoomName.Substring(0, underscoreIndex + 1);

				if (underscoreIndex < 0) GD.PrintErr($"Cannot find underscore in room name: {oldRoomName}");

                Room? newRoom = GetRoomFromDoors(usedDoors, pathStart);
				if (newRoom != null) SwapRooms(RoomParent, oldRoom, newRoom);
			}
		}

		/// <summary>swaps two rooms on the parent while keeping the settings the same</summary>
		private void SwapRooms(Node RoomParent, Room oldRoom, Room newRoom)
		{
			newRoom.GlobalTransform = oldRoom.GlobalTransform;
            RoomParent.AddChild(newRoom);
            RoomParent.RemoveChild(oldRoom);
			oldRoom.QueueFree();
		}

		/// <summary>gets a room that has exactly the same doors as passed in</summary>
		/// <returns>room if it finds one, null no match found</returns>
		private static Room? GetRoomFromDoors(IEnumerable<Door> doors, string pathStart)
		{
            List<string> activeDoors = doors.Select(door => (string)door.Name)
				.Intersect(new[] { "topDoor", "bottomDoor", "leftDoor", "rightDoor" })
				.OrderBy(name => {
                    switch (name)
                    {
                        case "topDoor":
                            return 0;
                        case "bottomDoor":
                            return 1;
                        case "leftDoor":
                            return 2;
                        case "rightDoor":
                            return 3;
						default: return int.MaxValue;
                    }
                })
				.ToList();

            string pathEnd = string.Join("", activeDoors.Select(name => name.Replace("Door", "")));
			string path = pathStart + pathEnd + ".tscn";

			PackedScene roomPacked = GD.Load<PackedScene>(path);
			if (roomPacked == null) {
				GD.PrintErr($"Room swap not found for path: {path}");
                return null;
			}

            return roomPacked.Instantiate<Room>();
		}
	}
}
