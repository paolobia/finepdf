using System.ComponentModel;
using System.Globalization;
using System.Runtime.InteropServices;

namespace FinePdf.Core.Services;

/// <inheritdoc />
public sealed class RecycleBinService : IRecycleBinService
{
    private readonly IProcessRunner _runner;
    private readonly string? _linuxTrashRootOverride;

    /// <param name="linuxTrashRootOverride">Test hook: the <c>Trash</c> directory to use on Linux.</param>
    public RecycleBinService(IProcessRunner runner, string? linuxTrashRootOverride = null)
    {
        ArgumentNullException.ThrowIfNull(runner);
        _runner = runner;
        _linuxTrashRootOverride = linuxTrashRootOverride;
    }

    public bool IsSupported => OperatingSystem.IsWindows() || OperatingSystem.IsLinux();

    public bool TryMoveToTrash(string path)
    {
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
        {
            return false;
        }

        try
        {
            if (OperatingSystem.IsWindows())
            {
                return TrashWindows(Path.GetFullPath(path));
            }

            if (OperatingSystem.IsLinux())
            {
                return TrashGio(path) || TrashFreedesktop(Path.GetFullPath(path));
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or Win32Exception)
        {
            return false;
        }

        return false;
    }

    // --- Linux: gio trash -----------------------------------------------------

    private bool TrashGio(string path)
    {
        try
        {
            return _runner.Run("gio", ["trash", "--", path]).Succeeded;
        }
        catch (Win32Exception)
        {
            return false; // gio not installed
        }
    }

    // --- Linux: freedesktop.org trash spec (home volume) ---------------------

    private bool TrashFreedesktop(string sourcePath)
    {
        string trashRoot = _linuxTrashRootOverride ?? Path.Combine(
            Environment.GetEnvironmentVariable("XDG_DATA_HOME") is { Length: > 0 } xdg
                ? xdg
                : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".local", "share"),
            "Trash");

        string filesDir = Path.Combine(trashRoot, "files");
        string infoDir = Path.Combine(trashRoot, "info");
        Directory.CreateDirectory(filesDir);
        Directory.CreateDirectory(infoDir);

        string baseName = Path.GetFileName(sourcePath);
        string name = baseName;
        for (int n = 2; File.Exists(Path.Combine(filesDir, name)) || File.Exists(Path.Combine(infoDir, name + ".trashinfo")); n++)
        {
            name = $"{Path.GetFileNameWithoutExtension(baseName)}_{n}{Path.GetExtension(baseName)}";
        }

        string infoPath = Path.Combine(infoDir, name + ".trashinfo");
        string encodedPath = string.Join('/', sourcePath.Split('/').Select(Uri.EscapeDataString));
        File.WriteAllText(
            infoPath,
            "[Trash Info]\n" +
            $"Path={encodedPath}\n" +
            $"DeletionDate={DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture)}\n");

        try
        {
            MoveOrCopyDelete(sourcePath, Path.Combine(filesDir, name));
            return true;
        }
        catch
        {
            TryDelete(infoPath);
            throw;
        }
    }

    private static void MoveOrCopyDelete(string source, string destination)
    {
        try
        {
            File.Move(source, destination);
        }
        catch (IOException)
        {
            // cross-device move
            File.Copy(source, destination, overwrite: true);
            File.Delete(source);
        }
    }

    private static void TryDelete(string path)
    {
        try
        {
            File.Delete(path);
        }
        catch (IOException)
        {
            // ignored
        }
    }

    // --- Windows: SHFileOperation with FOF_ALLOWUNDO ------------------------

    private static bool TrashWindows(string path)
    {
        var op = new ShFileOpStruct
        {
            wFunc = FoDelete,
            pFrom = path + "\0\0",
            fFlags = FofAllowUndo | FofNoConfirmation | FofSilent | FofNoErrorUi,
        };

        return SHFileOperation(ref op) == 0 && op.fAnyOperationsAborted == 0;
    }

    private const uint FoDelete = 0x0003;
    private const ushort FofSilent = 0x0004;
    private const ushort FofNoConfirmation = 0x0010;
    private const ushort FofAllowUndo = 0x0040;
    private const ushort FofNoErrorUi = 0x0400;

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct ShFileOpStruct
    {
        public IntPtr hwnd;
        public uint wFunc;
        [MarshalAs(UnmanagedType.LPWStr)] public string pFrom;
        [MarshalAs(UnmanagedType.LPWStr)] public string? pTo;
        public ushort fFlags;
        public int fAnyOperationsAborted;
        public IntPtr hNameMappings;
        [MarshalAs(UnmanagedType.LPWStr)] public string? lpszProgressTitle;
    }

    [DllImport("shell32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern int SHFileOperation(ref ShFileOpStruct lpFileOp);
}
