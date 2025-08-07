using Godot;
using System;
using System.Diagnostics;

public partial class ControlPanel : CanvasLayer
{
    [Export] public NodePath StimulusPath;
    [Export] public NodePath PanelPath;
    [Export] public NodePath AudioControllerPath;
    [Export] public NodePath PreviewViewportPath;
    
    private Stimulus stimulus;
    private Panel UIPanel;
    private AudioController audioController;
    private XRServerInstance xrServer;
    private SubViewport xrViewport;

    public override void _Ready()
    {
        stimulus = GetNode<Stimulus>(StimulusPath);
        xrServer = XRServer.Singleton;
        UIPanel = GetNode<Panel>(PanelPath);
        audioController = GetNode<AudioController>(AudioControllerPath);

        // Sliders
        var speedSlider = UIPanel.GetNode<HSlider>("SpeedSlider");
        speedSlider.ValueChanged += (value) => stimulus.SetSpeed((float)value);

        var rangeSlider = UIPanel.GetNode<HSlider>("RangeSlider");
        rangeSlider.ValueChanged += (value) => stimulus.SetRange((float)value);

        var distanceSlider = UIPanel.GetNode<HSlider>("DistanceSlider");
        rangeSlider.ValueChanged += (value) => stimulus.SetDistance((float)value);

        // Stimulus Type (2D or 3D)
        var typeSelector = UIPanel.GetNode<OptionButton>("StimulusType");
        typeSelector.ItemSelected += (index) => stimulus.SetStimulusType(index);

        // Sound Toggle
        var soundToggle = UIPanel.GetNode<CheckButton>("SoundToggle");
        soundToggle.Toggled += ToggleSound;

        // Hook up preview
        var preview = UIPanel.GetNode<TextureRect>("XRPreview");
        xrViewport = GetNode<SubViewport>(PreviewViewportPath);
        preview.Texture = xrViewport.GetTexture();  // Live headset feed

        // Start/Pause Button
        var startPauseButton = UIPanel.GetNode<Button>("StartPauseButton");
        startPauseButton.Pressed += TogglePause;

        // Reset Button
        var resetButton = UIPanel.GetNode<Button>("ResetButton");
        resetButton.Pressed += ResetScene;

        

        var resetViewButton = UIPanel.GetNode<Button>("ResetViewButton");
        resetViewButton.Pressed += () => CallDeferred(nameof(RecenterXROrigin));;
    }

    private void ToggleSound(bool enable)
    {
        audioController.ToggleSound(enable);
    }
    private void RecenterXROrigin()
    {
        xrServer.CenterOnHmd(XRServer.RotationMode.ResetButKeepTilt, true);
    }

    private void TogglePause()
    {
        stimulus.TogglePaused();
    }

    private void ResetScene()
    {
        stimulus.ResetScene();
    }
}
