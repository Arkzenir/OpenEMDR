using Godot;
using System.Text;

/// Broadcasts a small UDP JSON beacon so headsets can discover us without hardcoded IPs.
public partial class DiscoveryBeacon : Node
{
    [Export] public int BeaconPort = 50101;      // UDP port for discovery
    [Export] public int ServerPort = 9080;       // WebSocket server port
    [Export] public float IntervalSec = 0.5f;    // Broadcast interval

    private PacketPeerUdp _udp;
    private double _accum;

    public override void _Ready()
    {
        _udp = new PacketPeerUdp();
        _udp.SetBroadcastEnabled(true);
    }

    public override void _Process(double delta)
    {
        _accum += delta;
        if (_accum < IntervalSec) return;
        _accum = 0;

        var msg = $"{{\"svc\":\"emdr-ctrl\",\"ver\":\"1\",\"port\":{ServerPort}}}";
        var bytes = Encoding.UTF8.GetBytes(msg);

        _udp.SetDestAddress("255.255.255.255", BeaconPort);
        _udp.PutPacket(bytes);
    }
}
