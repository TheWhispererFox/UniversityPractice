using Godot;

public partial class PickupZone : Area3D
{
    [Export] public int MissionId { get; set; }
    [Signal] public delegate void PlayerEnteredEventHandler(Node3D player);

    public override void _Ready()
    {
        BodyEntered += body =>
        {
            if (body is PlayerController) EmitSignal(SignalName.PlayerEntered, body);
        };
    }

    public void SetActive(bool active)
    {
        GetNode<OmniLight3D>("Beacon").Visible = active;
        GetNode<Node3D>("Marker").Visible = active;
    }
}
