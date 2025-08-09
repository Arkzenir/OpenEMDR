using Godot;

public partial class StimulusManager : Node3D
{
    [Export] public float Speed = 0f;  // Movement speed
    [Export] public float Range = 0f;  // Current max distance from center
    [Export] public float Distance = 5.0f; // Current distance from center
    [Export] public float StimScale = 1.0f; // Scale of the stimulus 
    [Export] public float MaxSpeed = 10.0f;
    [Export] public float MaxDistance = 5.0f;
    [Export] public float MaxRange = 3.0f; // Possible max distance from center
    [Export] public float MaxScale = 3.0f; // Possible max scale of object

    [Export] public Node3D SpriteStimulus;
    [Export] public Node3D MeshStimulus;
    [Export] public AudioController audioController;

    private Node3D activeStimulus;
    
    private float time;
    private bool paused = true;

    public override void _PhysicsProcess(double delta)
    {
        if (paused || activeStimulus == null)
            return;

        time += (float)delta * (Speed);
        float x = Mathf.Sin(time) * Range;
        activeStimulus.Position = new Vector3(x, activeStimulus.Position.Y, Distance);
        activeStimulus.Scale = new Vector3(StimScale, StimScale, StimScale);
        // Update audio
        audioController?.UpdateStimulusPosition(x, Range);
    }

    public void SetSpeed(float newSpeed) => Speed = (newSpeed / 100.0f) * MaxSpeed;
    public void SetRange(float newRange) => Range = (newRange / 100.0f) * MaxRange;
    public void SetDistance(float newDist) => Distance = -(newDist / 100.0f) * MaxDistance - 1.0f;
    public void SetScale(float newScale) => StimScale = (newScale / 100.0f) * MaxScale + 0.1f; 
    public void TogglePaused() => paused = !paused;
    

    //Center Stimulus and Pause
    public void ResetScene()
    {
        paused = true;
        activeStimulus.Position = new Vector3(0, 0, activeStimulus.Position.Z);
    }

    // 0 = Sprite, 1 = Mesh
    public void SetStimulusType(long typeIndex)
    {
        SpriteStimulus.Visible = (typeIndex == 0);
        MeshStimulus.Visible = (typeIndex == 1);

        activeStimulus = typeIndex == 0 ? SpriteStimulus : MeshStimulus;
    }
}
