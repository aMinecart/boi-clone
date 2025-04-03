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
using System;

public partial class Dungeon : Node2D
{
	private RoomManager _roomManager;
	private RoomPlacer _roomPlacer;

	public override void _Ready()
	{
		_roomManager = new RoomManager();
		_roomPlacer = new RoomPlacer(this, _roomManager);

		_roomManager.LoadScenes();
		_roomPlacer.SetUpDungeon(10);
	}
}
