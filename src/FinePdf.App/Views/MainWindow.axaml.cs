using System;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.VisualTree;
using FinePdf.App.ViewModels;

namespace FinePdf.App.Views;

public partial class MainWindow : Window
{
    private const string SheetDragFormat = "finepdf-sheet-index";

    private bool _forceClose;
    private Point _dragOrigin;
    private SheetViewModel? _dragCandidate;
    private bool _dragging;

    public MainWindow()
    {
        InitializeComponent();
#if DEBUG
        this.AttachDevTools();
#endif
        SheetList.AddHandler(PointerPressedEvent, OnSheetPointerPressed, RoutingStrategies.Tunnel);
        SheetList.AddHandler(PointerMovedEvent, OnSheetPointerMoved, RoutingStrategies.Tunnel);
        SheetList.AddHandler(PointerReleasedEvent, OnSheetPointerReleased, RoutingStrategies.Tunnel);
        SheetList.AddHandler(DragDrop.DragOverEvent, OnSheetDragOver);
        SheetList.AddHandler(DragDrop.DropEvent, OnSheetDrop);
    }

    private void OnSourceDoubleTapped(object? sender, TappedEventArgs e)
    {
        if (DataContext is MainWindowViewModel vm &&
            sender is ListBox { SelectedItem: SourceFileViewModel file })
        {
            vm.AddSource(file);
        }
    }

    private void OnSheetPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (!e.GetCurrentPoint(SheetList).Properties.IsLeftButtonPressed)
        {
            return;
        }

        _dragCandidate = SheetUnder(e.Source as Visual);
        _dragOrigin = e.GetPosition(SheetList);
        _dragging = false;
    }

    private void OnSheetPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        _dragCandidate = null;
        _dragging = false;
    }

    private async void OnSheetPointerMoved(object? sender, PointerEventArgs e)
    {
        if (_dragCandidate is null || _dragging ||
            !e.GetCurrentPoint(SheetList).Properties.IsLeftButtonPressed ||
            DataContext is not MainWindowViewModel vm)
        {
            return;
        }

        Point now = e.GetPosition(SheetList);
        if (Math.Abs(now.X - _dragOrigin.X) < 6 && Math.Abs(now.Y - _dragOrigin.Y) < 6)
        {
            return;
        }

        int from = vm.Sheets.IndexOf(_dragCandidate);
        if (from < 0)
        {
            return;
        }

        _dragging = true;
        var data = new DataObject();
        data.Set(SheetDragFormat, from);
        try
        {
            await DragDrop.DoDragDrop(e, data, DragDropEffects.Move);
        }
        finally
        {
            _dragCandidate = null;
            _dragging = false;
        }
    }

    private void OnSheetDragOver(object? sender, DragEventArgs e)
    {
        e.DragEffects = e.Data.Contains(SheetDragFormat) ? DragDropEffects.Move : DragDropEffects.None;
        e.Handled = true;
    }

    private void OnSheetDrop(object? sender, DragEventArgs e)
    {
        e.Handled = true;
        if (DataContext is not MainWindowViewModel vm || e.Data.Get(SheetDragFormat) is not int from)
        {
            return;
        }

        SheetViewModel? target = SheetUnder(e.Source as Visual);
        int to = target is not null ? vm.Sheets.IndexOf(target) : vm.Sheets.Count - 1;
        vm.MoveSheetByDrag(from, to);
    }

    private static SheetViewModel? SheetUnder(Visual? source) =>
        source?.GetSelfAndVisualAncestors()
            .OfType<ListBoxItem>()
            .FirstOrDefault()?.DataContext as SheetViewModel;

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
