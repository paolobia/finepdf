using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using FinePdf.App.ViewModels;
using FinePdf.Core.Models;

namespace FinePdf.App.Views;

public partial class PrintDialog : Window
{
    public PrintDialog()
    {
        InitializeComponent();
    }

    private void OnCancel(object? sender, RoutedEventArgs e) => Close(null);

    private void OnConfirm(object? sender, RoutedEventArgs e)
    {
        if (DataContext is PrintDialogViewModel { CanConfirm: true } vm)
        {
            Close(vm.BuildOptions());
        }
    }

    /// <summary>Shows the dialog and returns the chosen options, or <c>null</c> if cancelled.</summary>
    public static Task<PrintOptions?> ShowAsync(Window owner, PrintDialogViewModel viewModel)
    {
        var dialog = new PrintDialog { DataContext = viewModel };
        return dialog.ShowDialog<PrintOptions?>(owner);
    }
}
