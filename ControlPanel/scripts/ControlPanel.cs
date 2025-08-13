using Godot;
using Godot.Collections;

public partial class ControlPanel : CanvasLayer
{
    [Export] public Panel UIPanel;
    [Export] public SubViewport PreviewViewport;

    [Export] public StimulusManager stimulusManager;

    [Export] public NetServer Net;           // Server for transmiting commands
    [Export] public DiscoveryBeacon Beacon;  // UDP beacon for advertising the server local adress and port

    private OptionButton _worldSelector;
    private bool _paused = true;

    // ~60 fps throttler for slider messages
    private double _lastSetSentTime;
    private void SendSetThrottled(Param p, float v)
    {
        var now = Time.GetUnixTimeFromSystem();
        if (now - _lastSetSentTime >= (1.0 / 60.0))
        {
            _lastSetSentTime = now;
            Broadcast(Cmd.Set(p, v));
        }
    }

    private void Broadcast(Command cmd)
    {
        if (Net == null)
        {
            GD.PushWarning("NetServer is not assigned on ControlPanel — no commands will be sent.");
            return;
        }
        Net.Broadcast(cmd);
    }

    public override void _Ready()
    {
        if (UIPanel == null)
        {
            GD.PushError("UIPanel export is not assigned. Hook it in the editor.");
            return;
        }

        Broadcast(Cmd.Hello("desk-1.0")); // one-time sanity ping; headset logs "Hello from desktop vdesk-1.0"

        // Sliders send throttled updates
        var speedSlider = UIPanel.GetNode<HSlider>("SpeedSlider");
        speedSlider.ValueChanged += (value) =>
        {
            SendSetThrottled(Param.Speed, (float)value);
            stimulusManager.SetSpeed((float)value);
        };
        var rangeSlider = UIPanel.GetNode<HSlider>("RangeSlider");
        rangeSlider.ValueChanged += (value) =>
        {
            SendSetThrottled(Param.Range, (float)value);
            stimulusManager.SetRange((float)value);
        };
        var distanceSlider = UIPanel.GetNode<HSlider>("DistanceSlider");
        distanceSlider.ValueChanged += (value) =>
        {
            SendSetThrottled(Param.Distance, (float)value);
            stimulusManager.SetDistance((float)value);
        };
        var sizeSlider = UIPanel.GetNode<HSlider>("SizeSlider");
        sizeSlider.ValueChanged += (value) =>
        {
            SendSetThrottled(Param.Scale, (float)value);
            stimulusManager.SetScale((float)value);
        };

        // Stimulus Type (2D or 3D)
        var typeSelector = UIPanel.GetNode<OptionButton>("StimulusType");
        typeSelector.ItemSelected += (index) =>
        {
            Broadcast(Cmd.StimType((int)index));
            stimulusManager.SetStimulusType((int)index);
            _worldSelector.Visible = (index == 1);
        };

        // World selection
        _worldSelector = UIPanel.GetNode<OptionButton>("WorldType");
        _worldSelector.ItemSelected += (index) =>
        {
            Broadcast(Cmd.World((int)index));
            stimulusManager.SetWorldType((int)index);
        };

        // Sound Toggle
        var soundToggle = UIPanel.GetNode<CheckButton>("SoundToggle");
        soundToggle.Toggled += (on) =>
        {
            Broadcast(Cmd.Sound(on));
        };

        var ARToggle = UIPanel.GetNode<CheckButton>("ARToggle");
        ARToggle.Toggled += (on) =>
        {
            Broadcast(Cmd.AR(on));
            stimulusManager.SetARPassthrough(on);
        };

        // Start/Pause
        var startPauseButton = UIPanel.GetNode<Button>("StartPauseButton");
        startPauseButton.Pressed += TogglePause;

        // Emergency Stop
        var emergencyStop = UIPanel.GetNode<Button>("EmergencyButton");
        emergencyStop.Pressed += EmergencyStop;

        // Reset scene
        var resetButton = UIPanel.GetNode<Button>("ResetButton");
        resetButton.Pressed += ResetScene;

        // Reset view (recenter on horizontal axis)
        var resetViewButton = UIPanel.GetNode<Button>("ResetViewButton");
        resetViewButton.Pressed += () => Broadcast(Cmd.Recenter());

        // Headset preview texture hookup
        var preview = UIPanel.GetNode<TextureRect>("XRPreview");
        if (PreviewViewport != null)
            preview.Texture = PreviewViewport.GetTexture();

        // Send initial state so headset starts consistent with the UI
        Broadcast(Cmd.StimType(typeSelector.Selected));

    }

    private void TogglePause()
    {
        _paused = !_paused;
        Broadcast(Cmd.Pause(_paused));
        stimulusManager.TogglePaused();
    }

    private void ResetScene()
    {
        Broadcast(Cmd.Reset());
        stimulusManager.ResetScene();
    }

    private void EmergencyStop()
    {
        Broadcast(Cmd.Emergency());
        stimulusManager.EmergencyStop();
    }
}
