using FinePdf.Core.Models;

namespace FinePdf.Core.Tests;

public class PageRangeTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Parse_BlankSpec_IsAll(string? text)
    {
        PageRange range = PageRange.Parse(text);

        Assert.True(range.IsAll);
        Assert.Equal(new[] { 1, 2, 3, 4, 5 }, range.Resolve(5));
    }

    [Fact]
    public void Parse_MixedSpec_ResolvesSortedDistinctAndClamped()
    {
        PageRange range = PageRange.Parse("3-1, 5, 8-10, 5");

        Assert.False(range.IsAll);
        Assert.Equal(new[] { 1, 2, 3, 5, 8, 9, 10 }, range.Resolve(12));
        Assert.Equal(new[] { 1, 2, 3, 5, 8, 9 }, range.Resolve(9));
    }

    [Theory]
    [InlineData("abc")]
    [InlineData("1-2-3")]
    [InlineData("0")]
    [InlineData("4-")]
    public void Parse_Malformed_Throws(string text)
    {
        Assert.Throws<FormatException>(() => PageRange.Parse(text));
    }

    [Fact]
    public void TryParse_Malformed_ReturnsFalseAndAll()
    {
        Assert.False(PageRange.TryParse("nope", out PageRange range));
        Assert.True(range.IsAll);
    }

    [Fact]
    public void Resolve_RangeBeyondDocument_YieldsNothing()
    {
        PageRange range = PageRange.Parse("50-60");

        Assert.Empty(range.Resolve(10));
    }
}
