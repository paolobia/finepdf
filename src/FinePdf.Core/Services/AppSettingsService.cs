using System.Text.Json;
using System.Text.Json.Serialization;

namespace FinePdf.Core.Services;

/// <inheritdoc />
public sealed class AppSettingsService : IAppSettingsService
{
    private static readonly JsonSerializerOptions SerializerOptions = new() { WriteIndented = true };

    private readonly string _directory;
    private readonly object _gate = new();

    /// <param name="overrideDirectory">
    /// Directory that holds <c>settings.json</c>. <c>null</c> (production) resolves to
    /// <c>%APPDATA%\FinePdf</c> / <c>$XDG_CONFIG_HOME/FinePdf</c> / <c>~/.config/FinePdf</c>.
    /// Tests pass a temp directory.
    /// </param>
    public AppSettingsService(string? overrideDirectory = null)
    {
        _directory = overrideDirectory ?? Path.Combine(
            Environment.GetFolderPath(
                Environment.SpecialFolder.ApplicationData,
                Environment.SpecialFolderOption.DoNotVerify),
            "FinePdf");
    }

    public string SettingsFilePath => Path.Combine(_directory, "settings.json");

    public string? LastFolder => ReadRooted(f => f.LastFolder);

    public string? PrinterOutputFolder => ReadRooted(f => f.PrinterOutputFolder);

    public bool TrashPrinterPdfs => Read().TrashPrinterPdfs ?? true;

    public void SetLastFolder(string absolutePath) => Update(f => f with { LastFolder = absolutePath });

    public void SetPrinterOutputFolder(string absolutePath) =>
        Update(f => f with { PrinterOutputFolder = absolutePath });

    public void SetTrashPrinterPdfs(bool value) => Update(f => f with { TrashPrinterPdfs = value });

    private string? ReadRooted(Func<SettingsFile, string?> pick)
    {
        string? value = pick(Read());
        return !string.IsNullOrWhiteSpace(value) && Path.IsPathRooted(value) ? value : null;
    }

    private SettingsFile Read()
    {
        lock (_gate)
        {
            try
            {
                string json = File.ReadAllText(SettingsFilePath);
                return JsonSerializer.Deserialize<SettingsFile>(json, SerializerOptions) ?? new SettingsFile();
            }
            catch (Exception ex) when (ex is FileNotFoundException or DirectoryNotFoundException
                or JsonException or IOException or UnauthorizedAccessException)
            {
                return new SettingsFile();
            }
        }
    }

    private void Update(Func<SettingsFile, SettingsFile> change)
    {
        lock (_gate)
        {
            SettingsFile next;
            try
            {
                string json = File.ReadAllText(SettingsFilePath);
                next = change(JsonSerializer.Deserialize<SettingsFile>(json, SerializerOptions) ?? new SettingsFile());
            }
            catch (Exception ex) when (ex is FileNotFoundException or DirectoryNotFoundException
                or JsonException or IOException or UnauthorizedAccessException)
            {
                next = change(new SettingsFile());
            }

            try
            {
                Directory.CreateDirectory(_directory);
                string tempPath = Path.Combine(_directory, $"settings.json.tmp-{Guid.NewGuid():N}");
                File.WriteAllText(tempPath, JsonSerializer.Serialize(next, SerializerOptions));
                File.Move(tempPath, SettingsFilePath, overwrite: true);
                TrySetOwnerOnlyMode(SettingsFilePath);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                // Persistence is best-effort; never break the UI over it.
            }
        }
    }

    private static void TrySetOwnerOnlyMode(string path)
    {
        if (!OperatingSystem.IsWindows())
        {
            try
            {
                File.SetUnixFileMode(path, UnixFileMode.UserRead | UnixFileMode.UserWrite);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or PlatformNotSupportedException)
            {
                // ignored
            }
        }
    }

    private sealed record SettingsFile
    {
        [JsonPropertyName("lastFolder")]
        public string? LastFolder { get; init; }

        [JsonPropertyName("printerOutputFolder")]
        public string? PrinterOutputFolder { get; init; }

        [JsonPropertyName("trashPrinterPdfs")]
        public bool? TrashPrinterPdfs { get; init; }
    }
}
