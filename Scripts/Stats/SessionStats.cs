using Godot;

public partial class SessionStats : Node
{
    [Signal] public delegate void CompletedEventHandler();
    public double ElapsedSeconds { get; private set; }
    public int DamageEvents { get; private set; }
    public int Deliveries => _missions.CompletedMissions;
    public bool Finished { get; private set; }
    public bool Won { get; private set; }
    private int _lastHp;
    private PlayerHealth _health;
    private MissionManager _missions;
    private GameManager _game;
    private SaveManager _save;

    public override void _Ready()
    {
        _health = GetNode<PlayerHealth>("../Player/PlayerHealth");
        _missions = GetNode<MissionManager>("../MissionManager");
        _game = GetNode<GameManager>("../GameManager");
        _save = GetNode<SaveManager>("../SaveManager");
        _lastHp = _health.CurrentHp;
        _health.HealthChanged += OnHealthChanged;
        _game.GameEnded += OnGameEnded;
    }

    public override void _Process(double delta)
    {
        if (!Finished) ElapsedSeconds += delta;
    }

    private void OnHealthChanged(int hp)
    {
        if (!Finished && hp < _lastHp) DamageEvents++;
        _lastHp = hp;
    }

    private void OnGameEnded(bool won)
    {
        if (Finished) return;
        Finished = true;
        Won = won;
        _save.RecordRun(won, ElapsedSeconds, Deliveries);
        EmitSignal(SignalName.Completed);
    }

    public override void _ExitTree()
    {
        if (GodotObject.IsInstanceValid(_health)) _health.HealthChanged -= OnHealthChanged;
        if (GodotObject.IsInstanceValid(_game)) _game.GameEnded -= OnGameEnded;
    }
}
