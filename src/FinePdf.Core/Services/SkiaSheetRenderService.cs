using System.Collections.Concurrent;
using System.Globalization;
using System.Text;
using FinePdf.Core.Layout;
using FinePdf.Core.Models;
using PDFtoImage;
using SkiaSharp;

namespace FinePdf.Core.Services;

/// <inheritdoc />
public sealed class SkiaSheetRenderService : ISheetRenderService
{
    private const int MaxCacheEntries = 300;

    private readonly ConcurrentDictionary<string, byte[]> _sourceBytes = new(StringComparer.Ordinal);
    private readonly Dictionary<string, byte[]> _cache = new(StringComparer.Ordinal);
    private readonly LinkedList<string> _lru = new();
    private readonly object _gate = new();

    /// <inheritdoc />
    public byte[] RenderSheetPng(Composition composition, Sheet sheet, int pixelWidth)
    {
        ArgumentNullException.ThrowIfNull(composition);
        ArgumentNullException.ThrowIfNull(sheet);
        ArgumentOutOfRangeException.ThrowIfLessThan(pixelWidth, 1);

        string key = BuildKey(composition, sheet, pixelWidth);
        lock (_gate)
        {
            if (_cache.TryGetValue(key, out byte[]? hit))
            {
                MarkUsed(key);
                return hit;
            }
        }

        byte[] png = Render(composition, sheet, pixelWidth);

        lock (_gate)
        {
            if (_cache.TryAdd(key, png))
            {
                _lru.AddFirst(key);
                Evict();
            }
        }

        return png;
    }

    /// <inheritdoc />
    public void InvalidateCache()
    {
        lock (_gate)
        {
            _cache.Clear();
            _lru.Clear();
        }

        _sourceBytes.Clear();
    }

    private byte[] Render(Composition composition, Sheet sheet, int pixelWidth)
    {
        (double sheetWidthPt, double sheetHeightPt) = composition.SheetSizePoints;
        double scale = pixelWidth / sheetWidthPt;
        int pixelHeight = Math.Max(1, (int)Math.Round(sheetHeightPt * scale));

        var info = new SKImageInfo(pixelWidth, pixelHeight);
        using SKSurface surface = SKSurface.Create(info);
        SKCanvas canvas = surface.Canvas;
        canvas.Clear(SKColors.White);

        using (var border = new SKPaint { Style = SKPaintStyle.Stroke, Color = new SKColor(0xCC, 0xCC, 0xCC), StrokeWidth = 1 })
        {
            canvas.DrawRect(0.5f, 0.5f, pixelWidth - 1f, pixelHeight - 1f, border);
        }

        PageMargins margins = composition.Margins;
        if (!margins.IsZero)
        {
            using var guide = new SKPaint
            {
                Style = SKPaintStyle.Stroke,
                Color = new SKColor(0x99, 0xB0, 0xD8),
                StrokeWidth = 1,
                PathEffect = SKPathEffect.CreateDash([4f, 3f], 0f),
            };
            canvas.DrawRect(
                (float)(margins.Left * scale),
                (float)(margins.Top * scale),
                (float)((sheetWidthPt - margins.Left - margins.Right) * scale),
                (float)((sheetHeightPt - margins.Top - margins.Bottom) * scale),
                guide);
        }

        FitMode fit = sheet.EffectiveFitMode(composition);
        int placementCount = sheet.Placements.Count;

        for (int i = 0; i < placementCount; i++)
        {
            Placement placement = sheet.Placements[i];
            if (placement.IsBlank)
            {
                // Blank placement: leave the cell as the white surface already painted.
                continue;
            }

            SourcePage sourcePage = placement.Page;

            Rect cell = SheetLayout.CellFor(
                placementCount, i, sheetWidthPt, sheetHeightPt, sheet.NUpOrder, composition.Margins);
            PlacedContent placed = SheetLayout.Place(
                cell, sourcePage.WidthPoints, sourcePage.HeightPoints, placement.Rotation.Value, fit);

            int targetPixels = Math.Max(1, (int)Math.Round(placed.Width * scale));
            using SKBitmap? pageBitmap = RenderSourcePage(placement, targetPixels);
            if (pageBitmap is null)
            {
                continue;
            }

            var dest = new SKRect(
                (float)(-placed.Width * scale / 2),
                (float)(-placed.Height * scale / 2),
                (float)(placed.Width * scale / 2),
                (float)(placed.Height * scale / 2));

            canvas.Save();
            canvas.Translate((float)(placed.CenterX * scale), (float)(placed.CenterY * scale));
            canvas.RotateDegrees(placed.RotationDegrees);
            using (var bitmapPaint = new SKPaint { FilterQuality = SKFilterQuality.Medium, IsAntialias = true })
            {
                canvas.DrawBitmap(pageBitmap, dest, bitmapPaint);
            }
            canvas.Restore();
        }

        canvas.Flush();
        using SKImage image = surface.Snapshot();
        using SKData data = image.Encode(SKEncodedImageFormat.Png, 100);
        return data.ToArray();
    }

    private SKBitmap? RenderSourcePage(Placement placement, int targetPixelWidth)
    {
        byte[] bytes = _sourceBytes.GetOrAdd(placement.Source.Path, static path => File.ReadAllBytes(path));

        var options = new RenderOptions
        {
            Width = targetPixelWidth,
            WithAspectRatio = true,
        };

        try
        {
            // PDFium (via PDFtoImage) supports every desktop OS this app targets.
#pragma warning disable CA1416
            return Conversion.ToImage(bytes, password: null, page: (Index)placement.SourcePageIndex, options: options);
#pragma warning restore CA1416
        }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            // A single unrenderable page must not blank the whole preview.
            return null;
        }
    }

    private void MarkUsed(string key)
    {
        _lru.Remove(key);
        _lru.AddFirst(key);
    }

    private void Evict()
    {
        while (_cache.Count > MaxCacheEntries && _lru.Last is { } oldest)
        {
            _lru.RemoveLast();
            _cache.Remove(oldest.Value);
        }
    }

    private static string BuildKey(Composition composition, Sheet sheet, int pixelWidth)
    {
        var sb = new StringBuilder();
        (double width, double height) = composition.SheetSizePoints;
        sb.Append(width.ToString("F1", CultureInfo.InvariantCulture)).Append('x')
          .Append(height.ToString("F1", CultureInfo.InvariantCulture)).Append('|')
          .Append(pixelWidth).Append('|')
          .Append(sheet.EffectiveFitMode(composition)).Append('|')
          .Append(sheet.NUpOrder).Append('|')
          .Append(composition.Margins.Left.ToString("F1", CultureInfo.InvariantCulture)).Append(',')
          .Append(composition.Margins.Top.ToString("F1", CultureInfo.InvariantCulture)).Append(',')
          .Append(composition.Margins.Right.ToString("F1", CultureInfo.InvariantCulture)).Append(',')
          .Append(composition.Margins.Bottom.ToString("F1", CultureInfo.InvariantCulture)).Append('|');

        foreach (Placement placement in sheet.Placements)
        {
            sb.Append(placement.Source.Path).Append('#')
              .Append(placement.SourcePageIndex).Append('r')
              .Append(placement.Rotation.Value).Append(';');
        }

        return sb.ToString();
    }
}
