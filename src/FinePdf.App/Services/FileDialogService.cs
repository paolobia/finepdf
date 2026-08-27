using Avalonia.Controls;
using Avalonia.Platform.Storage;

namespace FinePdf.App.Services;

/// <inheritdoc />
public sealed class FileDialogService(Func<TopLevel?> topLevelAccessor) : IFileDialogService
{
    private readonly Func<TopLevel?> _topLevelAccessor = topLevelAccessor;

    public async Task<string?> PickFolderAsync(string? startInPath = null)
    {
        TopLevel? topLevel = _topLevelAccessor();
        if (topLevel is null)
        {
            return null;
        }

        IReadOnlyList<IStorageFolder> folders = await topLevel.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = "Scegli la cartella dei PDF",
            AllowMultiple = false,
            SuggestedStartLocation = await TryGetFolderAsync(topLevel, startInPath),
        });

        return folders.Count > 0 ? folders[0].TryGetLocalPath() : null;
    }

    public async Task<string?> PickSavePdfAsync(string? suggestedFileName = null, string? startInPath = null)
    {
        TopLevel? topLevel = _topLevelAccessor();
        if (topLevel is null)
        {
            return null;
        }

        IStorageFile? file = await topLevel.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Salva PDF",
            SuggestedFileName = suggestedFileName ?? "documento.pdf",
            DefaultExtension = "pdf",
            ShowOverwritePrompt = true,
            SuggestedStartLocation = await TryGetFolderAsync(topLevel, startInPath),
            FileTypeChoices = [new FilePickerFileType("PDF") { Patterns = ["*.pdf"] }],
        });

        return file?.TryGetLocalPath();
    }

    private static async Task<IStorageFolder?> TryGetFolderAsync(TopLevel topLevel, string? path)
    {
        if (string.IsNullOrWhiteSpace(path) || !Directory.Exists(path))
        {
            return null;
        }

        try
        {
            return await topLevel.StorageProvider.TryGetFolderFromPathAsync(path);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return null;
        }
    }
}
