using Godot;

// A visual parcel. MissionManager owns pickup/delivery rules.
public partial class Cargo : Node3D
{
    public bool IsCarried { get; private set; }

    public void PlaceAt(Node3D parent, Vector3 position)
    {
        Reparent(parent, false);
        Position = position;
        Rotation = Vector3.Zero;
        IsCarried = false;
        Show();
    }

    public void AttachTo(PlayerController player)
    {
        Reparent(player.GetNode<Node3D>("Visual"), false);
        Position = new Vector3(0, -0.9f, 0);
        Rotation = Vector3.Zero;
        IsCarried = true;
    }

    public void Clear()
    {
        IsCarried = false;
        Hide();
    }
}
