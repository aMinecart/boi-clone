using Godot;
using System;
using System.Collections.Generic;

public class RoomManager
{
	[Export] public string roomsPath = "res://scenes/rooms/";
	[Export] public string hallwaysPath = "res://scenes/hallways/";
	public List<PackedScene> loadedRooms { get; private set; } = new List<PackedScene>();
	public List<PackedScene> loadedHallways { get; private set; } = new List<PackedScene>();

	private RandomNumberGenerator _rng = new RandomNumberGenerator();

	public RoomManager()
	{
		_rng.Randomize();
	}

	public void LoadScenes()
	{
		SetScenesFromFolder(roomsPath, loadedRooms);
		SetScenesFromFolder(hallwaysPath, loadedHallways);
	}

	// sets passed list to all scenes from the specified path
	private void SetScenesFromFolder(string folderPath, List<PackedScene> scenes)
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
	
	// returns a working scene and its entrance door
	public (Node2D scene, Marker2D entranceDoor) GetRoomWithEntrance(List<PackedScene> scenes, string prevDoorName)
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

	// returns a random door other than prev door
	public Marker2D GetRandomDoor(Node2D scene, string prevDoorName)
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
}
