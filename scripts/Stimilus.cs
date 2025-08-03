using Godot;
using System;

public partial class Stimilus : Node3D
{
	[Export] public float Speed = 1.0f;  // Movement speed
	[Export] public float Range = 2.0f;  // Max distance from center
	[Export] public float Distance = 5.0f;
	[Export] public NodePath StimulusObject; // Child object (sprite/mesh)

	private Node3D stimulus;
	private float time = 0;

	public override void _Ready()
	{
		stimulus = GetNode<Node3D>(StimulusObject);
	}

	public override void _PhysicsProcess(double delta)
	{
		time += (float)delta * Speed;
		float x = Mathf.Sin(time) * Range;
		stimulus.Position = new Vector3(x, 0, stimulus.Position.Z);
	}

	public void SetSpeed(float newSpeed) => Speed = newSpeed;
	public void SetRange(float newRange) => Range = newRange;
	public void SetDistance(float newDist) => Distance = newDist;
}
