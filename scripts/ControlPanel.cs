using Godot;
using System;
using System.Diagnostics;

public partial class ControlPanel : CanvasLayer
{
    [Export] public NodePath StimulusPath;
    [Export] public NodePath PanelPath;
    private Stimulus stimulus;
    private Panel UIPanel;
    private XRInterface xrInterface;
    private bool paused = false;

    public override void _Ready()
    {
        stimulus = GetNode<Stimulus>(StimulusPath);
        xrInterface = XRServer.Singleton.PrimaryInterface;
        UIPanel = GetNode<Panel>(PanelPath);
        
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

        // AR Mode toggle
        var arToggle = UIPanel.GetNode<CheckBox>("ARMode");
        arToggle.Toggled += ToggleARMode;

        // Pause Button
        var pauseButton = UIPanel.GetNode<Button>("PauseButton");
        pauseButton.Pressed += TogglePause;
    }

    private void ToggleARMode(bool enable)
    {
        if (xrInterface == null) return;

        xrInterface.EnvironmentBlendMode = 
            enable ? XRInterface.EnvironmentBlendModeEnum.AlphaBlend 
                   : XRInterface.EnvironmentBlendModeEnum.Opaque;
    }

    private void TogglePause()
    {
        paused = !paused;
        stimulus.SetPaused(paused);
    }
}
