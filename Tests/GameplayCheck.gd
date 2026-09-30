extends "res://Tests/DeliveryCheck.gd"

func check():
	# The difficulty profile must affect both mission budget and real impact damage.
	var budgets = [120.0, 75.0, 45.0]
	var damage = []
	for level in range(3):
		set_meta("difficulty", level)
		set_meta("weather", 0)
		await load_level()
		await require(is_equal_approx(missions.get("TimeLimit"), budgets[level]), "Wrong difficulty time budget")
		Input.action_press("move_forward")
		for i in range(240):
			await physics_frame
		Input.action_release("move_forward")
		damage.append(100 - health.get("CurrentHp"))
		scene.free()
	await require(damage == [15, 30, 45], "Difficulty must scale actual collision damage")

	set_meta("difficulty", 1)
	await load_level()
	var score = scene.get_node("TestLevel/ScoreManager")
	missions.set_physics_process(false)
	missions.call("_PhysicsProcess", 10.0)
	health.call("TakeDamage", 10)
	await visit("Pickup1")
	await require(scene.get_node("TestLevel/DeliveryEffects/Pickup").emitting, "Pickup effect missing")
	var seconds = missions.get("TimeRemaining")
	await visit("Delivery1")
	var expected = max(0, 500 + int(floor(seconds * 10)) - 150)
	await require(score.get("LastMissionScore") == expected, "Incorrect mission score")
	await require(score.get("LastDamagePenalty") == 150, "Damage penalty missing")
	await require(scene.get_node("TestLevel/DeliveryEffects/Delivery").emitting, "Delivery effect missing")
	await require(is_equal_approx(missions.get("TimeRemaining"), 75.0), "Timer did not reset on next mission")
	await visit("Pickup2")
	await visit("Delivery2")
	await require(score.get("LastDamagePenalty") == 0, "Previous mission damage counted twice")
	await require(score.get("LastMissionScore") > expected, "Fast undamaged mission should earn more")
	var total = score.get("TotalScore")
	await visit("Pickup3")
	missions.call("_PhysicsProcess", 100.0)
	await require(game.get("IsDefeated") and not game.get("IsVictorious"), "Timeout must cause defeat")
	await require(missions.get("TimeRemaining") == 0 and not missions.get("HasCargo"), "Timeout must clear cargo and clamp timer")
	await require(not player.is_physics_processing(), "Timeout must disable flight")
	for engine in player.get_node("Visual").find_children("*", "CPUParticles3D", true, false):
		await require(not engine.emitting, "Engine must stop after defeat")
	await visit("Delivery3")
	await require(score.get("TotalScore") == total, "Timeout must not award delivery points")
	scene.free()

	await load_level()
	score = scene.get_node("TestLevel/ScoreManager")
	await require(score.get("TotalScore") == 0, "New run must reset score")
	# Weather was removed from the current project; test the current four engines.
	var engines = player.get_node("Visual").find_children("*", "CPUParticles3D", true, false)
	await require(engines.size() == 4, "Expected four propeller engines")
	for engine in engines:
		await require(engine.emitting, "Engine particles missing")
	# Actual keyboard path: changing difficulty starts a fresh run and R retains it.
	var key = InputEventKey.new()
	key.physical_keycode = KEY_3
	key.pressed = true
	Input.parse_input_event(key)
	await settle()
	scene = current_scene
	await require(scene.get_node("TestLevel/MissionManager").get("TimeLimit") == 45.0, "Difficulty key failed")
	var restart = InputEventAction.new()
	restart.action = "restart"
	restart.pressed = true
	Input.parse_input_event(restart)
	await settle()
	scene = current_scene
	await require(scene.get_node("TestLevel/MissionManager").get("TimeLimit") == 45.0, "R must retain difficulty")
	print("PASS: difficulty, impact scaling, timer, score, damage penalty, timeout, particles, restart")
	quit(0)
