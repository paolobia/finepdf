namespace FinePdf.Core.Models;

/// <summary>Two-sided printing mode.</summary>
public enum DuplexMode
{
    /// <summary>One-sided.</summary>
    Simplex,

    /// <summary>Two-sided, flipped on the long edge (portrait book binding).</summary>
    LongEdge,

    /// <summary>Two-sided, flipped on the short edge (landscape / notepad binding).</summary>
    ShortEdge,
}
