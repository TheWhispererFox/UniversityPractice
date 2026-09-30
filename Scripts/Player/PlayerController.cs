using Godot;

public partial class PlayerController : CharacterBody3D
{
	[Export(PropertyHint.Range, "0.1,50,0.1")] public float FlightSpeed { get; set; } = 12f;
	[Export(PropertyHint.Range, "0.1,100,0.1")] public float Acceleration { get; set; } = 18f;
	[Export(PropertyHint.Range, "0.1,100,0.1")] public float Deceleration { get; set; } = 12f;
	[Export(PropertyHint.Range, "0,45,0.1")] public float PitchAngle { get; set; } = 15f;
	[Export(PropertyHint.Range, "0,45,0.1")] public float RollAngle { get; set; } = 20f;
	[Export(PropertyHint.Range, "0.1,30,0.1")] public float TiltSmoothingSpeed { get; set; } = 8f;
	[Export] public float TurnSpeed { get; set; } = 1.8f;
	[Export] public float DamageThreshold { get; set; } = 6f;
	[Export] public float DamagePerSpeed { get; set; } = 5f;
	[Export] public float DamageCooldown { get; set; } = 0.5f;

	public float DamageMultiplier { get; set; } = 1f;
	public Vector3 WindVelocity { get; set; }

	private PlayerHealth _health;
	private Node3D _visual;
	private float _damageCooldown;

	public override void _Ready()
	{
		_visual = GetNode<Node3D>("Visual");
		_health = GetNode<PlayerHealth>("PlayerHealth");
		_health.Died += OnDied;
		// The camera arm must not collide with its own drone.
		GetNode<SpringArm3D>("CameraPivot/SpringArm3D").AddExcludedObject(GetRid());
	}

	public override void _PhysicsProcess(double delta)
	{
		if (_health.IsDead) return;
		float dt = (float)delta;
		_damageCooldown = Mathf.Max(0f, _damageCooldown - dt);
		RotateY(Input.GetAxis("turn_right", "turn_left") * TurnSpeed * dt);

		Vector2 planar = Input.GetVector("move_left", "move_right", "move_forward", "move_backward");
		Vector3 direction = GlobalBasis * new Vector3(planar.X, 0f, planar.Y);
		direction.Y = Input.GetAxis("descend", "ascend");
		if (direction.LengthSquared() > 1f) direction = direction.Normalized();
		Vector3 targetVelocity = direction * FlightSpeed + WindVelocity;
		// Preserve the existing vertical response; only horizontal braking changes.
		float verticalVelocity = Velocity.MoveToward(targetVelocity, Acceleration * dt).Y;
		Vector2 horizontal = new Vector2(Velocity.X, Velocity.Z);
		Vector2 targetHorizontal = new Vector2(targetVelocity.X, targetVelocity.Z);
		bool braking = planar.IsZeroApprox() ||
			(horizontal.Dot(targetHorizontal) >= 0f && horizontal.LengthSquared() > targetHorizontal.LengthSquared());
		horizontal = horizontal.MoveToward(targetHorizontal, (braking ? Deceleration : Acceleration) * dt);
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

	public override void _Process(double delta)
	{
		// Use the physics root's local velocity, never the already tilted visual basis.
		Vector3 localVelocity = GlobalBasis.Inverse() * Velocity;
		float speed = Mathf.Max(FlightSpeed, 0.001f);
		float pitch = Mathf.Clamp(localVelocity.Z / speed, -1f, 1f) * Mathf.DegToRad(PitchAngle);
		float roll = -Mathf.Clamp(localVelocity.X / speed, -1f, 1f) * Mathf.DegToRad(RollAngle);
		float blend = 1f - Mathf.Exp(-TiltSmoothingSpeed * (float)delta);
		_visual.Rotation = new Vector3(
			Mathf.LerpAngle(_visual.Rotation.X, pitch, blend),
			0f,
			Mathf.LerpAngle(_visual.Rotation.Z, roll, blend));
	}

	public void StopFlight()
	{
		Velocity = Vector3.Zero;
		SetPhysicsProcess(false);
		GetNode<CpuParticles3D>("Visual/EngineLeft").Emitting = false;
		GetNode<CpuParticles3D>("Visual/EngineRight").Emitting = false;
	}

	private void OnDied()
	{
		StopFlight();
		GetNode<Node3D>("Visual").Hide();
	}
}
