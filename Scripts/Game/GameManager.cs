using Godot;

public partial class GameManager : Node
{
	public bool IsDefeated { get; private set; }
	public bool IsVictorious { get; private set; }

	public override void _Ready()
	{
		var player = GetNode<PlayerController>("../Player");
		var health = player.GetNode<PlayerHealth>("PlayerHealth");
		health.HealthChanged += hp => GD.Print($"Drone HP: {hp}/100");
		health.Died += () =>
		{
			if (IsVictorious) return;
			IsDefeated = true;
			GD.Print("Defeat: drone destroyed. Press R to restart.");
		};
		GetNode<MissionManager>("../MissionManager").AllMissionsCompleted += () =>
		{
			if (IsDefeated) return;
			IsVictorious = true;
			player.Velocity = Vector3.Zero;
			player.SetPhysicsProcess(false);
			GD.Print("Victory: all deliveries completed! Press R to restart.");
		};
		GD.Print("Drone HP: 100/100. Press R to restart.");
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		if (@event.IsActionPressed("restart") && !@event.IsEcho())
			GetTree().ReloadCurrentScene();
	}
}
