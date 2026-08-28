using System.Timers;
using Timer = System.Timers.Timer;

namespace FinePdf.Core.Services;

/// <inheritdoc />
public sealed class FolderSourceService : IFolderSourceService
{
    private readonly bool _enableWatcher;
    private readonly IReadOnlySet<string> _extensions;
    private readonly object _gate = new();
    private readonly Timer _debounce;

    private FileSystemWatcher? _watcher;
    private string? _folderPath;
    private IReadOnlyList<string> _files = [];

    /// <param name="enableWatcher">
    /// When <c>false</c>, no <see cref="FileSystemWatcher"/> is created and the caller must
    /// invoke <see cref="Refresh"/> explicitly. Used by tests to stay deterministic.
    /// </param>
    /// <param name="extensions">
    /// File extensions to list (with leading dot, case-insensitive). Defaults to <c>.pdf</c> only.
    /// </param>
    public FolderSourceService(bool enableWatcher = true, IReadOnlyCollection<string>? extensions = null)
    {
        _enableWatcher = enableWatcher;
        _extensions = extensions is { Count: > 0 }
            ? new HashSet<string>(extensions, StringComparer.OrdinalIgnoreCase)
            : new HashSet<string>(StringComparer.OrdinalIgnoreCase) { ".pdf" };
        _debounce = new Timer(250) { AutoReset = false };
        _debounce.Elapsed += OnDebounceElapsed;
    }

    public event EventHandler? FilesChanged;

    public string? FolderPath
    {
        get
        {
            lock (_gate)
            {
                return _folderPath;
            }
        }
    }

    public IReadOnlyList<string> Files
    {
        get
        {
            lock (_gate)
            {
                return _files;
            }
        }
    }

    public void SetFolder(string folderPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(folderPath);
        if (!Directory.Exists(folderPath))
        {
            throw new DirectoryNotFoundException($"Folder not found: {folderPath}");
        }

        string fullPath = Path.GetFullPath(folderPath);
        lock (_gate)
        {
            _folderPath = fullPath;
        }

        SetupWatcher(fullPath);
        Refresh();
    }

    public void Refresh()
    {
        string? folder = FolderPath;
        string[] scanned = folder is null
            ? []
            : [.. Directory.EnumerateFiles(folder)
                .Where(f => _extensions.Contains(Path.GetExtension(f)))
                .OrderByDescending(LastWriteUtcOrMin)
                .ThenBy(static f => Path.GetFileName(f), StringComparer.OrdinalIgnoreCase)];

        bool changed;
        lock (_gate)
        {
            changed = !_files.SequenceEqual(scanned, StringComparer.Ordinal);
            if (changed)
            {
                _files = scanned;
            }
        }

        if (changed)
        {
            FilesChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    private static DateTime LastWriteUtcOrMin(string path)
    {
        try
        {
            return File.GetLastWriteTimeUtc(path);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return DateTime.MinValue;
        }
    }

    public void Dispose()
    {
        _watcher?.Dispose();
        _debounce.Elapsed -= OnDebounceElapsed;
        _debounce.Dispose();
        GC.SuppressFinalize(this);
    }

    private void OnDebounceElapsed(object? sender, ElapsedEventArgs e) => Refresh();

    private void SetupWatcher(string folder)
    {
        _watcher?.Dispose();
        _watcher = null;

        if (!_enableWatcher)
        {
            return;
        }

        var watcher = new FileSystemWatcher(folder)
        {
            NotifyFilter = NotifyFilters.FileName | NotifyFilters.DirectoryName,
            IncludeSubdirectories = false,
        };
        watcher.Created += OnWatcherChanged;
        watcher.Deleted += OnWatcherChanged;
        watcher.Renamed += OnWatcherRenamed;
        watcher.EnableRaisingEvents = true;
        _watcher = watcher;
    }

    private void OnWatcherChanged(object sender, FileSystemEventArgs e) => RestartDebounce();

    private void OnWatcherRenamed(object sender, RenamedEventArgs e) => RestartDebounce();

    private void RestartDebounce()
    {
        _debounce.Stop();
        _debounce.Start();
    }
}
