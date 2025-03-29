/*
Owen Colley
3/29/2025

Requires:
Folder paths to be updated for room and hallway scenes
Marker2Ds to be places and labeled (like "leftDoor") at every door
Rooms and hallways need collisionShapes to see if they're overlapping

TODO:
Fix the IsOverlapping function
Add special rooms like entrance exit
add chests and random enemy spawns
*/

using Godot;
using System;
using System.Collections.Generic;

public partial class Dungeon : Node2D
{
	[Export] public string FolderPathRooms = "res://scenes/rooms/";
	[Export] public string FolderPathHallways = "res://scenes/hallways/";
	
	private List<PackedScene> _loadedRooms = new List<PackedScene>();
	private List<PackedScene> _loadedHallways = new List<PackedScene>();
	
	private RandomNumberGenerator _rng = new RandomNumberGenerator();

	// load scenes from folders into list and set up dungeon
	public override void _Ready()
	{
		_rng.Randomize();
		
		SetScenesFromFolder(FolderPathRooms, ref _loadedRooms);
		SetScenesFromFolder(FolderPathHallways, ref _loadedHallways);

		SetUpDungeon(roomCount: 10);
	}

	// sets passed list to all scenes from the specified path
	private void SetScenesFromFolder(string folderPath, ref List<PackedScene> scenes)
	{
		DirAccess dir = DirAccess.Open(folderPath);
		
		dir.ListDirBegin();
		string fileName;
		while ((fileName = dir.GetNext()) != "") { // loop while files exist
			PackedScene scene = GD.Load<PackedScene>(folderPath + fileName);
			scenes.Add(scene);
		}
		dir.ListDirEnd();
	}

	// add scenes alternating between rooms and hallways
	private void SetUpDungeon(int roomCount) {
		Vector2 position = new Vector2(100, 200);
		string prevDoorName = "rightDoor";
		for (int i = 0; i < roomCount; i++)
		{
			List<PackedScene> scenes = (i % 2 == 0) ? _loadedRooms : _loadedHallways;
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
			(scene, entranceDoor) = GetRoomWithEntrance(scenes, prevDoorName);
			position -= entranceDoor.Position;

			scene.Position = position;
			AddChild(scene);

			if (IsOverlapping(scene))
			{
				RemoveChild(scene);
				position += entranceDoor.Position;
				continue;
			}

			exitDoor = GetRandomDoor(scene, prevDoorName: entranceDoor.Name);
			position += exitDoor.Position;

			return (position, prevDoorName: exitDoor.Name);
		}
	}

	// returns a working scene and its entrance door
	private (Node2D scene, Marker2D entranceDoor) GetRoomWithEntrance(List<PackedScene> scenes, string prevDoorName)
	{
		Node2D scene;
		Marker2D entranceDoor;
		
		do
		{
			int randIndex = _rng.RandiRange(0, scenes.Count - 1);
			scene = scenes[randIndex].Instantiate<Node2D>();
			entranceDoor = GetOppositeDoor(scene, prevDoorName);
		} while (entranceDoor == null);
		
		return (scene, entranceDoor);
	}
	
	// returns opposite door to prev door
	private Marker2D GetOppositeDoor(Node2D scene, string prevDoorName)
	{
		Dictionary<string, string> oppositeDoors = new Dictionary<string, string>()
		{
			{ "leftDoor", "rightDoor" },
			{ "rightDoor", "leftDoor" },
			{ "topDoor", "bottomDoor" },
			{ "bottomDoor", "topDoor" }
		};
	
		if (oppositeDoors.ContainsKey(prevDoorName))
		{
			string oppositeDoorName = oppositeDoors[prevDoorName];
			return scene.GetNodeOrNull<Marker2D>(oppositeDoorName);
		}
		return null;
	}
	
	// returns a random door other than prev door
	private Marker2D GetRandomDoor(Node2D scene, string prevDoorName)
	{
		string[] doorNames = { "leftDoor", "rightDoor", "topDoor", "bottomDoor" };
		
		string doorName;
		Marker2D door;
		do
		{
			int direction = _rng.RandiRange(0, 3);
			doorName = doorNames[direction];
			door = scene.GetNodeOrNull<Marker2D>(doorName);
		} while (doorName == prevDoorName || door == null);
		
		return door;
	}
	
	// returns if any area2D is overlapping with the passed scene's area2D
	private bool IsOverlapping(Node2D scene)
	{
		Area2D area = scene.GetNode<Area2D>("Area2D");
		return area.HasOverlappingAreas();
	}
}
