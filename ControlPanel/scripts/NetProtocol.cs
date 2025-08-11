// NetProtocol.cs
using System;
using System.Text.Json;
using System.Text.Json.Serialization;

public enum Param { Speed, Range, Distance, Scale }
public enum CmdType { Set, StimType, World, ToggleSound, ToggleAR, Pause, Reset, Recenter, Emergency, Hello }

// Base type + records
public abstract record Command([property:JsonPropertyName("t")] CmdType T)
{
    public string ToJson() => JsonSerializer.Serialize((object)this, JsonOpts);

    public static Command FromJson(string json)
    {
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        // pull "t" (string enum value or number)
        if (!root.TryGetProperty("t", out var tEl))
            throw new NotSupportedException("Command is missing 't'");

        string tStr = tEl.ValueKind switch
        {
            JsonValueKind.String => tEl.GetString(),
            JsonValueKind.Number => tEl.GetInt32().ToString(),
            _ => tEl.ToString()
        };

        // normalize
        tStr = (tStr ?? "").Trim().ToLowerInvariant();

        switch (tStr)
        {
            case "set":
                // k can be "speed" | "range" | "distance" | "scale"
                var kStr = root.TryGetProperty("k", out var kEl) ? kEl.GetString() : null;
                if (string.IsNullOrWhiteSpace(kStr))
                    throw new NotSupportedException("Set command missing 'k'");
                if (!Enum.TryParse<Param>(kStr, true, out var k))
                    throw new NotSupportedException($"Unknown param '{kStr}'");
                var v = root.TryGetProperty("v", out var vEl) ? (float)vEl.GetDouble() : 0f;
                return new SetCmd(k, v);

            case "stimtype":
                var sti = root.TryGetProperty("i", out var stiEl) ? stiEl.GetInt32() : 0;
                return new StimTypeCmd(sti);

            case "world":
                var wi = root.TryGetProperty("i", out var wiEl) ? wiEl.GetInt32() : 0;
                return new WorldCmd(wi);

            case "togglesound":
                var onSound = root.TryGetProperty("on", out var onElSnd) && onElSnd.GetBoolean();
                return new ToggleSoundCmd(onSound);

            case "togglear":
                var onAR = root.TryGetProperty("on", out var onElAR) && onElAR.GetBoolean();
                return new ToggleARCmd(onAR);

            case "pause":
                var onPause = root.TryGetProperty("on", out var onEl) && onEl.GetBoolean();
                return new PauseCmd(onPause);

            case "reset":
                return new ResetCmd();

            case "recenter":
                return new RecenterCmd();

            case "emergency":
                return new EmergencyCmd();

            case "hello":
                var ver = root.TryGetProperty("ver", out var verEl) ? (verEl.ValueKind == JsonValueKind.String ? verEl.GetString() : verEl.ToString()) : "";
                return new HelloCmd(ver ?? "");

            default:
                throw new NotSupportedException($"Unknown command type '{tStr}'");
        }
    }

    public static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) },
        WriteIndented = false
    };
}

public sealed record SetCmd(Param K, float V) : Command(CmdType.Set);
public sealed record StimTypeCmd(int I) : Command(CmdType.StimType);
public sealed record WorldCmd(int I) : Command(CmdType.World);
public sealed record ToggleSoundCmd(bool On) : Command(CmdType.ToggleSound);
public sealed record ToggleARCmd(bool On) : Command(CmdType.ToggleAR);
public sealed record PauseCmd(bool On) : Command(CmdType.Pause);
public sealed record ResetCmd() : Command(CmdType.Reset);
public sealed record RecenterCmd() : Command(CmdType.Recenter);
public sealed record EmergencyCmd() : Command(CmdType.Emergency);
public sealed record HelloCmd(string Ver) : Command(CmdType.Hello);

public static class Cmd
{
    public static SetCmd Set(Param p, float v) => new(p, v);
    public static StimTypeCmd StimType(int i) => new(i);
    public static WorldCmd World(int i) => new(i);
    public static ToggleSoundCmd Sound(bool on) => new(on);
    public static ToggleARCmd AR(bool on) => new(on);
    public static PauseCmd Pause(bool on) => new(on);
    public static ResetCmd Reset() => new();
    public static RecenterCmd Recenter() => new();
    public static EmergencyCmd Emergency() => new();
    public static HelloCmd Hello(string ver) => new(ver);
}
