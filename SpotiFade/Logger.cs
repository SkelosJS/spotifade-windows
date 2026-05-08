namespace SpotiFade;

/// <summary>
/// Rolling text log at %LOCALAPPDATA%\SpotiFade\log.txt — useful when ads slip
/// past detection, since the user can open the file and see what metadata
/// Spotify actually reported.
/// </summary>
internal static class Logger
{
    private static readonly object _lock = new();
    private static readonly string _path;

    static Logger()
    {
        var dir = System.IO.Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "SpotiFade");
        Directory.CreateDirectory(dir);
        _path = System.IO.Path.Combine(dir, "log.txt");
    }

    public static string FilePath => _path;

    /// <summary>Verbose entries — stripped from release builds.</summary>
    [System.Diagnostics.Conditional("DEBUG")]
    public static void Debug(string line) => Write(line);

    public static void Write(string line)
    {
        var stamped = $"{DateTime.Now:HH:mm:ss} {line}";
        lock (_lock)
        {
            try
            {
                // Cap at ~512 KB so it doesn't grow forever.
                if (File.Exists(_path) && new FileInfo(_path).Length > 512 * 1024)
                {
                    File.Delete(_path);
                }
                File.AppendAllText(_path, stamped + Environment.NewLine);
            }
            catch
            {
                // Logging is best-effort.
            }
        }
    }
}
