using Godot;

public partial class DeliveryEffects : Node3D
{
    public void PlayPickup(Vector3 position) => Play("Pickup", position);
    public void PlayDelivery(Vector3 position) => Play("Delivery", position);

    private void Play(string name, Vector3 position)
    {
        var particles = GetNode<CpuParticles3D>(name);
        particles.GlobalPosition = position;
        particles.Restart();
        particles.Emitting = true;
    }
}
