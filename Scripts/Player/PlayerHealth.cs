using Godot;

public partial class PlayerHealth : Node
{
    [Signal] public delegate void HealthChangedEventHandler(int currentHp);
    [Signal] public delegate void DiedEventHandler();

    public int CurrentHp { get; private set; } = 100;
    public bool IsDead => CurrentHp == 0;

    public void TakeDamage(int amount)
    {
        if (IsDead || amount <= 0) return;
        CurrentHp = Mathf.Max(0, CurrentHp - amount);
        EmitSignal(SignalName.HealthChanged, CurrentHp);
        if (IsDead) EmitSignal(SignalName.Died);
    }
}
