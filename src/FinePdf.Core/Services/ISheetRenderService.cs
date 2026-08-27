using FinePdf.Core.Models;

namespace FinePdf.Core.Services;

/// <summary>
/// Renders a single <see cref="Sheet"/> to a PNG for the on-screen print preview, using the
/// same <see cref="Layout.SheetLayout"/> maths as <see cref="IPdfComposer"/> so preview and
/// output match.
/// </summary>
public interface ISheetRenderService
{
    /// <summary>
    /// Renders <paramref name="sheet"/> as a PNG image <paramref name="pixelWidth"/> pixels
    /// wide (height follows the sheet's aspect ratio). Results are cached.
    /// </summary>
    byte[] RenderSheetPng(Composition composition, Sheet sheet, int pixelWidth);

    /// <summary>Drops every cached render (e.g. when a source file changed on disk).</summary>
    void InvalidateCache();
}
