extends SceneTree

var scene: Node
var player: Node3D
var missions: Node
var game: Node
var health: Node

func _initialize():
	call_deferred("check")

func require(condition: bool, message: String):
	if not condition:
		push_error(message)
		quit(1)
		# Never resume the test and overwrite the failing exit code with quit(0).
		while true:
			await process_frame

func settle():
	for i in range(5):
		await physics_frame

func visit(zone_name: String):
	player.position = Vector3(0, 5, 12)
	player.velocity = Vector3.ZERO
	await settle()
	var zone = scene.get_node("TestLevel/Zones/" + zone_name)
	player.global_position = zone.global_position + Vector3(0, 3, 0)
	await settle()

func load_level():
	scene = load("res://Main.tscn").instantiate()
	root.add_child(scene)
	current_scene = scene
	player = scene.get_node("TestLevel/Player")
	missions = scene.get_node("TestLevel/MissionManager")
	game = scene.get_node("TestLevel/GameManager")
	health = player.get_node("PlayerHealth")
	await settle()

func check():
	await load_level()
	await require(missions.get("MissionCount") == 3, "Expected three missions")
	await require(scene.has_node("TestLevel/Zones/Pickup1/Cargo"), "Cargo must start at active pickup")
	await visit("Pickup2")
	await require(not missions.get("HasCargo"), "Wrong pickup granted cargo")
	await visit("Delivery1")
	await require(missions.get("CompletedMissions") == 0, "Delivery without cargo")
	await visit("Pickup1")
	await require(missions.get("HasCargo"), "Correct pickup failed")
	await require(player.has_node("Visual/Cargo"), "Cargo must attach to drone")
	await visit("Delivery2")
	await require(missions.get("HasCargo") and missions.get("CompletedMissions") == 0, "Wrong destination accepted cargo")
	await visit("Pickup2")
	await require(missions.get("CompletedMissions") == 0, "Pickup skipped mission")
	await visit("Delivery1")
	await require(missions.get("CompletedMissions") == 1 and not missions.get("HasCargo"), "Mission 1 failed")
	await visit("Pickup1")
	await require(not missions.get("HasCargo"), "Completed pickup remains active")
	for i in [2, 3]:
		await visit("Pickup" + str(i))
		await require(missions.get("HasCargo"), "Pickup failed")
		await visit("Delivery" + str(i))
		await require(missions.get("CompletedMissions") == i, "Delivery failed")
	await require(game.get("IsVictorious") and not game.get("IsDefeated"), "Victory missing")
	await require(not player.is_physics_processing(), "Victory should stop flight")
	await visit("Pickup1")
	await require(not missions.get("HasCargo"), "Pickup after victory")
	scene.free()
	await load_level()
	await visit("Pickup1")
	health.call("TakeDamage", 100)
	await require(game.get("IsDefeated") and not missions.get("HasCargo"), "Defeat should clear cargo")
	await visit("Delivery1")
	await require(missions.get("CompletedMissions") == 0, "Dead player delivered")
	var restart = InputEventAction.new()
	restart.action = "restart"
	restart.pressed = true
	Input.parse_input_event(restart)
	await settle()
	scene = current_scene
	missions = scene.get_node("TestLevel/MissionManager")
	game = scene.get_node("TestLevel/GameManager")
	health = scene.get_node("TestLevel/Player/PlayerHealth")
	await require(health.get("CurrentHp") == 100, "Restart did not restore HP")
	await require(missions.get("CompletedMissions") == 0 and not missions.get("HasCargo"), "Restart did not reset missions")
	await require(not game.get("IsDefeated") and not game.get("IsVictorious"), "Restart did not reset game")
	print("PASS: route validation, cargo attachment, three deliveries, victory, defeat, R restart")
	quit(0)
