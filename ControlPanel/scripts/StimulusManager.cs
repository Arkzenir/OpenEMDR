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
    [Export] public Godot.Collections.Array<Node3D> WorldsList;

    private Node3D activeStimulus;
    private long currentWorld = 0;
    private long worldBeforeAR = 0;
    private float time;
    private bool paused = true;

    public override void _Ready()
    {
        // Ensure a valid default so reset can’t null-ref if a command arrives first
        SetStimulusType(0);
    }

    public override void _PhysicsProcess(double delta)
    {
        if (paused || activeStimulus == null)
            return;

        time += (float)delta * (Speed);
        float x = Mathf.Sin(time) * Range;
        activeStimulus.Position = new Vector3(x, activeStimulus.Position.Y, Distance);
        activeStimulus.Scale = new Vector3(StimScale, StimScale, StimScale);
        // Do not update audio in control panel (to be removed later)
        //audioController?.UpdateStimulusPosition(x, Range);
    }

    public void SetSpeed(float newSpeed) => Speed = (newSpeed / 100.0f) * MaxSpeed;
    public void SetRange(float newRange) => Range = (newRange / 100.0f) * MaxRange;
    public void SetDistance(float newDist) => Distance = -(newDist / 100.0f) * MaxDistance - 1.0f;
    public void SetScale(float newScale) => StimScale = (newScale / 100.0f) * MaxScale + 0.1f;
    public void TogglePaused()
    {
        paused = !paused;
        activeStimulus.Visible = true; //Ensure something is visible
    }

    public void ToggleAudio(bool enable)
    {
        audioController.ToggleSound(enable);
    }

    public void SetARPassthrough(bool enable)
    {
        if (enable)
        {
            worldBeforeAR = currentWorld;
            SetWorldType(0);
        }
        else
        {
            SetWorldType(worldBeforeAR);
        }
    }

    //Center Stimulus and Pause
    public void ResetScene()
    {
        paused = true;
        if (activeStimulus != null)
        {
            activeStimulus.Position = new Vector3(0, activeStimulus.Position.Y, activeStimulus.Position.Z);  // Reset position
            activeStimulus.Visible = true; //Ensure something is visible
        }
            
    }

    // 0 = Sprite, 1 = Mesh
    public void SetStimulusType(long typeIndex)
    {
        SpriteStimulus.Visible = (typeIndex == 0);
        MeshStimulus.Visible = (typeIndex == 1);

        activeStimulus = typeIndex == 0 ? SpriteStimulus : MeshStimulus;

        if (typeIndex == 0) SetWorldType(0);
    }

    public void SetWorldType(long worldIndex)
    {
        currentWorld = worldIndex;
        for (int i = 0; i < WorldsList.Count; i++)
            WorldsList[i].Visible = (worldIndex != 0 && i == worldIndex - 1);
    }

    public void EmergencyStop()
    {
        ResetScene();
        //Emergency results in void
        SetWorldType(0);
        activeStimulus.Visible = false;
    }
}
