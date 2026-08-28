using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using FinePdf.App.ViewModels;

namespace FinePdf.App.Views;

public partial class PrinterSetupDialog : Window
{
    public PrinterSetupDialog()
    {
        InitializeComponent();
    }

    private void OnClose(object? sender, RoutedEventArgs e) => Close(null);

    private void OnInstall(object? sender, RoutedEventArgs e)
    {
        if (DataContext is PrinterSetupDialogViewModel { CanInstall: true } vm)
        {
            vm.ConfirmInstall();
            Close(vm.Result);
        }
    }

    private void OnUninstall(object? sender, RoutedEventArgs e)
    {
        if (DataContext is PrinterSetupDialogViewModel vm)
        {
            vm.ConfirmUninstall();
            Close(vm.Result);
        }
    }

    /// <summary>Shows the dialog; returns the chosen action, or <c>null</c> if closed/cancelled.</summary>
    public static Task<PrinterSetupResult?> ShowAsync(Window owner, PrinterSetupDialogViewModel viewModel)
    {
        var dialog = new PrinterSetupDialog { DataContext = viewModel };
        return dialog.ShowDialog<PrinterSetupResult?>(owner);
    }
}
