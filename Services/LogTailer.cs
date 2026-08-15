using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace EclipticaOverlay.Services;

/// Tails a single log file: on first poll reads it from the start (catch-up), then only
/// emits newly appended lines. Tolerant of the file being locked or briefly unavailable
/// while VRChat is writing to it.
public sealed class LogTailer
{
    private readonly string _filePath;
    private long _position;
    private string _leftover = string.Empty;

    public LogTailer(string filePath)
    {
        _filePath = filePath;
    }

    public event Action<string>? LineRead;

    public async Task RunAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                await PollOnceAsync();
            }
            catch (IOException)
            {
                // Log file is momentarily locked (VRChat writing) or being rotated; retry next tick.
            }
            catch (UnauthorizedAccessException)
            {
            }

            try
            {
                await Task.Delay(TimeSpan.FromMilliseconds(500), ct);
            }
            catch (TaskCanceledException)
            {
                break;
            }
        }
    }

    private async Task PollOnceAsync()
    {
        using var stream = new FileStream(
            _filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);

        if (stream.Length < _position)
        {
            // File shrank/was replaced out from under us; start over.
            _position = 0;
            _leftover = string.Empty;
        }

        if (stream.Length == _position)
            return;

        stream.Seek(_position, SeekOrigin.Begin);
        using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: false);
        var chunk = await reader.ReadToEndAsync();
        _position = stream.Length;

        var combined = _leftover + chunk;
        var lines = combined.Split('\n');
        _leftover = lines[^1];

        for (var i = 0; i < lines.Length - 1; i++)
        {
            var line = lines[i].TrimEnd('\r');
            if (line.Length > 0)
                LineRead?.Invoke(line);
        }
    }
}
