extends "res://Tests/DeliveryCheck.gd"

func step(action: String, dt: float):
    if action != "": Input.action_press(action)
    player.call("_PhysicsProcess", dt)
    if action != "": Input.action_release(action)

func check():
    await load_level()
    player.set_physics_process(false)
    player.set_process(false)
    var visual = player.get_node("Visual")
    var collider = player.get_node("CollisionShape3D")
    var camera = player.get_node("CameraPivot")
    var camera_transform = camera.transform
    var collider_transform = collider.transform
    var exports = ["MaxHorizontalSpeed", "HorizontalAcceleration", "HorizontalDeceleration", "MaxClimbSpeed", "MaxDescentSpeed", "VerticalAcceleration", "VerticalDeceleration", "YawSpeed", "MaxPitchAngle", "MaxRollAngle", "TiltSpeed", "TiltAltitudeLoss"]
    for prop in player.get_property_list():
        if prop.name in exports and (prop.usage & PROPERTY_USAGE_EDITOR): exports.erase(prop.name)
    await require(exports.is_empty(), "Movement parameters missing from Inspector")
    player.set("TiltAltitudeLoss", 0.0)
    step("move_forward", 0.1)
    await require(is_equal_approx(player.velocity.z, -1.8), "Input must accelerate gradually")
    player.call("_Process", 0.1)
    await require(visual.rotation.x < 0, "Acceleration must pitch nose down")
    step("", 0.05)
    await require(is_equal_approx(player.velocity.z, -1.1), "Release must brake gradually")
    player.call("_Process", 0.5)
    await require(visual.rotation.x > 0, "Forward braking must pitch nose up")
    step("", 0.2)
    step("", 0.016) # Next tick has no braking acceleration once stopped.
    player.call("_Process", 1.0)
    await require(player.velocity.is_zero_approx() and visual.rotation.length() < 0.001, "Stop must hover and settle")
    # All directions use current yaw; visual rotation never feeds physics.
    var actions = ["move_forward", "move_backward", "move_right", "move_left"]
    var directions = [Vector3.FORWARD, Vector3.BACK, Vector3.RIGHT, Vector3.LEFT]
    for yaw in [0.0, PI / 2, PI]:
        player.rotation = Vector3(0, yaw, 0)
        for i in range(4):
            player.position = Vector3(0, 6, 12)
            player.velocity = Vector3.ZERO
            visual.rotation = Vector3.ZERO
            step(actions[i], 0.1)
            var local = player.global_basis.inverse() * player.velocity
            await require(local.is_equal_approx(directions[i] * 1.8), "Acceleration ignored yaw")
            var root_transform = player.transform
            var before_velocity = player.velocity
            player.call("_Process", 0.01)
            var pitch = directions[i].z * deg_to_rad(player.get("MaxPitchAngle"))
            var roll = -directions[i].x * deg_to_rad(player.get("MaxRollAngle"))
            if abs(pitch) > 0.01:
                await require(visual.rotation.x * pitch > 0 and abs(visual.rotation.x) < abs(pitch), "Pitch sign or smoothing failed")
            if abs(roll) > 0.01:
                await require(visual.rotation.z * roll > 0 and abs(visual.rotation.z) < abs(roll), "Roll sign or smoothing failed")
            await require(player.transform == root_transform and player.velocity == before_velocity, "Visual modified physics")
            await require(collider.transform == collider_transform and camera.transform == camera_transform, "Collider or camera tilted")
            step("", 0.05)
            player.call("_Process", 0.5)
            await require(visual.rotation.x * pitch + visual.rotation.z * roll < 0, "Braking must reverse tilt")
    player.rotation = Vector3.ZERO
    player.position = Vector3(0, 6, 12)
    player.velocity = Vector3.ZERO
    step("ascend", 0.1)
    await require(is_equal_approx(player.velocity.y, 0.8), "Climb must accelerate")
    step("", 0.04)
    await require(is_equal_approx(player.velocity.y, 0.4), "Vertical inertia missing")
    step("", 0.1)
    await require(is_zero_approx(player.velocity.y), "Vertical braking must reach hover")
    step("ascend", 1.0)
    await require(is_equal_approx(player.velocity.y, 4.0), "Climb cap failed")
    player.velocity = Vector3.ZERO
    step("descend", 1.0)
    await require(is_equal_approx(player.velocity.y, -3.0), "Descent cap failed")
    player.velocity = Vector3.ZERO
    Input.action_press("move_right")
    step("move_forward", 1.0)
    Input.action_release("move_right")
    await require(is_equal_approx(Vector2(player.velocity.x, player.velocity.z).length(), 12.0), "Diagonal horizontal cap failed")
    await require(is_zero_approx(player.velocity.y), "Disabled altitude loss must cause no sink")
    player.velocity = Vector3.ZERO
    player.set("TiltAltitudeLoss", 0.25)
    step("move_forward", 0.1)
    await require(player.velocity.y < 0 and player.velocity.y >= -0.25, "Tilt loss missing or excessive")
    for i in range(10): step("", 0.1)
    await require(player.velocity.is_zero_approx(), "Altitude loss must not prevent hover")
    var rotor = visual.get_node("PropellerNE")
    var rotor_before = rotor.transform
    visual.call("_Process", 0.01)
    await require(rotor.transform != rotor_before, "Propeller animation broken")
    step("move_right", 0.1)
    player.call("_Process", 0.2)
    player.call("StopFlight")
    player.call("_Process", 1.0)
    await require(visual.rotation.length() < 0.001, "Stopped flight must settle")
    for engine in visual.find_children("*", "CPUParticles3D", true, false):
        await require(not engine.emitting, "Engine still emitting after stop")
    print("PASS: Inspector exports, acceleration, braking tilt, yaw, vertical caps/hover, diagonal cap, altitude loss, isolation, propellers")
    quit(0)
