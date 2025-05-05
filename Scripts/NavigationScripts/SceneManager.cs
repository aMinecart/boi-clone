using Godot;
using System;
using System.Collections.Generic;

public enum eSceneNames {
	MainMenu = 10,
	TestScene = 20,
	GameOver = 30
	}

public partial class SceneManager : Node {

	public static SceneManager instance;

	public Dictionary<eSceneNames, SceneData> sceneDictionary = new Dictionary<eSceneNames, SceneData>() {
		{eSceneNames.MainMenu, new SceneData("res://Scenes/GameplayScenes/MainMenu.tscn", "Main Menu") },
		{eSceneNames.TestScene, new SceneData("res://Scenes/GameplayScenes/TestScene.tscn", "Test Scene") },
		{eSceneNames.GameOver, new SceneData("res://Scenes/GameplayScenes/GameOver.tscn", "Game Over") }
		
	};

	public override void _Ready() {
		instance = this;
	}

	public void ChangeScene(eSceneNames mySceneName) {
		string myPath = sceneDictionary[mySceneName].path;
		GetTree().ChangeSceneToFile(myPath);
	}

}
