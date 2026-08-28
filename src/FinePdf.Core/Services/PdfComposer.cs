using FinePdf.Core.Layout;
using FinePdf.Core.Models;
using PdfSharp.Drawing;
using PdfSharp.Pdf;

namespace FinePdf.Core.Services;

/// <inheritdoc />
public sealed class PdfComposer : IPdfComposer
{
    /// <inheritdoc />
    public void Compose(Composition composition, Stream output)
    {
        ArgumentNullException.ThrowIfNull(composition);
        ArgumentNullException.ThrowIfNull(output);

        if (composition.Sheets.Count == 0)
        {
            throw new InvalidOperationException("The composition has no sheets to export.");
        }

        (double sheetWidth, double sheetHeight) = composition.SheetSizePoints;

        using var document = new PdfDocument();
        var forms = new Dictionary<string, XPdfForm>(StringComparer.Ordinal);
        try
        {
            foreach (Sheet sheet in composition.Sheets)
            {
                PdfPage page = document.AddPage();
                page.Width = XUnit.FromPoint(sheetWidth);
                page.Height = XUnit.FromPoint(sheetHeight);

                if (sheet.IsBlank)
                {
                    continue;
                }

                using XGraphics gfx = XGraphics.FromPdfPage(page);
                FitMode fit = sheet.EffectiveFitMode(composition);
                int placementCount = sheet.Placements.Count;

                for (int i = 0; i < placementCount; i++)
                {
                    Placement placement = sheet.Placements[i];
                    if (placement.IsBlank)
                    {
                        // A blank placement leaves its cell white — nothing to draw.
                        continue;
                    }

                    SourcePage sourcePage = placement.Page;

                    Rect cell = SheetLayout.CellFor(
                        placementCount, i, sheetWidth, sheetHeight, sheet.NUpOrder, composition.Margins);
                    PlacedContent placed = SheetLayout.Place(
                        cell,
                        sourcePage.WidthPoints,
                        sourcePage.HeightPoints,
                        placement.Rotation.Value,
                        fit);

                    XPdfForm form = GetForm(forms, placement);

                    XGraphicsState state = gfx.Save();
                    gfx.TranslateTransform(placed.CenterX, placed.CenterY);
                    if (placed.RotationDegrees != 0)
                    {
                        gfx.RotateTransform(placed.RotationDegrees);
                    }

                    gfx.DrawImage(form, -placed.Width / 2, -placed.Height / 2, placed.Width, placed.Height);
                    gfx.Restore(state);
                }
            }

            document.Save(output, closeStream: false);
        }
        finally
        {
            foreach (XPdfForm form in forms.Values)
            {
                form.Dispose();
            }
        }
    }

    /// <inheritdoc />
    public void Compose(Composition composition, string outputPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(outputPath);
        using FileStream file = File.Create(outputPath);
        Compose(composition, file);
    }

    private static XPdfForm GetForm(Dictionary<string, XPdfForm> cache, Placement placement)
    {
        string key = $"{placement.Source.Path}|{placement.SourcePageIndex}";
        if (!cache.TryGetValue(key, out XPdfForm? form))
        {
            form = XPdfForm.FromFile(placement.Source.Path);
            form.PageNumber = placement.SourcePageIndex + 1;
            cache[key] = form;
        }

        return form;
    }
}
