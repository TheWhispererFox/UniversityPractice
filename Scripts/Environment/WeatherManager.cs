using Godot;

public partial class WeatherManager : Node3D
{
    public enum Weather { Clear, Rain, Fog }
    [Export] public Weather Mode { get; set; } = Weather.Clear;
    public Vector3 WindVelocity { get; private set; }
    private Godot.Environment _environment;
    private CpuParticles3D _rain;
    private PlayerController _player;
    private float _elapsed;

    public override void _Ready()
    {
        _player = GetNode<PlayerController>("../Player");
        var world = GetNode<WorldEnvironment>("../WorldEnvironment");
        _environment = (Godot.Environment)world.Environment.Duplicate();
        world.Environment = _environment;
        _rain = GetNode<CpuParticles3D>("Rain");
        if (GetTree().HasMeta("weather")) Mode = (Weather)(int)GetTree().GetMeta("weather");
        SetWeather((int)Mode);
    }

    public void SetWeather(int mode)
    {
        if (mode < 0 || mode > 2) return;
        Mode = (Weather)mode;
        GetTree().SetMeta("weather", mode);
        _environment.FogEnabled = Mode != Weather.Clear;
        _environment.FogDensity = Mode == Weather.Fog ? 0.035f : 0.006f;
        _environment.FogLightColor = new Color(0.55f, 0.65f, 0.72f);
        _environment.AmbientLightEnergy = Mode == Weather.Clear ? 0.7f : 0.5f;
        _rain.Emitting = Mode == Weather.Rain;
        // Hide remaining drops immediately when switching out of rain.
        _rain.Visible = Mode == Weather.Rain;
        UpdateWind();
        GD.Print($"Weather: {Mode}. Keys 4/5/6: Clear/Rain/Fog.");
    }

    public override void _PhysicsProcess(double delta)
    {
        _elapsed += (float)delta;
        UpdateWind();
        _rain.GlobalPosition = _player.GlobalPosition + Vector3.Up * 7f;
    }

    private void UpdateWind()
    {
        float strength = Mode switch { Weather.Rain => 1.8f, Weather.Fog => 0.6f, _ => 0f };
        WindVelocity = new Vector3(1f, 0f, 0.35f).Normalized() * strength * (0.8f + 0.2f * Mathf.Sin(_elapsed));
        _player.WindVelocity = WindVelocity;
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event is not InputEventKey key || !key.Pressed || key.Echo) return;
        int mode = key.PhysicalKeycode switch { Key.Key4 => 0, Key.Key5 => 1, Key.Key6 => 2, _ => -1 };
        if (mode >= 0) SetWeather(mode);
    }
}
