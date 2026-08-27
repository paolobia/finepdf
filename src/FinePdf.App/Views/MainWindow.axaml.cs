using System;
using System.ComponentModel;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using FinePdf.App.ViewModels;

namespace FinePdf.App.Views;

public partial class MainWindow : Window
{
    private bool _forceClose;

    public MainWindow()
    {
        InitializeComponent();
#if DEBUG
        this.AttachDevTools();
#endif
    }

    private void OnSourceDoubleTapped(object? sender, TappedEventArgs e)
    {
        if (DataContext is MainWindowViewModel vm &&
            sender is ListBox { SelectedItem: SourceFileViewModel file })
        {
            vm.AddSource(file);
        }
    }

    protected override async void OnClosing(WindowClosingEventArgs e)
    {
        base.OnClosing(e);

        if (_forceClose || DataContext is not MainWindowViewModel { HasUnsavedChanges: true })
        {
            return;
        }

        e.Cancel = true;
        bool leave = await ConfirmDiscardAsync();
        if (leave)
        {
            _forceClose = true;
            Close();
        }
    }

    private async Task<bool> ConfirmDiscardAsync()
    {
        var discard = new Button { Content = "Esci senza salvare", HorizontalAlignment = HorizontalAlignment.Right };
        var cancel = new Button { Content = "Annulla", HorizontalAlignment = HorizontalAlignment.Right, IsDefault = true };

        var dialog = new Window
        {
            Title = "Modifiche non salvate",
            SizeToContent = SizeToContent.WidthAndHeight,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            CanResize = false,
            Content = new StackPanel
            {
                Margin = new Thickness(20),
                Spacing = 16,
                Children =
                {
                    new TextBlock { Text = "Ci sono modifiche non salvate. Uscire comunque?" },
                    new StackPanel
                    {
                        Orientation = Orientation.Horizontal,
                        Spacing = 8,
                        HorizontalAlignment = HorizontalAlignment.Right,
                        Children = { discard, cancel },
                    },
                },
            },
        };

        var result = new TaskCompletionSource<bool>();
        discard.Click += (_, _) => { result.TrySetResult(true); dialog.Close(); };
        cancel.Click += (_, _) => { result.TrySetResult(false); dialog.Close(); };
        dialog.Closed += (_, _) => result.TrySetResult(false);

        await dialog.ShowDialog(this);
        return await result.Task;
    }
}
