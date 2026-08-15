using System.IO;
using System.Threading;
using System.Windows;
using EclipticaOverlay.Services;

namespace EclipticaOverlay;

public partial class App : Application
{
    private readonly CancellationTokenSource _lifetimeCts = new();
    private LogWatcherService? _watcher;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Optional: pass a folder as the first argument to watch it instead of the real
        // VRChat log folder (e.g. for testing against sample logs).
        var logDirectory = e.Args.Length > 0 && Directory.Exists(e.Args[0])
            ? e.Args[0]
            : LogFileLocator.GetDefaultLogDirectory();

        _watcher = new LogWatcherService(logDirectory);
        _ = _watcher.RunAsync(_lifetimeCts.Token);

        var window = new MainWindow(_watcher);
        window.Show();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _lifetimeCts.Cancel();
        _watcher?.Dispose();
        base.OnExit(e);
    }
}
