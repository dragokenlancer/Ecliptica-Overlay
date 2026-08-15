using EclipticaOverlay.Models;

namespace EclipticaOverlay.Services;

/// Owns chatbox output: builds text from MatchState (via the user's templates) and sends it
/// over OSC, throttled so we don't flood VRChat's chatbox on every 250ms UI refresh tick.
public sealed class ChatboxController : IDisposable
{
    // Floor for the user-configurable interval — protects against a 0/near-0 setting getting
    // the chatbox rate-limited or muted by VRChat.
    private const double MinIntervalFloorSeconds = 0.5;

    private readonly OscChatboxSender _sender = new();
    private string? _lastSentText;
    private DateTime _lastSentAt = DateTime.MinValue;

    public bool Enabled { get; set; }

    public void Update(MatchState state, AppSettings settings)
    {
        if (!Enabled)
            return;

        var text = ChatboxMessageBuilder.Build(state, settings);
        if (text == null || text == _lastSentText)
            return;

        var now = DateTime.Now;
        var minInterval = TimeSpan.FromSeconds(Math.Max(MinIntervalFloorSeconds, settings.ChatboxIntervalSeconds));
        if (now - _lastSentAt < minInterval)
            return;

        _sender.Send(text, settings.ChatboxNotifySound);
        _lastSentText = text;
        _lastSentAt = now;
    }

    public void Dispose() => _sender.Dispose();
}
