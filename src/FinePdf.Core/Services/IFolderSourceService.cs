namespace FinePdf.Core.Services;

/// <summary>
/// Watches a single folder and exposes the <c>*.pdf</c> files in it (non-recursive),
/// raising <see cref="FilesChanged"/> when that set changes on disk.
/// </summary>
public interface IFolderSourceService : IDisposable
{
    /// <summary>Absolute path of the folder currently watched, or <c>null</c> before the first <see cref="SetFolder"/>.</summary>
    string? FolderPath { get; }

    /// <summary>
    /// Absolute paths of the matching files in <see cref="FolderPath"/>, most recently
    /// modified first (ties broken by file name).
    /// </summary>
    IReadOnlyList<string> Files { get; }

    event EventHandler? FilesChanged;

    /// <exception cref="DirectoryNotFoundException"><paramref name="folderPath"/> does not exist.</exception>
    void SetFolder(string folderPath);

    /// <summary>Re-scans the folder now. Normally triggered automatically by the file-system watcher.</summary>
    void Refresh();
}
