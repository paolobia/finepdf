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

    /// <summary>Absolute path to the PDF on disk.</summary>
    public string Path { get; }

    public string FileName { get; }

    public IReadOnlyList<SourcePage> Pages { get; }

    public int PageCount => Pages.Count;

    public override string ToString() => $"{FileName} ({PageCount} pg)";
}
