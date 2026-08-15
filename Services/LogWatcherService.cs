using System.Threading;
using System.Threading.Tasks;
using EclipticaOverlay.Models;

namespace EclipticaOverlay.Services;

/// Watches a VRChat log directory for the current/newest output_log_*.txt, tails it, feeds
/// every ECLIPTICA line through the parser, and exposes the resulting MatchState.
/// If VRChat rolls over to a new log file (new session), the tracked state is reset and the
/// new file is tailed from its start.
public sealed class LogWatcherService : IDisposable
{
    private static readonly TimeSpan RescanInterval = TimeSpan.FromSeconds(3);

    private readonly string _logDirectory;
    private readonly EclipticaLogParser _parser = new();
    private readonly object _stateLock = new();
    private MatchState _state = new();

    private CancellationTokenSource? _tailerCts;
    private string? _currentFilePath;

    public LogWatcherService(string logDirectory)
    {
        _logDirectory = logDirectory;
    }

    public async Task RunAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            var latest = LogFileLocator.FindLatestLogFile(_logDirectory);
            if (latest != null && latest.FullName != _currentFilePath)
            {
                SwitchToFile(latest.FullName);
            }

            lock (_stateLock)
            {
                _state.LogConnected = _currentFilePath != null;
            }

            try
            {
                await Task.Delay(RescanInterval, ct);
            }
            catch (TaskCanceledException)
            {
                break;
            }
        }

        _tailerCts?.Cancel();
    }

    private void SwitchToFile(string path)
    {
        _tailerCts?.Cancel();
        _tailerCts = new CancellationTokenSource();
        _currentFilePath = path;

        lock (_stateLock)
        {
            _state = new MatchState { LogConnected = true };
        }

        var tailer = new LogTailer(path);
        tailer.LineRead += OnLineRead;
        _ = tailer.RunAsync(_tailerCts.Token);
    }

    private void OnLineRead(string line)
    {
        lock (_stateLock)
        {
            _parser.TryApply(line, _state);
        }
    }

    /// Returns a snapshot copy of the current match state (safe to bind to the UI thread).
    public MatchState GetSnapshot()
    {
        lock (_stateLock)
        {
            return _state.Clone();
        }
    }

    public void Dispose()
    {
        _tailerCts?.Cancel();
        _tailerCts?.Dispose();
    }
}
