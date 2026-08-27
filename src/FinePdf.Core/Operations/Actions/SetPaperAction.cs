using FinePdf.Core.Models;

namespace FinePdf.Core.Operations.Actions;

/// <summary>Changes the document-wide paper size and/or orientation of the print preview.</summary>
public sealed class SetPaperAction : IUndoableAction
{
    private readonly Composition _composition;
    private readonly PaperSize _paperSize;
    private readonly PageOrientation _orientation;

    private PaperSize? _previousPaperSize;
    private PageOrientation _previousOrientation;

    public SetPaperAction(Composition composition, PaperSize paperSize, PageOrientation orientation)
    {
        ArgumentNullException.ThrowIfNull(composition);
        ArgumentNullException.ThrowIfNull(paperSize);

        _composition = composition;
        _paperSize = paperSize;
        _orientation = orientation;
    }

    public string Description =>
        $"Formato {_paperSize.Name} {(_orientation == PageOrientation.Portrait ? "verticale" : "orizzontale")}";

    public void Do()
    {
        _previousPaperSize = _composition.PaperSize;
        _previousOrientation = _composition.Orientation;
        _composition.PaperSize = _paperSize;
        _composition.Orientation = _orientation;
    }

    public void Undo()
    {
        if (_previousPaperSize is null)
        {
            return;
        }

        _composition.PaperSize = _previousPaperSize;
        _composition.Orientation = _previousOrientation;
    }
}
