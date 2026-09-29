using Godot;

public partial class GameManager : Node
{
	public bool IsDefeated { get; private set; }

	public override void _Ready()
	{
		var health = GetNode<PlayerHealth>("../Player/PlayerHealth");
		health.HealthChanged += hp => GD.Print($"Drone HP: {hp}/100");
		health.Died += () =>
		{
			IsDefeated = true;
			GD.Print("Defeat: drone destroyed. Press R to restart.");
		};
		GD.Print("Test flight ready. Drone HP: 100/100. Press R to restart.");
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		if (@event.IsActionPressed("restart") && !@event.IsEcho())
			GetTree().ReloadCurrentScene();
	}
}
