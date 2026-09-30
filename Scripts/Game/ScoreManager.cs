using Godot;

public partial class ScoreManager : Node
{
    [Signal] public delegate void ScoreChangedEventHandler(int total);
    public int TotalScore { get; private set; }
    public int LastMissionScore { get; private set; }
    public int LastTimeBonus { get; private set; }
    public int LastDamagePenalty { get; private set; }

    public void AwardDelivery(float secondsLeft, int damageTaken)
    {
        LastTimeBonus = Mathf.FloorToInt(Mathf.Max(0f, secondsLeft) * 10f);
        LastDamagePenalty = Mathf.Max(0, damageTaken) * 15;
        LastMissionScore = Mathf.Max(0, 500 + LastTimeBonus - LastDamagePenalty);
        TotalScore += LastMissionScore;
        EmitSignal(SignalName.ScoreChanged, TotalScore);
        GD.Print($"Score: 500 + {LastTimeBonus} time bonus - {LastDamagePenalty} damage = {LastMissionScore}. Total: {TotalScore}.");
    }
}
