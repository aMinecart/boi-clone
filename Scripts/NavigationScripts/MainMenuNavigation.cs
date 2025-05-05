using Godot;
using System;

public partial class MainMenuNavigation : Control
{
	private void _on_game_start_button_up()
	{
		SceneManager.instance.ChangeScene(eSceneNames.TestScene);
	}

	private void _on_game_quit_button_up()
	{
		GetTree().Quit();
	}
}
