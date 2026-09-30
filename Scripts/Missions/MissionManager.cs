using Godot;
using System.Linq;

public partial class MissionManager : Node
{
    [Signal] public delegate void AllMissionsCompletedEventHandler();
    [Signal] public delegate void MissionTimedOutEventHandler();
    public float TimeRemaining { get; private set; }
    public float TimeLimit { get; private set; }
    private bool _timerRunning;
    private int _startingHp;
    private int _lastReportedSeconds;
    private DifficultyManager _difficulty;
    private ScoreManager _score;
    private DeliveryEffects _effects;
    public int CompletedMissions { get; private set; }
    public int MissionCount => _pickups.Length;
    public bool HasCargo => _cargo.IsCarried;

    private PickupZone[] _pickups;
    private DeliveryZone[] _destinations;
    private PlayerController _player;
    private PlayerHealth _health;
    private Cargo _cargo;
    private bool _stopped;

    public override void _Ready()
    {
        _difficulty = GetNode<DifficultyManager>("../DifficultyManager");
        _score = GetNode<ScoreManager>("../ScoreManager");
        _effects = GetNode<DeliveryEffects>("../DeliveryEffects");
        _player = GetNode<PlayerController>("../Player");
        _health = _player.GetNode<PlayerHealth>("PlayerHealth");
        _cargo = GetNode<Cargo>("../Cargo");
        _pickups = GetNode("../Zones").GetChildren().OfType<PickupZone>().OrderBy(zone => zone.MissionId).ToArray();
        _destinations = GetNode("../Zones").GetChildren().OfType<DeliveryZone>().OrderBy(zone => zone.MissionId).ToArray();
        // Fail clearly if a level contains missing or ambiguous route pairs.
        if (_pickups.Length == 0 || _pickups.Length != _destinations.Length ||
            _pickups.Select(zone => zone.MissionId).Distinct().Count() != _pickups.Length ||
            !_pickups.Select(zone => zone.MissionId).SequenceEqual(_destinations.Select(zone => zone.MissionId)))
        {
            GD.PushError("Each mission needs one unique pickup and matching delivery zone.");
            _stopped = true;
            return;
        }
        foreach (var zone in _pickups)
            zone.PlayerEntered += body => TryPickup(zone, body);
        foreach (var zone in _destinations)
            zone.PlayerEntered += body => TryDeliver(zone, body);
        _health.Died += Stop;
        // Wait until the level finishes adding children before moving the parcel.
        Callable.From(BeginMission).CallDeferred();
    }

    public override void _PhysicsProcess(double delta)
    {
        if (!_timerRunning || _stopped) return;
        TimeRemaining = Mathf.Max(0f, TimeRemaining - (float)delta);
        int seconds = Mathf.CeilToInt(TimeRemaining);
        if (seconds != _lastReportedSeconds && (seconds % 10 == 0 || seconds <= 5))
            GD.Print($"Mission time left: {seconds}s.");
        _lastReportedSeconds = seconds;
        if (TimeRemaining > 0f) return;
        Stop();
        EmitSignal(SignalName.MissionTimedOut);
    }

    private bool CanInteract(Node3D body) => !_stopped && !_health.IsDead && body == _player;

    private void TryPickup(PickupZone zone, Node3D body)
    {
        if (!CanInteract(body) || HasCargo || zone != _pickups[CompletedMissions]) return;
        _cargo.AttachTo(_player);
        _effects.PlayPickup(_player.GlobalPosition);
        UpdateMarkers();
        GD.Print($"Cargo collected. Fly to Delivery {zone.MissionId} (green beacon).");
    }

    private void TryDeliver(DeliveryZone zone, Node3D body)
    {
        if (!CanInteract(body) || !HasCargo || zone != _destinations[CompletedMissions]) return;
        _effects.PlayDelivery(_player.GlobalPosition);
        _score.AwardDelivery(TimeRemaining, _startingHp - _health.CurrentHp);
        _cargo.Clear();
        CompletedMissions++;
        GD.Print($"Delivered: {CompletedMissions}/{MissionCount}.");
        if (CompletedMissions == MissionCount)
        {
            Stop();
            EmitSignal(SignalName.AllMissionsCompleted);
        }
        else BeginMission();
    }

    private void BeginMission()
    {
        if (_stopped) return;
        TimeLimit = _difficulty.MissionSeconds;
        TimeRemaining = TimeLimit;
        _startingHp = _health.CurrentHp;
        _timerRunning = true;
        _lastReportedSeconds = Mathf.CeilToInt(TimeRemaining);
        GD.Print($"Mission timer: {TimeRemaining}s.");
        _cargo.PlaceAt(_pickups[CompletedMissions], new Vector3(0, 0.8f, 0));
        UpdateMarkers();
        GD.Print($"Mission {CompletedMissions + 1}/{MissionCount}: Pickup {_pickups[CompletedMissions].MissionId} (orange beacon).");
    }

    private void UpdateMarkers()
    {
        for (int i = 0; i < MissionCount; i++)
        {
            _pickups[i].SetActive(!_stopped && i == CompletedMissions && !HasCargo);
            _destinations[i].SetActive(!_stopped && i == CompletedMissions && HasCargo);
        }
    }

    private void Stop()
    {
        _stopped = true;
        _timerRunning = false;
        _cargo.Clear();
        UpdateMarkers();
    }
}
