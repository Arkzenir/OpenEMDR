using Godot;
using System;

public partial class AudioController : Node3D
{
    [Export] public NodePath LeftPlayerPath;
    [Export] public NodePath RightPlayerPath;
    [Export] public float TriggerThreshold = 0.95f; // Play near edges (Might change later/Become seperate)

    private AudioStreamPlayer3D leftPlayer;
    private AudioStreamPlayer3D rightPlayer;
    private float lastX = 0;

    public override void _Ready()
    {
        leftPlayer = GetNode<AudioStreamPlayer3D>(LeftPlayerPath);
        rightPlayer = GetNode<AudioStreamPlayer3D>(RightPlayerPath);
    }

    public void UpdateStimulusPosition(float x, float maxRange)
    {
        float normalized = x / maxRange;

        // Left trigger
        if (normalized <= -TriggerThreshold && lastX > -TriggerThreshold)
            leftPlayer.Play();

        // Right trigger
        if (normalized >= TriggerThreshold && lastX < TriggerThreshold)
            rightPlayer.Play();

        lastX = normalized;
    }
}
