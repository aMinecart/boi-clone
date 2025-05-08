using Godot;
using System;

public partial class GameOverNavigation : Control
{
    private void _on_play_again_button_up()
	{
		SceneManager.instance.ChangeScene(eSceneNames.TestScene);
	}

    private void _on_return_to_menu_button_up()
	{
		SceneManager.instance.ChangeScene(eSceneNames.MainMenu);
	}
}
