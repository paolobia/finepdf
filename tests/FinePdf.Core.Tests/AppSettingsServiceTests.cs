using FinePdf.Core.Services;
using FinePdf.Core.Tests.TestSupport;

namespace FinePdf.Core.Tests;

public class AppSettingsServiceTests
{
    [Fact]
    public void SetFolders_ThenFreshInstance_RoundTripsBothKeys()
    {
        using var dir = new TempDir();
        new AppSettingsService(dir.Path).SetLastFolder(dir.Path);
        new AppSettingsService(dir.Path).SetPrinterOutputFolder(dir.Path);

        var reader = new AppSettingsService(dir.Path);
        Assert.Equal(dir.Path, reader.LastFolder);
        Assert.Equal(dir.Path, reader.PrinterOutputFolder);
    }

    [Fact]
    public void LastFolder_WhenFileAbsent_ReturnsNull()
    {
        using var dir = new TempDir();
        var service = new AppSettingsService(dir.Path);

        Assert.Null(service.LastFolder);
        Assert.Null(service.PrinterOutputFolder);
    }

    [Fact]
    public void Getters_WhenJsonCorrupt_ReturnNull()
    {
        using var dir = new TempDir();
        File.WriteAllText(Path.Combine(dir.Path, "settings.json"), "{ not json");

        var service = new AppSettingsService(dir.Path);
        Assert.Null(service.LastFolder);
    }

    [Fact]
    public void Getter_WhenValueNotRooted_ReturnsNull()
    {
        using var dir = new TempDir();
        File.WriteAllText(Path.Combine(dir.Path, "settings.json"), "{\"lastFolder\":\"relative/path\"}");

        Assert.Null(new AppSettingsService(dir.Path).LastFolder);
    }

    [Fact]
    public void SetOneKey_KeepsTheOther()
    {
        using var dir = new TempDir();
        var service = new AppSettingsService(dir.Path);
        service.SetLastFolder(dir.Path);
        service.SetPrinterOutputFolder(dir.Path);

        string other = Path.GetTempPath();
        service.SetLastFolder(other);

        Assert.Equal(other, service.LastFolder);
        Assert.Equal(dir.Path, service.PrinterOutputFolder);
    }

    [Fact]
    public void SetLastFolder_CreatesDirectory()
    {
        using var dir = new TempDir();
        string nested = Path.Combine(dir.Path, "a", "b");
        var service = new AppSettingsService(nested);

        service.SetLastFolder(dir.Path);

        Assert.True(File.Exists(Path.Combine(nested, "settings.json")));
    }

    [Fact]
    public void TrashPrinterPdfs_DefaultsToTrue_AndRoundTrips()
    {
        using var dir = new TempDir();
        Assert.True(new AppSettingsService(dir.Path).TrashPrinterPdfs);

        new AppSettingsService(dir.Path).SetTrashPrinterPdfs(false);
        Assert.False(new AppSettingsService(dir.Path).TrashPrinterPdfs);

        new AppSettingsService(dir.Path).SetTrashPrinterPdfs(true);
        Assert.True(new AppSettingsService(dir.Path).TrashPrinterPdfs);
    }

    [Fact]
    public void SetTrashPrinterPdfs_KeepsFolders()
    {
        using var dir = new TempDir();
        var service = new AppSettingsService(dir.Path);
        service.SetLastFolder(dir.Path);
        service.SetTrashPrinterPdfs(false);

        Assert.Equal(dir.Path, service.LastFolder);
        Assert.False(service.TrashPrinterPdfs);
    }

    [Fact]
    public void SettingsFile_UsesTheContractKeyNames()
    {
        using var dir = new TempDir();
        var service = new AppSettingsService(dir.Path);
        service.SetLastFolder(dir.Path);
        service.SetPrinterOutputFolder(dir.Path);

        string json = File.ReadAllText(service.SettingsFilePath);
        Assert.Contains("\"lastFolder\"", json, StringComparison.Ordinal);
        Assert.Contains("\"printerOutputFolder\"", json, StringComparison.Ordinal);
    }
}
