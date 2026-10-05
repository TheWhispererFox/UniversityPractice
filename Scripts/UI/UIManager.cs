using Godot;

public partial class UIManager : CanvasLayer
{
    private PlayerHealth _health;
    private MissionManager _missions;
    private GameManager _game;
    private SessionStats _stats;
    private SaveManager _save;
    private ScoreManager _score;
    private DifficultyManager _difficulty;
    private Label _hp, _mission, _task, _cargo, _timer, _points, _mode, _resultTitle, _resultText;
    private ProgressBar _healthBar;
    private Control _overlay;
    private Button _restart;

    public override void _Ready()
    {
        _health = GetNode<PlayerHealth>("../Player/PlayerHealth");
        _missions = GetNode<MissionManager>("../MissionManager");
        _game = GetNode<GameManager>("../GameManager");
        _stats = GetNode<SessionStats>("../SessionStats");
        _save = GetNode<SaveManager>("../SaveManager");
        _score = GetNode<ScoreManager>("../ScoreManager");
        _difficulty = GetNode<DifficultyManager>("../DifficultyManager");
        var root = new Control { Name = "Root", MouseFilter = Control.MouseFilterEnum.Ignore };
        AddChild(root);
        UiLayout.Fill(root);
        var margin = new MarginContainer { Name = "HUD", MouseFilter = Control.MouseFilterEnum.Ignore };
        root.AddChild(margin);
        margin.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.TopLeft);
        margin.AddThemeConstantOverride("margin_left", 24);
        margin.AddThemeConstantOverride("margin_top", 24);
        var box = UiLayout.Card(margin, "FlightStatus", 390);
        UiLayout.Text(box, "Heading", "FLIGHT STATUS", 18).Modulate = UiLayout.Accent;
        _hp = UiLayout.Text(box, "Health", "");
        _healthBar = new ProgressBar { Name = "HealthBar", MaxValue = 100, ShowPercentage = false, CustomMinimumSize = new Vector2(0, 12) };
        box.AddChild(_healthBar);
        _mission = UiLayout.Text(box, "Mission", "");
        _task = UiLayout.Text(box, "Task", "", 20);
        _cargo = UiLayout.Text(box, "Cargo", "", 20);
        _timer = UiLayout.Text(box, "Timer", "", 20);
        _points = UiLayout.Text(box, "Score", "", 20);
        _mode = UiLayout.Text(box, "Difficulty", "", 18);
        _overlay = new ColorRect { Name = "ResultOverlay", Color = new Color(0.01f, 0.02f, 0.04f, 0.82f), Visible = false };
        root.AddChild(_overlay);
        UiLayout.Fill(_overlay);
        var center = new CenterContainer();
        _overlay.AddChild(center);
        UiLayout.Fill(center);
        var result = UiLayout.Card(center, "ResultCard", 540);
        _resultTitle = UiLayout.Text(result, "ResultTitle", "", 40);
        _resultTitle.Modulate = UiLayout.Accent;
        _resultText = UiLayout.Text(result, "ResultText", "", 22);
        _restart = UiLayout.Button(result, "Restart", "Restart", _game.Restart);
        UiLayout.Button(result, "MainMenu", "Main Menu", _game.MainMenu);
        _stats.Completed += ShowResult;
        // Cache references once; refresh text at 10 Hz, not every rendered frame.
        var refresh = new Timer { WaitTime = 0.1, Autostart = true };
        AddChild(refresh);
        refresh.Timeout += RefreshHud;
        Callable.From(RefreshHud).CallDeferred();
    }

    public void RefreshHud()
    {
        _hp.Text = $"Health  {_health.CurrentHp} / 100";
        _healthBar.Value = _health.CurrentHp;
        _mission.Text = $"Mission {Mathf.Min(_missions.CompletedMissions + 1, _missions.MissionCount)} / {_missions.MissionCount}";
        _task.Text = _game.IsVictorious ? "All missions completed" : _game.IsDefeated ? _game.ResultReason : _missions.HasCargo ? "Deliver cargo to the green beacon" : "Fly to pickup — orange beacon";
        _cargo.Text = _missions.HasCargo ? "Cargo: acquired" : "Cargo: none";
        _timer.Text = $"Mission time: {Mathf.CeilToInt(_missions.TimeRemaining)} s";
        _points.Text = $"Score: {_score.TotalScore}";
        _mode.Text = $"{_difficulty.Level}  •  R — restart";
    }

    private void ShowResult()
    {
        RefreshHud();
        _resultTitle.Text = _stats.Won ? "DELIVERY COMPLETE" : "FLIGHT FAILED";
        string best = _save.Data.BestCompletionTime is double time ? $"{time:F1} s" : "—";
        _resultText.Text = $"{_game.ResultReason}\n\nTime: {_stats.ElapsedSeconds:F1} s\nDeliveries: {_stats.Deliveries} / {_missions.MissionCount}\nDamage events: {_stats.DamageEvents}\nScore: {_score.TotalScore}\nBest completion: {best}\n" +
            (_save.LastSaveSucceeded ? "Statistics saved locally" : "Statistics could not be saved");
        _overlay.Show();
        _restart.GrabFocus();
    }

    public override void _ExitTree()
    {
        if (GodotObject.IsInstanceValid(_stats)) _stats.Completed -= ShowResult;
    }
}
