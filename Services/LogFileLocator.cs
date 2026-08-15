using System.IO;
using System.Linq;

namespace EclipticaOverlay.Services;

/// Finds the VRChat output log directory and the most recently written log file in it.
public static class LogFileLocator
{
    /// Default VRChat log directory: %USERPROFILE%\AppData\LocalLow\VRChat\VRChat
    public static string GetDefaultLogDirectory()
    {
        var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        return Path.Combine(userProfile, "AppData", "LocalLow", "VRChat", "VRChat");
    }

    /// Returns the most recently written output_log_*.txt in `directory`, or null if none exist.
    public static FileInfo? FindLatestLogFile(string directory)
    {
        if (!Directory.Exists(directory))
            return null;

        return new DirectoryInfo(directory)
            .EnumerateFiles("output_log_*.txt")
            .OrderByDescending(f => f.LastWriteTimeUtc)
            .FirstOrDefault();
    }
}
