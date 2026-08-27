using System.Globalization;

namespace FinePdf.Core.Models;

/// <summary>
/// A 1-based selection of output sheets, e.g. <c>"1-3,5,8-10"</c>. An empty/blank spec means
/// "every sheet". Used by <see cref="PrintOptions"/>.
/// </summary>
public sealed class PageRange
{
    private readonly (int From, int To)[] _spans;

    private PageRange((int From, int To)[] spans, bool isAll, string text)
    {
        _spans = spans;
        IsAll = isAll;
        Text = text;
    }

    /// <summary>Every sheet.</summary>
    public static PageRange All { get; } = new([], isAll: true, string.Empty);

    public bool IsAll { get; }

    /// <summary>The original, normalised text (empty when <see cref="IsAll"/>).</summary>
    public string Text { get; }

    /// <summary>
    /// Parses a range spec. <c>null</c>, empty or whitespace yields <see cref="All"/>.
    /// </summary>
    /// <exception cref="FormatException">The spec is malformed.</exception>
    public static PageRange Parse(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return All;
        }

        var spans = new List<(int From, int To)>();
        foreach (string rawPart in text.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            string[] ends = rawPart.Split('-', StringSplitOptions.TrimEntries);
            if (ends.Length is 1)
            {
                int single = ParsePositive(ends[0], rawPart);
                spans.Add((single, single));
            }
            else if (ends.Length is 2)
            {
                int from = ParsePositive(ends[0], rawPart);
                int to = ParsePositive(ends[1], rawPart);
                spans.Add(from <= to ? (from, to) : (to, from));
            }
            else
            {
                throw new FormatException($"Intervallo non valido: «{rawPart}».");
            }
        }

        return new PageRange([.. spans], isAll: false, text.Trim());
    }

    public static bool TryParse(string? text, out PageRange range)
    {
        try
        {
            range = Parse(text);
            return true;
        }
        catch (FormatException)
        {
            range = All;
            return false;
        }
    }

    /// <summary>
    /// The concrete 1-based sheet numbers this range selects, clamped to
    /// <paramref name="totalSheets"/>, sorted ascending and de-duplicated.
    /// </summary>
    public IReadOnlyList<int> Resolve(int totalSheets)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(totalSheets);
        if (IsAll)
        {
            return [.. Enumerable.Range(1, totalSheets)];
        }

        var pages = new SortedSet<int>();
        foreach ((int from, int to) in _spans)
        {
            for (int p = Math.Max(1, from); p <= Math.Min(totalSheets, to); p++)
            {
                pages.Add(p);
            }
        }

        return [.. pages];
    }

    public override string ToString() => IsAll ? "tutti" : Text;

    private static int ParsePositive(string token, string part)
    {
        if (!int.TryParse(token, NumberStyles.Integer, CultureInfo.InvariantCulture, out int value) || value < 1)
        {
            throw new FormatException($"Intervallo non valido: «{part}».");
        }

        return value;
    }
}
