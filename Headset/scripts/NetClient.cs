using Godot;
using System;
using System.Text;
using System.Text.Json;
using System.Collections.Generic;

public partial class NetClient : Node
{
    [ExportGroup("Refs")]
    [Export] public StimulusManager StimulusManager;

    [ExportGroup("Network")]
    [Export] public int BeaconPort = 50101;   // must match desktop
    [Export] public int DefaultPort = 9080;   // fallback

    private PacketPeerUdp _udp;
    private StreamPeerTcp _tcp;
    private string _lastServerHost;
    private int _lastServerPort;
    private float _retryBackoff = 0.0f;// seconds
    private float _connectWait = 0f;

    // buffer for newline-delimited JSON messages
    private StringBuilder _rxBuf = new StringBuilder();

    private readonly Dictionary<Param, Action<float>> _setters = new();

    public override void _Ready()
    {
        // Map param → applier once
        _setters[Param.Speed] = (v) => StimulusManager.SetSpeed(v);
        _setters[Param.Range] = (v) => StimulusManager.SetRange(v);
        _setters[Param.Distance] = (v) => StimulusManager.SetDistance(v);
        _setters[Param.Scale] = (v) => StimulusManager.SetScale(v);

        _udp = new PacketPeerUdp();
        var bindErr = _udp.Bind(BeaconPort, "0.0.0.0");
        if (bindErr != Error.Ok)
            GD.PushError($"UDP bind failed: {bindErr}");

        _tcp = new StreamPeerTcp();

        // Try last remembered server (if any)
        //if (LoadLastServer(out var host, out var port))
        //    TryConnect(host, port);
    }

    public override void _Process(double delta)
    {
        PollUdp();
        PollTcp(delta);
    }

    private void TryConnect(string host, int port)
    {
        // Always create a fresh socket before a new connect attempt
        try { _tcp?.DisconnectFromHost(); } catch { }
        _tcp = new StreamPeerTcp();
        
        _lastServerHost = host;
        _lastServerPort = port;

        var err = _tcp.ConnectToHost(host, (ushort)port);
        GD.Print($"[NetClient] TCP connecting to {host}:{port} (err={err})");
        _retryBackoff = 0.5f;
        _connectWait = 0f;
    }

    // If forcing localhost, ignore beacons entirely during editor/PC runs
    private void PollUdp()
    {
        while (_udp.GetAvailablePacketCount() > 0)
        {
            var packet = _udp.GetPacket();
            var txt = Encoding.UTF8.GetString(packet);
            try
            {
                using var doc = System.Text.Json.JsonDocument.Parse(txt);
                var root = doc.RootElement;
                if (!root.TryGetProperty("svc", out var svc) || svc.GetString() != "emdr-ctrl")
                    continue;

                int port = DefaultPort;
                if (root.TryGetProperty("port", out var p)) port = p.GetInt32();

                string host = null;
                if (root.TryGetProperty("host", out var h)) host = h.GetString();
                if (string.IsNullOrWhiteSpace(host))
                    host = _udp.GetPacketIP();

                GD.Print($"[NetClient] Beacon from {host}:{port}");

                if (_tcp.GetStatus() != StreamPeerTcp.Status.Connected ||
                    host != _lastServerHost || port != _lastServerPort)
                {
                    TryConnect(host, port);
                }
            }
            catch { /* ignore malformed beacons */ }
        }
    }
    private void PollTcp(double delta)
    {
        _tcp.Poll();
        var status = _tcp.GetStatus();

        if (status == StreamPeerTcp.Status.Connected)
        {
            // Normal read loop
            var available = _tcp.GetAvailableBytes();
            if (available > 0)
            {
                string chunk = _tcp.GetUtf8String((int)available);
                if (!string.IsNullOrEmpty(chunk))
                {
                    _rxBuf.Append(chunk);
                    var buf = _rxBuf.ToString();
                    int start = 0, nl;
                    while ((nl = buf.IndexOf('\n', start)) >= 0)
                    {
                        var line = buf.Substring(start, nl - start).Trim();
                        if (line.Length > 0)
                        {
                            GD.Print($"[NetClient] LINE: {line}");
                            try { Handle(line); }
                            catch (Exception e) { GD.PrintErr($"Bad packet: {e}\n{line}"); }
                        }
                        start = nl + 1;
                    }
                    _rxBuf.Clear();
                    if (start < buf.Length)
                        _rxBuf.Append(buf.AsSpan(start));
                }
            }
            _connectWait = 0f;
            return;
        }

        if (status == StreamPeerTcp.Status.Connecting)
        {
            GD.Print("[NetClient] Stuck at Connecting");
            return;
        }

        if (status == StreamPeerTcp.Status.None || status == StreamPeerTcp.Status.Error)
        {
            _retryBackoff -= (float)delta;
            if (_retryBackoff <= 0 && !string.IsNullOrEmpty(_lastServerHost))
                TryConnect(_lastServerHost, _lastServerPort);
        }
    }

    private void Handle(string json)
    {
        var cmd = Command.FromJson(json);
        GD.Print($"[NetClient] Handle: {json}");
        switch (cmd)
        {
            case SetCmd s when _setters.TryGetValue(s.K, out var applier):
                applier(s.V);
                break;

            case StimTypeCmd st:
                StimulusManager.SetStimulusType(st.I);
                break;

            case WorldCmd w:
                StimulusManager.SetWorldType(w.I);
                break;

            case ToggleSoundCmd snd:
                StimulusManager.ToggleAudio(snd.On);
                break;

            case ToggleARCmd ar:
                StimulusManager.SetARPassthrough(ar.On);
                break;


            case PauseCmd p:
                EnsurePaused(p.On);
                break;

            case ResetCmd:
                StimulusManager.ResetScene();
                break;

            case RecenterCmd:
                XRServer.Singleton.CenterOnHmd(XRServer.RotationMode.ResetButKeepTilt, true);
                break;

            case EmergencyCmd:
                StimulusManager.EmergencyStop();
                GD.Print("Emergency Stop (remote)");
                break;

            case HelloCmd h:
                GD.Print($"Hello from desktop v{h.Ver}");
                break;
        }
    }

    private bool _paused = true;
    private void EnsurePaused(bool shouldBePaused)
    {
        if (shouldBePaused != _paused)
            StimulusManager.TogglePaused();
        _paused = shouldBePaused;
    }

    /*
    private bool LoadLastServer(out string host, out int port)
    {
        host = null; port = DefaultPort;
        var path = "user://last_server.txt";
        if (FileAccess.FileExists(path))
        {
            using var f = FileAccess.Open(path, FileAccess.ModeFlags.Read);
            var s = f.GetAsText().Trim();
            var parts = s.Split(':');
            if (parts.Length == 2 && int.TryParse(parts[1], out var p))
            {
                host = parts[0];
                port = p;
                return true;
            }
        }
        return false;
    }

    
    private void SaveLastServer(string host, int port)
    {
        var path = "user://last_server.txt";
        using var f = FileAccess.Open(path, FileAccess.ModeFlags.Write);
        f.StoreString($"{host}:{port}");
    }
    */
}
