using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using FinePdf.Core.Models;

namespace FinePdf.App.ViewModels;

/// <summary>One sheet in the right-hand print preview.</summary>
public partial class SheetViewModel : ViewModelBase
{
    private readonly Func<Sheet, int, byte[]> _render;
    private int _lastWidth;

    [ObservableProperty]
    private bool _isSelected;

    [ObservableProperty]
    private Bitmap? _preview;

    [ObservableProperty]
    private string _title = string.Empty;

    [ObservableProperty]
    private string _caption = string.Empty;

    public SheetViewModel(Sheet sheet, Func<Sheet, int, byte[]> render)
    {
        Sheet = sheet;
        _render = render;
    }

    public event EventHandler? SelectionChanged;

    public Sheet Sheet { get; }

    public void UpdateLabels(int position)
    {
        Title = $"Foglio {position}";

        int total = Sheet.Placements.Count;
        int blanks = total - Sheet.ContentPlacementCount;
        Caption = Sheet.IsBlank
            ? "pagina vuota"
            : total == 1
                ? DescribePlacement(Sheet.Placements[0])
                : blanks == 0
                    ? $"{total} pagine su un foglio"
                    : $"{total - blanks} pagine + {blanks} vuota su un foglio";
    }

    public async Task RenderAsync(int pixelWidth, CancellationToken cancellationToken = default)
    {
        if (pixelWidth < 1)
        {
            return;
        }

        _lastWidth = pixelWidth;

        try
        {
            byte[] png = await Task.Run(() => _render(Sheet, pixelWidth), cancellationToken).ConfigureAwait(true);
            if (cancellationToken.IsCancellationRequested)
            {
                return;
            }

            var bitmap = new Bitmap(new MemoryStream(png));
            Bitmap? previous = Preview;
            Preview = bitmap;
            previous?.Dispose();
        }
        catch (OperationCanceledException)
        {
            // ignored
        }
        catch (Exception)
        {
            // keep whatever preview we already had
        }
    }

    public Task RefreshAsync(CancellationToken cancellationToken = default) =>
        _lastWidth > 0 ? RenderAsync(_lastWidth, cancellationToken) : Task.CompletedTask;

    partial void OnIsSelectedChanged(bool value) => SelectionChanged?.Invoke(this, EventArgs.Empty);

    private static string DescribePlacement(Placement placement)
    {
        string rotation = placement.Rotation.Value == 0 ? string.Empty : $" · {placement.Rotation.Value}°";
        return $"{placement.Source.FileName} — p.{placement.SourcePageIndex + 1}{rotation}";
    }
}
