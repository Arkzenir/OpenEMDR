using Godot;
using Godot.Collections;


public partial class ControlPanel : CanvasLayer
{
    [Export] public StimulusManager StimulusManager;
    [Export] public Panel UIPanel;
    [Export] public AudioController AudioController;
    [Export] public SubViewport PreviewViewport;
    [Export] public Array<Node3D> WorldsList; //Godot array for available worlds
    

    private XRServerInstance xrServer;
    private OptionButton worldSelector;

    public override void _Ready()
    {
        xrServer = XRServer.Singleton;

        // Sliders
        var speedSlider = UIPanel.GetNode<HSlider>("SpeedSlider");
        speedSlider.ValueChanged += (value) => StimulusManager.SetSpeed((float)value);

        var rangeSlider = UIPanel.GetNode<HSlider>("RangeSlider");
        rangeSlider.ValueChanged += (value) => StimulusManager.SetRange((float)value);

        var distanceSlider = UIPanel.GetNode<HSlider>("DistanceSlider");
        distanceSlider.ValueChanged += (value) => StimulusManager.SetDistance((float)value);

        var sizeSlider = UIPanel.GetNode<HSlider>("SizeSlider");
        sizeSlider.ValueChanged += (value) => StimulusManager.SetScale((float)value);

        // Stimulus Type (2D or 3D)
        var typeSelector = UIPanel.GetNode<OptionButton>("StimulusType");
        typeSelector.ItemSelected += (index) => SetStimulusType(index);

        // World selection
        worldSelector = UIPanel.GetNode<OptionButton>("WorldType");
        worldSelector.ItemSelected += (index) => SetWorldType(index);

        // Sound Toggle
        var soundToggle = UIPanel.GetNode<CheckButton>("SoundToggle");
        soundToggle.Toggled += ToggleSound;

        // Hook up preview
        var preview = UIPanel.GetNode<TextureRect>("XRPreview");
        preview.Texture = PreviewViewport.GetTexture();  // Live headset feed

        // Start/Pause Button
        var startPauseButton = UIPanel.GetNode<Button>("StartPauseButton");
        startPauseButton.Pressed += TogglePause;

        var emergencyStop = UIPanel.GetNode<Button>("EmergencyButton");
        emergencyStop.Pressed += EmergencyStop;

        // Reset Buttons
        var resetButton = UIPanel.GetNode<Button>("ResetButton");
        resetButton.Pressed += ResetScene;

        var resetViewButton = UIPanel.GetNode<Button>("ResetViewButton");
        resetViewButton.Pressed += () => CallDeferred(nameof(RecenterXROrigin));

        SetStimulusType(typeSelector.Selected);
    }

    private void SetStimulusType(long stimIndex)
    {
        StimulusManager.SetStimulusType(stimIndex);
        if (stimIndex == 0)
        {
            SetWorldType(0);
            worldSelector.Visible = false;
        }
        else worldSelector.Visible = true;
        
    }

    private void SetWorldType(long worldIndex)
    {
        for (int i = 0; i < WorldsList.Count; i++)
        {
            if (worldIndex != 0 && i == worldIndex - 1)
            {
                WorldsList[i].Visible = true;
                continue;
            }
            WorldsList[i].Visible = false;
        }
    }

    private void ToggleSound(bool enable)
    {
        AudioController.ToggleSound(enable);
    }
    private void RecenterXROrigin()
    {
        xrServer.CenterOnHmd(XRServer.RotationMode.ResetButKeepTilt, true);
    }

    private void TogglePause()
    {
        StimulusManager.TogglePaused();
    }

    private void ResetScene()
    {
        StimulusManager.ResetScene();
    }

    private void EmergencyStop()
    {
        ResetScene();
        GD.Print("Emergency Stop Initiated");
    }
}
