using Godot;
using System.Linq;

public partial class MissionManager : Node
{
    [Signal] public delegate void AllMissionsCompletedEventHandler();
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

    private bool CanInteract(Node3D body) => !_stopped && !_health.IsDead && body == _player;

    private void TryPickup(PickupZone zone, Node3D body)
    {
        if (!CanInteract(body) || HasCargo || zone != _pickups[CompletedMissions]) return;
        _cargo.AttachTo(_player);
        UpdateMarkers();
        GD.Print($"Cargo collected. Fly to Delivery {zone.MissionId} (green beacon).");
    }

    private void TryDeliver(DeliveryZone zone, Node3D body)
    {
        if (!CanInteract(body) || !HasCargo || zone != _destinations[CompletedMissions]) return;
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
        _cargo.Clear();
        UpdateMarkers();
    }
}
