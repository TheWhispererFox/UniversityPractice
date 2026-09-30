using Godot;

public partial class PropellerAnimator : Node3D
{
	[Export] public Node3D[] Propellers { get; set; } = [];

	[Export]
	public float RotationSpeed { get; set; } = 1500.0f; // градусов/сек

	public override void _Process(double delta)
	{
		float rotation =
			Mathf.DegToRad(RotationSpeed) * (float)delta;

		foreach (var propeller in Propellers)
		{
			if (propeller != null)
				propeller.RotateY(rotation);
		}
	}
}
