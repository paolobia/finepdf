using FinePdf.Core.Models;

namespace FinePdf.Core.Operations.Actions;

/// <summary>Sets the document-wide <see cref="Composition.Margins"/>.</summary>
public sealed class SetMarginsAction : IUndoableAction
{
    private readonly Composition _composition;
    private readonly PageMargins _margins;
    private PageMargins? _previous;

    public SetMarginsAction(Composition composition, PageMargins margins)
    {
        ArgumentNullException.ThrowIfNull(composition);
        ArgumentNullException.ThrowIfNull(margins);

        _composition = composition;
        _margins = margins;
    }

    public string Description => _margins.IsZero
        ? "Margini: nessuno"
        : $"Margini: {_margins.MaxEdgeMillimeters:0.#} mm";

    public void Do()
    {
        _previous = _composition.Margins;
        _composition.Margins = _margins;
    }

    public void Undo()
    {
        if (_previous is not null)
        {
            _composition.Margins = _previous;
        }
    }
}
