extends SceneTree

func _initialize():
    set_meta("test_stats_path", "user://test_process_persistence.json")
    call_deferred("check")

func check():
    var path = str(get_meta("test_stats_path"))
    var writing = "--write" in OS.get_cmdline_user_args()
    if writing and FileAccess.file_exists(path): DirAccess.remove_absolute(path)
    var save = load("res://Scripts/Stats/SaveManager.cs").new()
    root.add_child(save)
    save.call("RecordRun", writing, 12.5 if writing else 20.0, 3 if writing else 1)
    var data = JSON.parse_string(FileAccess.get_file_as_string(path))
    var correct = data != null and data.totalRuns == (1 if writing else 2) and data.totalWins == 1 and data.totalLosses == (0 if writing else 1) and data.totalDeliveries == (3 if writing else 4) and data.bestCompletionTime == 12.5
    if not correct:
        push_error("Persistence across process restart failed")
        quit(1)
        return
    print("PASS: persistence writer" if writing else "PASS: separate process reloaded and updated statistics")
    quit(0)
