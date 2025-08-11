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

    private string _host;
    private string _subnetBroadcast; // also send subnet-directed broadcast

    public override void _Ready()
    {
        _udp = new PacketPeerUdp();
        _udp.SetBroadcastEnabled(true);
        _host = PickPrivateIPv4();
        _subnetBroadcast = GuessSubnetBroadcast(_host); // e.g., 192.168.1.255
        GD.Print($"[DiscoveryBeacon] Advertising host: {_host}:{ServerPort} (bc:{_subnetBroadcast})");
    }

    public override void _Process(double delta)
    {
        _accum += delta;
        if (_accum < IntervalSec) return;
        _accum = 0;

        var msg = $"{{\"svc\":\"emdr-ctrl\",\"ver\":\"1\",\"port\":{ServerPort},\"host\":\"{_host}\"}}";
        var bytes = Encoding.UTF8.GetBytes(msg);

        // Limited broadcast
        _udp.SetDestAddress("255.255.255.255", BeaconPort);
        _udp.PutPacket(bytes);

        // Subnet-directed broadcast (helps on some routers)
        if (!string.IsNullOrEmpty(_subnetBroadcast))
        {
            _udp.SetDestAddress(_subnetBroadcast, BeaconPort);
            _udp.PutPacket(bytes);
        }
    }

    private static string PickPrivateIPv4()
    {
        foreach (var s in IP.GetLocalAddresses())
        {
            var ip = s.ToString();
            if (IsPrivateIPv4(ip)) return ip;
        }
        return "0.0.0.0";
    }

    private static bool IsPrivateIPv4(string ip)
    {
        if (string.IsNullOrEmpty(ip) || ip.IndexOf('.') < 0) return false;
        if (ip.StartsWith("10.") || ip.StartsWith("192.168.")) return true;
        if (ip.StartsWith("172."))
        {
            var parts = ip.Split('.');
            if (parts.Length > 1 && int.TryParse(parts[1], out var b)) return b >= 16 && b <= 31;
        }
        return false;
    }

    private static string GuessSubnetBroadcast(string ip)
    {
        var p = ip.Split('.');
        return p.Length == 4 ? $"{p[0]}.{p[1]}.{p[2]}.255" : null;
    }
}
