using FinePdf.Core.Models;

namespace FinePdf.Core.Operations.Actions;

/// <summary>Changes <see cref="Composition.DefaultFitMode"/> (the fallback for sheets without an override).</summary>
public sealed class SetDefaultFitModeAction : IUndoableAction
{
    private readonly Composition _composition;
    private readonly FitMode _mode;
    private FitMode? _previous;

    public SetDefaultFitModeAction(Composition composition, FitMode mode)
    {
        ArgumentNullException.ThrowIfNull(composition);
        _composition = composition;
        _mode = mode;
    }

    public string Description => $"Adattamento predefinito: {_mode}";

    public void Do()
    {
        _previous = _composition.DefaultFitMode;
        _composition.DefaultFitMode = _mode;
    }

    public void Undo()
    {
        if (_previous is { } mode)
        {
            _composition.DefaultFitMode = mode;
        }
    }
}
