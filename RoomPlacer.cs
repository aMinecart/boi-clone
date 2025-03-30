using Godot;
using System;
using System.Collections.Generic;

public class RoomPlacer
{
	private Dungeon _dungeon;
	private RoomManager _roomManager;

	public RoomPlacer(Dungeon dungeon, RoomManager roomManager)
	{
		_dungeon = dungeon;
		_roomManager = roomManager;
	}

	// add scenes alternating between rooms and hallways
	public void SetUpDungeon(int roomCount) {
		Vector2 position = new Vector2(100, 200);
		string prevDoorName = "rightDoor";
		for (int i = 0; i < roomCount; i++)
		{
			List<PackedScene> scenes = (i % 2 == 0) ? _roomManager.loadedRooms : _roomManager.loadedHallways;
			(position, prevDoorName) = AddRandomRoom(scenes, position, prevDoorName);
		}
	}

	// add room to dungeon in random direction
	private (Vector2 position, string prevDoorName) AddRandomRoom(List<PackedScene> scenes, Vector2 position, string prevDoorName)
	{
		Node2D scene;
		Marker2D entranceDoor;
		Marker2D exitDoor;

		while (true)
		{
			(scene, entranceDoor) = _roomManager.GetRoomWithEntrance(scenes, prevDoorName);
			position -= entranceDoor.Position;

			scene.Position = position;
			_dungeon.AddChild(scene);

			if (IsOverlapping(scene))
			{
				_dungeon.RemoveChild(scene);
				position += entranceDoor.Position;
				continue;
			}

			exitDoor = _roomManager.GetRandomDoor(scene, prevDoorName: entranceDoor.Name);
			position += exitDoor.Position;

			return (position, prevDoorName: exitDoor.Name);
		}
	}

	// returns if any area2D is overlapping with the passed scene's area2D
	private bool IsOverlapping(Node2D scene)
	{
		Area2D area = scene.GetNode<Area2D>("Area2D");
		return area.HasOverlappingAreas();
	}
}
