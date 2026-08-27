namespace FinePdf.Core.Models;

/// <summary>
/// One sheet of the output document. Holds an ordered list of <see cref="Placement"/>:
/// <list type="bullet">
///   <item>0 placements — a blank sheet;</item>
///   <item>1 placement — a normal page;</item>
///   <item>2 or 4 placements — an N-up imposition.</item>
/// </list>
/// The paper size and orientation come from the owning <see cref="Composition"/>; only the
/// <see cref="FitModeOverride"/> is per-sheet.
/// </summary>
public sealed class Sheet
{
    private readonly List<Placement> _placements;

    public Sheet()
    {
        _placements = [];
    }

    public Sheet(IEnumerable<Placement> placements)
    {
        ArgumentNullException.ThrowIfNull(placements);
        _placements = [.. placements];
    }

    public Sheet(params Placement[] placements)
        : this((IEnumerable<Placement>)placements)
    {
    }

    public Guid Id { get; } = Guid.NewGuid();

    public IReadOnlyList<Placement> Placements => _placements;

    public bool IsBlank => _placements.Count == 0;

    /// <summary>When set, overrides <see cref="Composition.DefaultFitMode"/> for this sheet.</summary>
    public FitMode? FitModeOverride { get; set; }

    public FitMode EffectiveFitMode(Composition composition)
    {
        ArgumentNullException.ThrowIfNull(composition);
        return FitModeOverride ?? composition.DefaultFitMode;
    }

    public void AddPlacement(Placement placement)
    {
        ArgumentNullException.ThrowIfNull(placement);
        _placements.Add(placement);
    }

    public void InsertPlacement(int index, Placement placement)
    {
        ArgumentNullException.ThrowIfNull(placement);
        _placements.Insert(index, placement);
    }

    public void ReplacePlacement(int index, Placement placement)
    {
        ArgumentNullException.ThrowIfNull(placement);
        _placements[index] = placement;
    }

    public void RemovePlacementAt(int index) => _placements.RemoveAt(index);
}
