using Godot;
using System;
using System.Text.Json;

public partial class SaveManager : Node
{
    // Tests use a separate path through SceneTree metadata, never the player's file.
    public string StoragePath => GetTree().HasMeta("test_stats_path")
        ? (string)GetTree().GetMeta("test_stats_path") : "user://statistics.json";
    public Totals Data { get; private set; } = new();
    public bool LastSaveSucceeded { get; private set; } = true;
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase, WriteIndented = true };

    public sealed class Totals
    {
        public int TotalRuns { get; set; }
        public int TotalWins { get; set; }
        public int TotalLosses { get; set; }
        public int TotalDeliveries { get; set; }
        public double? BestCompletionTime { get; set; }
    }

    public override void _Ready() => LoadStats();

    public void LoadStats()
    {
        Data = new Totals();
        if (!FileAccess.FileExists(StoragePath)) return;
        try
        {
            using var file = FileAccess.Open(StoragePath, FileAccess.ModeFlags.Read);
            if (file == null) throw new InvalidOperationException("Cannot open statistics file.");
            var data = JsonSerializer.Deserialize<Totals>(file.GetAsText(), JsonOptions);
            if (data == null || data.TotalRuns < 0 || data.TotalWins < 0 || data.TotalLosses < 0 || data.TotalDeliveries < 0 ||
                (long)data.TotalWins + data.TotalLosses != data.TotalRuns ||
                (data.BestCompletionTime is double time && (!double.IsFinite(time) || time < 0)))
                throw new InvalidOperationException("Invalid statistics values.");
            Data = data;
        }
        catch (Exception error) when (error is JsonException or InvalidOperationException or NotSupportedException)
        {
            GD.PushWarning($"Statistics unavailable; starting with empty totals. {error.Message}");
        }
    }

    public void RecordRun(bool won, double elapsed, int deliveries)
    {
        Data.TotalRuns++;
        if (won) Data.TotalWins++; else Data.TotalLosses++;
        Data.TotalDeliveries += deliveries;
        if (won && (!Data.BestCompletionTime.HasValue || elapsed < Data.BestCompletionTime.Value))
            Data.BestCompletionTime = elapsed;
        LastSaveSucceeded = false;
        string tempPath = StoragePath + ".tmp";
        try
        {
            using (var file = FileAccess.Open(tempPath, FileAccess.ModeFlags.Write))
            {
                if (file == null) throw new InvalidOperationException("Cannot write statistics file.");
                file.StoreString(JsonSerializer.Serialize(Data, JsonOptions));
                file.Flush();
                if (file.GetError() != Error.Ok) throw new InvalidOperationException("Statistics write failed.");
            }
            LastSaveSucceeded = DirAccess.RenameAbsolute(ProjectSettings.GlobalizePath(tempPath), ProjectSettings.GlobalizePath(StoragePath)) == Error.Ok;
            if (!LastSaveSucceeded) GD.PushWarning("Cannot replace statistics file.");
        }
        catch (Exception error) when (error is JsonException or InvalidOperationException or NotSupportedException)
        {
            GD.PushWarning($"Statistics not saved: {error.Message}");
        }
    }
}
