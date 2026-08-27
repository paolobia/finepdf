namespace FinePdf.Core.Models;

/// <summary>
/// The order in which N-up cells on a sheet are filled with placements.
/// </summary>
public enum NUpOrder
{
    /// <summary>Left-to-right, then top-to-bottom (default).</summary>
    RowMajor,

    /// <summary>Top-to-bottom, then left-to-right.</summary>
    ColumnMajor,
}
