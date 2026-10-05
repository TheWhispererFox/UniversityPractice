extends SceneTree

func _initialize():
    call_deferred("check")

func check():
    change_scene_to_file("res://Scenes/UI/MainMenu.tscn")
    await scene_changed
    var button = current_scene.find_child("Exit", true, false)
    if not button is Button:
        push_error("Exit button missing")
        quit(1)
        return
    print("Invoking Exit button; process should close with code 0.")
    button.emit_signal("pressed")
