using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace EclipticaOverlay.Services;

/// Sends chatbox text to VRChat via OSC (localhost UDP 9000, "/chatbox/input").
/// Hand-encodes the OSC 1.0 packet directly — the payload shape needed here (one address,
/// one string argument, two bool argument tags) doesn't justify a full OSC library dependency.
public sealed class OscChatboxSender : IDisposable
{
    private const string Address = "/chatbox/input";
    private readonly UdpClient _udp = new();
    private readonly IPEndPoint _endpoint = new(IPAddress.Loopback, 9000);

    /// notify: true also plays VRChat's chatbox notification sound/icon for this message.
    /// Sends are fire-and-forget UDP; a VRChat client that isn't running or listening simply
    /// never receives the packet, which is fine — there's nothing to react to on our end.
    public void Send(string message, bool notify = false)
    {
        using var ms = new MemoryStream();
        WriteOscString(ms, Address);
        WriteOscString(ms, notify ? ",sTT" : ",sTF");
        WriteOscString(ms, message);

        var packet = ms.ToArray();
        try
        {
            _udp.Send(packet, packet.Length, _endpoint);
        }
        catch (SocketException)
        {
            // No listener / network hiccup — safe to drop, next tick will just try again.
        }
    }

    private static void WriteOscString(Stream stream, string value)
    {
        var bytes = Encoding.UTF8.GetBytes(value);
        stream.Write(bytes, 0, bytes.Length);

        // OSC strings are null-terminated and padded so the total lands on a 4-byte boundary.
        var padded = ((bytes.Length + 1 + 3) / 4) * 4;
        for (var i = bytes.Length; i < padded; i++)
            stream.WriteByte(0);
    }

    public void Dispose() => _udp.Dispose();
}
