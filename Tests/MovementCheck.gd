extends "res://Tests/DeliveryCheck.gd"

func check():
    await load_level()
    player.set_physics_process(false)
    player.set_process(false)
    var visual = player.get_node("Visual")
    var collider = player.get_node("CollisionShape3D")
    var camera = player.get_node("CameraPivot")
    var camera_transform = camera.transform
    var collider_transform = collider.transform
    Input.action_press("move_forward")
    player.call("_PhysicsProcess", 0.1)
    Input.action_release("move_forward")
    await require(is_equal_approx(player.velocity.z, -1.8), "Horizontal acceleration failed")
    player.call("_PhysicsProcess", 0.05)
    await require(is_equal_approx(player.velocity.z, -1.2), "Release should brake gradually using deceleration")
    player.call("_PhysicsProcess", 0.2)
    await require(player.velocity.is_zero_approx(), "Braking should stop without overshoot")
    Input.action_press("ascend")
    player.call("_PhysicsProcess", 0.1)
    Input.action_release("ascend")
    await require(is_equal_approx(player.velocity.y, 1.8), "Vertical acceleration changed")
    player.velocity = Vector3.ZERO
    # Local directions must remain correct when the physics root turns.
    for yaw in [0.0, PI / 2, PI]:
        player.rotation = Vector3(0, yaw, 0)
        var root_transform = player.transform
        for local in [Vector3(0, 0, -12), Vector3(0, 0, 12), Vector3(12, 0, 0), Vector3(-12, 0, 0)]:
            visual.rotation = Vector3.ZERO
            player.velocity = player.global_basis * local
            var before_velocity = player.velocity
            player.call("_Process", 0.01)
            var target_pitch = deg_to_rad(15) * local.z / 12
            var target_roll = -deg_to_rad(20) * local.x / 12
            await require(abs(visual.rotation.x) <= abs(target_pitch) + 0.0001, "Pitch exceeds limit")
            await require(abs(visual.rotation.z) <= abs(target_roll) + 0.0001, "Roll exceeds limit")
            if abs(target_pitch) > 0.01:
                await require(visual.rotation.x * target_pitch > 0 and abs(visual.rotation.x) < abs(target_pitch), "Pitch sign or smoothing failed")
            if abs(target_roll) > 0.01:
                await require(visual.rotation.z * target_roll > 0 and abs(visual.rotation.z) < abs(target_roll), "Roll sign or smoothing failed")
            await require(player.transform == root_transform and player.velocity == before_velocity, "Tilt changed physics")
            await require(collider.transform == collider_transform and camera.transform == camera_transform, "Tilt changed collider or camera")
    player.velocity = Vector3.ZERO
    player.call("_Process", 1.0)
    await require(visual.rotation.length() < 0.001, "Visual must return to neutral")
    player.velocity = player.global_basis * Vector3(100, 0, -100)
    player.call("_Process", 2.0)
    await require(abs(visual.rotation.x) <= deg_to_rad(15) + 0.0001 and abs(visual.rotation.z) <= deg_to_rad(20) + 0.0001, "Wind or overspeed exceeds tilt limits")
    player.call("StopFlight")
    player.call("_Process", 1.0)
    await require(visual.rotation.length() < 0.001, "Stopped flight must settle visually")
    print("PASS: acceleration, braking, vertical response, local tilt, smoothing, limits, physics isolation")
    quit(0)
