namespace FinePdf.Core.Models;

/// <summary>
/// How a source page is placed inside its cell on the output sheet.
/// </summary>
public enum FitMode
{
    /// <summary>Scale down/up to fit entirely, preserving aspect ratio (may leave white margins). Default.</summary>
    Fit,

    /// <summary>Keep the content at 1:1 and centre it; overflow is clipped to the cell.</summary>
    ActualSize,

    /// <summary>Scale to cover the whole cell, preserving aspect ratio; overflow is clipped.</summary>
    Fill,
}
