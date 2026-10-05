extends SceneTree

func _initialize():
    set_meta("test_stats_path", "user://test_" + get_script().resource_path.get_file().get_basename() + ".json")
    call_deferred("check")

func check():
    var scene = load("res://Main.tscn").instantiate()
    root.add_child(scene)
    current_scene = scene
    await physics_frame
    var player = scene.get_node("TestLevel/Player")
    var health = player.get_node("PlayerHealth")
    assert(health.get("CurrentHp") == 100)
    var start = player.position
    for i in range(30):
        await physics_frame
    assert(player.position.distance_to(start) < 0.01, "Hover should hold position")
    Input.action_press("move_forward")
    for i in range(240):
        await physics_frame
    Input.action_release("move_forward")
    assert(player.position.z < start.z - 5, "Forward flight failed")
    assert(health.get("CurrentHp") < 100, "Strong wall impact should damage drone")
    assert(health.get("CurrentHp") > 0, "A single impact should not destroy drone")
    health.call("TakeDamage", 100)
    assert(health.get("CurrentHp") == 0)
    assert(scene.get_node("TestLevel/GameManager").get("IsDefeated"))
    assert(not player.is_physics_processing())
    assert(not player.get_node("Visual").visible)
    health.call("TakeDamage", 20)
    assert(health.get("CurrentHp") == 0)
    print("PASS: hover, flight, collision damage, defeat, disabled control, HP clamp")
    quit()
