using Godot;
using System;

public partial class Stimulus : Node3D
{
    [Export] public float Speed = 0f;  // Movement speed
    [Export] public float Range = 2.0f;  // Max distance from center
    [Export] public float Distance = 5.0f;
    [Export] public NodePath MeshStimulus;
    [Export] public NodePath SpriteStimulus;
    [Export] public NodePath AudioController;

    private Node3D spriteStimulus;
    private Node3D meshStimulus;
    private Node3D activeStimulus;
    private AudioController audioController;
    private float time;
    private bool paused = true;

    public override void _Ready()
    {
        spriteStimulus = GetNode<Node3D>(MeshStimulus);
        meshStimulus = GetNode<Node3D>(SpriteStimulus);
        audioController = GetNode<AudioController>(AudioController);

        SetStimulusType(0);
    }

    public override void _PhysicsProcess(double delta)
    {
        if (paused || activeStimulus == null)
            return;

        time += (float)delta * Speed;
        float x = Mathf.Sin(time) * Range;
        activeStimulus.Position = new Vector3(x, 0, activeStimulus.Position.Z);

        // Optional: Update audio
        audioController?.UpdateStimulusPosition(x, Range);
    }

    public void SetSpeed(float newSpeed) => Speed = newSpeed;
    public void SetRange(float newRange) => Range = newRange;
    public void TogglePaused() => paused = !paused;
    public void SetDistance(float newDist) => Distance = newDist;

    //Center Stimulus and Pause
    public void ResetScene()
    {
        paused = true;
        activeStimulus.Position = new Vector3(0, 0, activeStimulus.Position.Z);
    }

    // 0 = Sprite, 1 = Mesh
    public void SetStimulusType(long typeIndex)
    {
        spriteStimulus.Visible = (typeIndex == 0);
        meshStimulus.Visible = (typeIndex == 1);

        activeStimulus = typeIndex == 0 ? spriteStimulus : meshStimulus;
    }
}
