using Godot;

public partial class GameManager : Node
{
	[Signal] public delegate void GameEndedEventHandler(bool won);
	public bool IsDefeated { get; private set; }
	public bool IsVictorious { get; private set; }
	public string ResultReason { get; private set; } = "";
	private PlayerController _player;
	private PlayerHealth _health;
	private MissionManager _missions;

	public override void _Ready()
	{
		_player = GetNode<PlayerController>("../Player");
		_health = _player.GetNode<PlayerHealth>("PlayerHealth");
		_missions = GetNode<MissionManager>("../MissionManager");
		_health.Died += OnDestroyed;
		_missions.AllMissionsCompleted += OnVictory;
		_missions.MissionTimedOut += OnTimeout;
	}

	private void OnDestroyed() => Finish(false, "Drone destroyed");
	private void OnTimeout() => Finish(false, "Mission time expired");
	private void OnVictory() => Finish(true, "All deliveries completed");

	private void Finish(bool won, string reason)
	{
		if (IsDefeated || IsVictorious) return;
		IsVictorious = won;
		IsDefeated = !won;
		ResultReason = reason;
		_player.StopFlight();
		EmitSignal(SignalName.GameEnded, won);
	}

	public void Restart() => GetTree().ReloadCurrentScene();
	public void MainMenu() => GetTree().ChangeSceneToFile("res://Scenes/UI/MainMenu.tscn");

	public override void _UnhandledInput(InputEvent @event)
	{
		if (@event.IsActionPressed("restart") && !@event.IsEcho()) Restart();
	}

	public override void _ExitTree()
	{
		if (GodotObject.IsInstanceValid(_health)) _health.Died -= OnDestroyed;
		if (GodotObject.IsInstanceValid(_missions))
		{
			_missions.AllMissionsCompleted -= OnVictory;
			_missions.MissionTimedOut -= OnTimeout;
		}
	}
}
