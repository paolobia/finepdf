using Avalonia.Controls;
using FinePdf.App.ViewModels;
using FinePdf.App.Views;
using FinePdf.Core.Services;

namespace FinePdf.App.Services;

/// <inheritdoc />
public sealed class PrinterSetupDialogService(Func<Window?> ownerAccessor, IFileDialogService fileDialogs)
    : IPrinterSetupDialogService
{
    private readonly Func<Window?> _ownerAccessor = ownerAccessor;
    private readonly IFileDialogService _fileDialogs = fileDialogs;

    public async Task<PrinterSetupResult?> RequestAsync(PrinterInstallState state, string suggestedFolder)
    {
        Window? owner = _ownerAccessor();
        if (owner is null)
        {
            return null;
        }

        var viewModel = new PrinterSetupDialogViewModel(state, suggestedFolder, _fileDialogs);
        return await PrinterSetupDialog.ShowAsync(owner, viewModel);
    }
}
