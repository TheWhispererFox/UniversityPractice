extends "res://Tests/DeliveryCheck.gd"

var stats: Node
var save: Node
var ui: Node
var screenshots = false

func bind_game():
    scene = current_scene
    player = scene.get_node("TestLevel/Player")
    missions = scene.get_node("TestLevel/MissionManager")
    game = scene.get_node("TestLevel/GameManager")
    health = player.get_node("PlayerHealth")
    stats = scene.get_node("TestLevel/SessionStats")
    save = scene.get_node("TestLevel/SaveManager")
    ui = scene.get_node("TestLevel/GameUI")

func click_button(name: String):
    var button = current_scene.find_child(name, true, false)
    await require(button is Button, "Missing button: " + name)
    button.emit_signal("pressed")
    await scene_changed
    await settle()

func totals():
    return JSON.parse_string(FileAccess.get_file_as_string(get_meta("test_stats_path")))

func inspect_layout(node: Node):
    if node is Control and node.is_visible_in_tree() and (node is Label or node is Button or node is ProgressBar):
        var bounds = Rect2(Vector2.ZERO, root.get_visible_rect().size).grow(1)
        await require(bounds.encloses(node.get_global_rect()), "UI outside viewport: " + str(node.get_path()))
    if node is VBoxContainer and node.is_visible_in_tree():
        var previous: Control
        for child in node.get_children():
            if child is Control and child.is_visible_in_tree():
                if previous:
                    await require(not previous.get_global_rect().intersects(child.get_global_rect()), "Overlapping UI controls")
                previous = child
    for child in node.get_children():
        await inspect_layout(child)

func capture(name: String):
    for size in [Vector2i(1280, 720), Vector2i(1920, 1080)]:
        root.size = size
        await settle()
        await inspect_layout(current_scene)
        if screenshots:
            await RenderingServer.frame_post_draw
            var path = "res://docs/screenshots/" + name + "_" + str(size.x) + ".png"
            await require(root.get_texture().get_image().save_png(path) == OK, "Screenshot failed")

func check():
    screenshots = "--capture" in OS.get_cmdline_user_args()
    root.content_scale_size = Vector2i(1280, 720)
    root.content_scale_mode = Window.CONTENT_SCALE_MODE_CANVAS_ITEMS
    if screenshots: DirAccess.make_dir_recursive_absolute("res://docs/screenshots")
    var path = str(get_meta("test_stats_path"))
    if FileAccess.file_exists(path): DirAccess.remove_absolute(path)
    change_scene_to_file("res://Scenes/UI/MainMenu.tscn")
    await scene_changed
    await settle()
    await capture("menu")
    await click_button("StartGame")
    bind_game()
    await require(stats.get("DamageEvents") == 0 and stats.get("Deliveries") == 0, "Session not reset")
    await require(not FileAccess.file_exists(path), "Starting a run must not count as completion")
    ui.call("RefreshHud")
    await require(ui.find_child("Cargo", true, false).text == "Cargo: none", "Initial HUD cargo")
    await capture("hud")
    for i in [1, 2, 3]:
        await visit("Pickup" + str(i))
        ui.call("RefreshHud")
        await require(ui.find_child("Cargo", true, false).text == "Cargo: acquired", "HUD cargo mismatch")
        if i == 1: await capture("cargo")
        await visit("Delivery" + str(i))
    await require(stats.get("Finished") and stats.get("Won"), "Victory did not finalize session")
    await require(stats.get("Deliveries") == 3, "Session deliveries mismatch")
    await require(ui.find_child("ResultOverlay", true, false).visible, "Victory panel absent")
    await require(totals().totalRuns == 1 and totals().totalWins == 1 and totals().totalDeliveries == 3, "Victory aggregate wrong")
    var elapsed = stats.get("ElapsedSeconds")
    var best = totals().bestCompletionTime
    game.emit_signal("GameEnded", true)
    await settle()
    await require(totals().totalRuns == 1 and stats.get("ElapsedSeconds") == elapsed, "Duplicate finish or timer keeps running")
    await capture("victory")
    await click_button("Restart")
    bind_game()
    await require(health.get("CurrentHp") == 100 and missions.get("CompletedMissions") == 0 and not missions.get("HasCargo"), "Restart state wrong")
    await require(stats.get("DamageEvents") == 0 and not stats.get("Finished"), "Restart stats wrong")
    health.call("TakeDamage", 10)
    health.call("TakeDamage", 100)
    await settle()
    await require(stats.get("DamageEvents") == 2 and stats.get("Finished") and not stats.get("Won"), "Damage/death stats wrong")
    await require(totals().totalLosses == 1 and totals().totalRuns == 2 and totals().bestCompletionTime == best, "Defeat totals wrong")
    await capture("defeat")
    await click_button("Restart")
    bind_game()
    await require(health.get("CurrentHp") == 100 and stats.get("DamageEvents") == 0, "Defeat restart failed")
    missions.call("_PhysicsProcess", 1000.0)
    await settle()
    await require(game.get("IsDefeated") and stats.get("DamageEvents") == 0, "Timeout defeat failed")
    await click_button("MainMenu")
    await require(current_scene.name == "MainMenu", "Defeat to menu failed")
    await click_button("StartGame")
    bind_game()
    for i in [1, 2, 3]:
        await visit("Pickup" + str(i))
        await visit("Delivery" + str(i))
    await click_button("MainMenu")
    await click_button("StartGame")
    bind_game()
    await require(totals().totalRuns == 4 and totals().totalWins == 2 and totals().totalLosses == 2 and totals().totalDeliveries == 6, "Aggregates/reload failed")
    # Fresh SaveManager reload demonstrates persistence independently of in-memory state.
    save.call("LoadStats")
    await require(save.get("LastSaveSucceeded"), "Save failed")
    for contents in ["{broken", '{"totalRuns":-1}', "null"]:
        var file = FileAccess.open(path, FileAccess.WRITE)
        file.store_string(contents)
        file.close()
        save.call("LoadStats")
        health.call("TakeDamage", 100)
        await settle()
        # Only the first death finishes this run; later cases just exercise loading.
    var denied_path = "user://missing_test_directory/statistics.json"
    set_meta("test_stats_path", denied_path)
    save.call("RecordRun", false, 1.0, 0)
    await require(not save.get("LastSaveSucceeded"), "Write failure not reported")
    print("PASS: UI layout 720p/1080p, menu/start, HUD, victory/defeat/restart/menu, session stats, persistence, duplicate guard, corrupt/missing/write-error handling")
    quit(0)
