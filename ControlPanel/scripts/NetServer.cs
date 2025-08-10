// NetServer.cs
using Godot;
using Godot.Collections;
using System.Linq;
using System.Text;

public partial class NetServer : Node
{
    [Export] public ushort Port = 9080;

    private TcpServer _server;
    private Array<StreamPeerTcp> _clients = new Array<StreamPeerTcp>();

    public override void _Ready()
    {
         _server = new TcpServer();

        var ok = _server.Listen(Port) == Error.Ok;
    }

    public override void _Process(double delta)
    {
        if (_server.IsListening() && _server.IsConnectionAvailable())
        {
            var peer = _server.TakeConnection();
            _clients.Add(peer);
            GD.Print($"[NetServer] Client connected ({peer.GetConnectedHost()}:{peer.GetConnectedPort()})");

            // Send one line immediately (newline-delimited JSON)
            var hello = Cmd.Hello("desk-ignite").ToJson() + "\n"; // includes "ver"
            var bytes = System.Text.Encoding.UTF8.GetBytes(hello);
            var sendErr = peer.PutData(bytes);
            GD.Print($"[NetServer] Sent HELLO to new client (err={sendErr})");
        }

        for (int i = _clients.Count - 1; i >= 0; i--)
        {
            var c = _clients[i];
            c.Poll();
            if (c.GetStatus() != StreamPeerTcp.Status.Connected)
            {
                _clients.RemoveAt(i);
                GD.Print("[NetServer] Client removed");
            }
        }
    }

    public void Broadcast(string json)
    {
        int count = _clients.Count(c => c.GetStatus() == StreamPeerTcp.Status.Connected);
        GD.Print($"[NetServer] Broadcasting to {count} client(s): {json}");
        if (count == 0) return;

        var bytes = Encoding.UTF8.GetBytes(json + "\n");

        for (int i = _clients.Count - 1; i >= 0; i--)
        {
            var c = _clients[i];
            if (c.GetStatus() != StreamPeerTcp.Status.Connected)
            {
                _clients.RemoveAt(i);
                continue;
            }

            var err = c.PutData(bytes);
            if (err != Error.Ok)
            {
                GD.PrintErr($"[NetServer] Send failed: {err} → dropping client");
                _clients.RemoveAt(i);
            }
        }
    }

    public void Broadcast(Command cmd) => Broadcast(cmd.ToJson());

    public bool HasClients() => _clients.Any(c => c.GetStatus() == StreamPeerTcp.Status.Connected);
}
