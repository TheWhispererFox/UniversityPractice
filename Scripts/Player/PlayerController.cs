using Godot;

public partial class PlayerController : CharacterBody3D
{
	[Export(PropertyHint.Range, "0.1,50,0.1")] public float MaxHorizontalSpeed { get; set; } = 12f;
	[Export(PropertyHint.Range, "0.1,100,0.1")] public float HorizontalAcceleration { get; set; } = 18f;
	[Export(PropertyHint.Range, "0.1,100,0.1")] public float HorizontalDeceleration { get; set; } = 14f;
	[Export(PropertyHint.Range, "0,45,0.1")] public float MaxPitchAngle { get; set; } = 18f;
	[Export(PropertyHint.Range, "0,45,0.1")] public float MaxRollAngle { get; set; } = 22f;
	[Export(PropertyHint.Range, "0.1,30,0.1")] public float TiltSpeed { get; set; } = 8f;
	[Export(PropertyHint.Range, "0.1,15,0.1")] public float MaxClimbSpeed { get; set; } = 4f;
	[Export(PropertyHint.Range, "0.1,15,0.1")] public float MaxDescentSpeed { get; set; } = 3f;
	[Export(PropertyHint.Range, "0.1,50,0.1")] public float VerticalAcceleration { get; set; } = 8f;
	[Export(PropertyHint.Range, "0.1,50,0.1")] public float VerticalDeceleration { get; set; } = 10f;
	[Export(PropertyHint.Range, "0,1,0.01")] public float TiltAltitudeLoss { get; set; } = 0.25f;
	[Export(PropertyHint.Range, "0.1,6,0.1")] public float YawSpeed { get; set; } = 1.8f;
	[Export] public float DamageThreshold { get; set; } = 6f;
	[Export] public float DamagePerSpeed { get; set; } = 5f;
	[Export] public float DamageCooldown { get; set; } = 0.5f;

	public float DamageMultiplier { get; set; } = 1f;

	private PlayerHealth _health;
	private Node3D _visual;
	private float _damageCooldown;
	private Vector3 _controlAcceleration;
	private readonly System.Collections.Generic.List<CpuParticles3D> _engineParticles = new();

	public override void _Ready()
	{
		_visual = GetNode<Node3D>("Visual");
		_health = GetNode<PlayerHealth>("PlayerHealth");
		_health.Died += OnDied;
		// Cache the current visual's engines without depending on model-specific paths.
		foreach (Node node in _visual.FindChildren("*", "CPUParticles3D", true, false))
			_engineParticles.Add((CpuParticles3D)node);
		// The camera arm must not collide with its own drone.
		GetNode<SpringArm3D>("CameraPivot/SpringArm3D").AddExcludedObject(GetRid());
	}

	public override void _PhysicsProcess(double delta)
	{
		if (_health.IsDead) return;
		float dt = (float)delta;
		_damageCooldown = Mathf.Max(0f, _damageCooldown - dt);
		RotateY(Input.GetAxis("turn_right", "turn_left") * YawSpeed * dt);

		if (dt <= 0f) return;
		Vector2 planar = Input.GetVector("move_left", "move_right", "move_forward", "move_backward");
		Vector3 direction = GlobalBasis * new Vector3(planar.X, 0f, planar.Y);
		Vector2 horizontal = new Vector2(Velocity.X, Velocity.Z);
		Vector2 previousHorizontal = horizontal;
		if (planar.IsZeroApprox())
			horizontal = horizontal.MoveToward(Vector2.Zero, HorizontalDeceleration * dt);
		else
			// Input requests acceleration, not a target velocity. Momentum survives yaw changes.
			horizontal += new Vector2(direction.X, direction.Z) * HorizontalAcceleration * dt;
		horizontal = horizontal.LimitLength(MaxHorizontalSpeed);
		// Exclude collision impulses from the visual control tilt.
		Vector2 acceleration = (horizontal - previousHorizontal) / dt;
		_controlAcceleration = new Vector3(acceleration.X, 0f, acceleration.Y);

		Vector2 targetTilt = GetTargetTilt();
		float angleLimit = Mathf.DegToRad(Mathf.Max(MaxPitchAngle, MaxRollAngle));
		float tiltAmount = angleLimit > 0f ? Mathf.Clamp(targetTilt.Length() / angleLimit, 0f, 1f) : 0f;
		float verticalInput = Input.GetAxis("descend", "ascend");
		float verticalTarget = verticalInput * (verticalInput > 0f ? MaxClimbSpeed : MaxDescentSpeed);
		// Bounded arcade sink from commanded tilt, never from the rendered visual transform.
		verticalTarget -= TiltAltitudeLoss * tiltAmount * tiltAmount;
		verticalTarget = Mathf.Clamp(verticalTarget, -MaxDescentSpeed, MaxClimbSpeed);
		float verticalRate = Mathf.IsZeroApprox(verticalInput) ? VerticalDeceleration : VerticalAcceleration;
		float verticalVelocity = Mathf.MoveToward(Velocity.Y, verticalTarget, verticalRate * dt);
		Velocity = new Vector3(horizontal.X, verticalVelocity, horizontal.Y);
		Vector3 incomingVelocity = Velocity;
		MoveAndSlide();

		// Use the strongest normal impact only; sliding along a wall is harmless.
		float impactSpeed = 0f;
		for (int i = 0; i < GetSlideCollisionCount(); i++)
			impactSpeed = Mathf.Max(impactSpeed, -incomingVelocity.Dot(GetSlideCollision(i).GetNormal()));

		if (_damageCooldown <= 0f && impactSpeed > DamageThreshold)
		{
			_damageCooldown = DamageCooldown;
			_health.TakeDamage(Mathf.CeilToInt((impactSpeed - DamageThreshold) * DamagePerSpeed * DamageMultiplier));
		}
	}

	private Vector2 GetTargetTilt()
	{
		Vector3 localAcceleration = GlobalBasis.Inverse() * _controlAcceleration;
		float referenceAcceleration = Mathf.Max(HorizontalAcceleration, 0.001f);
		return new Vector2(
			Mathf.Clamp(localAcceleration.Z / referenceAcceleration, -1f, 1f) * Mathf.DegToRad(MaxPitchAngle),
			-Mathf.Clamp(localAcceleration.X / referenceAcceleration, -1f, 1f) * Mathf.DegToRad(MaxRollAngle));
	}

	public override void _Process(double delta)
	{
		Vector2 tilt = GetTargetTilt();
		float blend = 1f - Mathf.Exp(-TiltSpeed * (float)delta);
		_visual.Rotation = new Vector3(
			Mathf.LerpAngle(_visual.Rotation.X, tilt.X, blend),
			0f,
			Mathf.LerpAngle(_visual.Rotation.Z, tilt.Y, blend));
	}

	public void StopFlight()
	{
		Velocity = Vector3.Zero;
		_controlAcceleration = Vector3.Zero;
		SetPhysicsProcess(false);
		foreach (var particles in _engineParticles)
			particles.Emitting = false;
	}

	private void OnDied()
	{
		StopFlight();
		GetNode<Node3D>("Visual").Hide();
	}
}
