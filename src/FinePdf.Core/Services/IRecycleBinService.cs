namespace FinePdf.Core.Services;

/// <summary>
/// Moves a file to the OS recycle bin / trash (recoverable), never a permanent delete.
/// Windows: <c>SHFileOperation</c> with undo. Linux: <c>gio trash</c>, falling back to the
/// freedesktop.org trash spec under <c>$XDG_DATA_HOME/Trash</c>.
/// </summary>
public interface IRecycleBinService
{
    bool IsSupported { get; }

    /// <returns><c>true</c> if the file was moved to the trash; <c>false</c> if unsupported or it failed.</returns>
    bool TryMoveToTrash(string path);
}
