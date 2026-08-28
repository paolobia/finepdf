namespace FinePdf.Core.Models;

/// <summary>
/// A PDF file the user can draw pages from. Immutable snapshot taken at load time:
/// path plus per-page metadata. The bytes are re-read from <see cref="Path"/> only when
/// composing or rendering.
/// </summary>
public sealed class SourceDocument
{
    public SourceDocument(string path, IReadOnlyList<SourcePage> pages)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentNullException.ThrowIfNull(pages);

        Path = path;
        FileName = System.IO.Path.GetFileName(path);
        Pages = pages;
    }

    /// <summary>Absolute path to the PDF on disk (or <c>"(blank)"</c> for a synthetic blank page).</summary>
    public string Path { get; }

    public string FileName { get; }

    public IReadOnlyList<SourcePage> Pages { get; }

    public int PageCount => Pages.Count;

    /// <summary>
    /// <c>true</c> for the synthetic single blank page produced by <see cref="CreateBlank"/>.
    /// Composer and preview leave a blank placement's cell white and never touch <see cref="Path"/>.
    /// </summary>
    public bool IsBlank { get; init; }

    /// <summary>A one-page, no-content document sized to the given sheet dimensions (in points).</summary>
    public static SourceDocument CreateBlank(double widthPoints, double heightPoints) =>
        new("(blank)", [new SourcePage(0, widthPoints, heightPoints)]) { IsBlank = true };

    public override string ToString() => IsBlank ? "(pagina vuota)" : $"{FileName} ({PageCount} pg)";
}
