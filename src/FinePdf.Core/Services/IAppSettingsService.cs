namespace FinePdf.Core.Services;

/// <summary>
/// Persists a tiny amount of user state to <c>~/.config/FinePdf/settings.json</c>
/// (<c>%APPDATA%\FinePdf\settings.json</c> on Windows). Best-effort: every operation swallows
/// I/O errors rather than surfacing them to the UI.
/// <para>
/// The JSON keys (<c>lastFolder</c>, <c>printerOutputFolder</c>) are a contract shared with the
/// Linux CUPS backend, which reads the same file — do not rename them.
/// </para>
/// </summary>
public interface IAppSettingsService
{
    /// <summary>Last working folder browsed in the "Cartella di lavoro" tab, or <c>null</c>.</summary>
    string? LastFolder { get; }

    /// <summary>Folder the "FinePdf" virtual printer drops PDFs into, or <c>null</c>.</summary>
    string? PrinterOutputFolder { get; }

    /// <summary>Move a printer-folder PDF to the recycle bin after it is added to the composition. Default <c>true</c>.</summary>
    bool TrashPrinterPdfs { get; }

    void SetLastFolder(string absolutePath);

    void SetPrinterOutputFolder(string absolutePath);

    void SetTrashPrinterPdfs(bool value);

    /// <summary>Absolute path of the backing JSON file (may not exist yet).</summary>
    string SettingsFilePath { get; }
}
