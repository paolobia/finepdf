using System.Collections.Generic;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using FinePdf.Core.Models;
using FinePdf.Core.Services;

namespace FinePdf.App.ViewModels;

/// <summary>Backs the internal print dialog: printer, copies, range, one/two-sided.</summary>
public partial class PrintDialogViewModel : ViewModelBase
{
    [ObservableProperty]
    private PrinterInfo? _selectedPrinter;

    [ObservableProperty]
    private int _copies = 1;

    [ObservableProperty]
    private string _rangeText = string.Empty;

    [ObservableProperty]
    private DuplexChoice _selectedDuplex;

    [ObservableProperty]
    private string _advancedOptions = string.Empty;

    public PrintDialogViewModel(IReadOnlyList<PrinterInfo> printers)
    {
        Printers = printers;
        _selectedPrinter = printers.FirstOrDefault(p => p.IsDefault) ?? printers.FirstOrDefault();
        _selectedDuplex = Duplexes[0];
    }

    public IReadOnlyList<PrinterInfo> Printers { get; }

    public IReadOnlyList<DuplexChoice> Duplexes { get; } =
    [
        new(DuplexMode.Simplex, "Una facciata"),
        new(DuplexMode.LongEdge, "Fronte/retro, lato lungo"),
        new(DuplexMode.ShortEdge, "Fronte/retro, lato corto"),
    ];

    public bool RangeIsValid => PageRange.TryParse(RangeText, out _);

    public bool HasPrinters => Printers.Count > 0;

    public bool CanConfirm => HasPrinters && SelectedPrinter is not null && Copies >= 1 && RangeIsValid;

    public PrintOptions BuildOptions() => new(SelectedPrinter!.Name)
    {
        Copies = Copies,
        Duplex = SelectedDuplex.Mode,
        Range = PageRange.Parse(RangeText),
        RawOptions = ParseAdvancedOptions(AdvancedOptions),
    };

    private static IReadOnlyList<string> ParseAdvancedOptions(string text) =>
    [
        .. text.Split(new[] { '\n', '\r', ';' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
               .Where(entry => entry.Contains('=', StringComparison.Ordinal)),
    ];

    partial void OnSelectedPrinterChanged(PrinterInfo? value) => OnPropertyChanged(nameof(CanConfirm));

    partial void OnCopiesChanged(int value) => OnPropertyChanged(nameof(CanConfirm));

    partial void OnSelectedDuplexChanged(DuplexChoice value) => OnPropertyChanged(nameof(CanConfirm));

    partial void OnRangeTextChanged(string value)
    {
        OnPropertyChanged(nameof(RangeIsValid));
        OnPropertyChanged(nameof(CanConfirm));
    }
}

/// <summary>A duplex option with its Italian label for the combo box.</summary>
public sealed record DuplexChoice(DuplexMode Mode, string Label)
{
    public override string ToString() => Label;
}
