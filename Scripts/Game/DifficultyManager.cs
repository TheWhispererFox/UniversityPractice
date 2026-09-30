using Godot;

public partial class DifficultyManager : Node
{
    public enum Difficulty { Easy, Normal, Hard }
    [Export] public Difficulty Level { get; set; } = Difficulty.Normal;
    public float MissionSeconds => Level switch { Difficulty.Easy => 120f, Difficulty.Hard => 45f, _ => 75f };
    public float DamageMultiplier => Level switch { Difficulty.Easy => 0.5f, Difficulty.Hard => 1.5f, _ => 1f };

    public override void _Ready()
    {
        if (GetTree().HasMeta("difficulty")) Level = (Difficulty)(int)GetTree().GetMeta("difficulty");
        GetNode<PlayerController>("../Player").DamageMultiplier = DamageMultiplier;
        GD.Print($"Difficulty: {Level}. Mission time: {MissionSeconds}s; impact damage x{DamageMultiplier}.");
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event is not InputEventKey key || !key.Pressed || key.Echo) return;
        int selected = key.PhysicalKeycode switch { Key.Key1 => 0, Key.Key2 => 1, Key.Key3 => 2, _ => -1 };
        if (selected < 0) return;
        GetTree().SetMeta("difficulty", selected);
        GetViewport().SetInputAsHandled();
        GetTree().ReloadCurrentScene();
    }
}
