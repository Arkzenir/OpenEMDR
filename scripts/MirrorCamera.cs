using Godot;
using System;

public partial class MirrorCamera : Camera3D
{
    [Export] public NodePath HeadsetCameraPath;

    private XRCamera3D headsetCamera;

    public override void _Ready()
    {
        headsetCamera = GetNode<XRCamera3D>(HeadsetCameraPath);
    }

    public override void _Process(double delta)
    {
        if (headsetCamera == null) return;

        // Position and rotation from headset
        GlobalTransform = headsetCamera.GlobalTransform;
    }
}
