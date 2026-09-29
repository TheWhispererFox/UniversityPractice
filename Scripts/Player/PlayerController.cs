using Godot;

public partial class PlayerController : CharacterBody3D
{
	[Export] public float FlightSpeed { get; set; } = 12f;
	[Export] public float Acceleration { get; set; } = 18f;
	[Export] public float TurnSpeed { get; set; } = 1.8f;
	[Export] public float DamageThreshold { get; set; } = 6f;
	[Export] public float DamagePerSpeed { get; set; } = 5f;
	[Export] public float DamageCooldown { get; set; } = 0.5f;

	private PlayerHealth _health;
	private float _damageCooldown;

	public override void _Ready()
	{
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
		Velocity = Velocity.MoveToward(direction * FlightSpeed, Acceleration * dt);
		Vector3 incomingVelocity = Velocity;
		MoveAndSlide();

		// Use the strongest normal impact only; sliding along a wall is harmless.
		float impactSpeed = 0f;
		for (int i = 0; i < GetSlideCollisionCount(); i++)
			impactSpeed = Mathf.Max(impactSpeed, -incomingVelocity.Dot(GetSlideCollision(i).GetNormal()));

		if (_damageCooldown <= 0f && impactSpeed > DamageThreshold)
		{
			_damageCooldown = DamageCooldown;
			_health.TakeDamage(Mathf.CeilToInt((impactSpeed - DamageThreshold) * DamagePerSpeed));
		}
	}

	private void OnDied()
	{
		Velocity = Vector3.Zero;
		GetNode<Node3D>("Visual").Hide();
		SetPhysicsProcess(false);
	}
}
