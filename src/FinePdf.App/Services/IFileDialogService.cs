namespace FinePdf.App.Services;

/// <summary>Thin wrapper over Avalonia's storage pickers so the view models stay testable.</summary>
public interface IFileDialogService
{
    Task<string?> PickFolderAsync(string? startInPath = null);

    Task<string?> PickSavePdfAsync(string? suggestedFileName = null, string? startInPath = null);
}
