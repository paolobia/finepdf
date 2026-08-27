using FinePdf.Core.Models;

namespace FinePdf.Core.Services;

/// <summary>
/// Turns a <see cref="Composition"/> into a PDF file. One sheet becomes one output page in
/// the document's paper size and orientation; each placement is drawn into its cell using
/// <see cref="Layout.SheetLayout"/> — the same maths the preview renderer uses.
/// </summary>
public interface IPdfComposer
{
    /// <exception cref="InvalidOperationException">The composition has no sheets.</exception>
    void Compose(Composition composition, Stream output);

    /// <inheritdoc cref="Compose(Composition, Stream)" />
    void Compose(Composition composition, string outputPath);
}
