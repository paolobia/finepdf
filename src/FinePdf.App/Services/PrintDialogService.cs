using Avalonia.Controls;
using FinePdf.App.ViewModels;
using FinePdf.App.Views;
using FinePdf.Core.Models;
using FinePdf.Core.Services;

namespace FinePdf.App.Services;

/// <inheritdoc />
public sealed class PrintDialogService(Func<Window?> ownerAccessor) : IPrintDialogService
{
    private readonly Func<Window?> _ownerAccessor = ownerAccessor;

    public async Task<PrintOptions?> RequestAsync(IReadOnlyList<PrinterInfo> printers)
    {
        Window? owner = _ownerAccessor();
        if (owner is null)
        {
            return null;
        }

        return await PrintDialog.ShowAsync(owner, new PrintDialogViewModel(printers));
    }
}
