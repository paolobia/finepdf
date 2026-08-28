using FinePdf.Core.Layout;
using FinePdf.Core.Models;

namespace FinePdf.Core.Tests;

public class PageMarginsTests
{
    [Fact]
    public void FromMillimeters_ConvertsToPoints()
    {
        PageMargins m = PageMargins.FromMillimeters(10);

        Assert.Equal(28.35, m.Left, 2); // 10 mm * 72 / 25.4
        Assert.Equal(m.Left, m.Right, 6);
        Assert.Equal(10, m.MaxEdgeMillimeters, 2);
    }

    [Fact]
    public void Zero_IsZero()
    {
        Assert.True(PageMargins.Zero.IsZero);
        Assert.False(PageMargins.Uniform(1).IsZero);
    }

    [Fact]
    public void NegativeMargin_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new PageMargins(-1, 0, 0, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => PageMargins.FromMillimeters(-5));
    }

    [Fact]
    public void CellFor_WithUniformMargins_InsetsTheUsableArea()
    {
        Rect cell = SheetLayout.CellFor(1, 0, 600, 800, NUpOrder.RowMajor, PageMargins.Uniform(20));

        Assert.Equal(new Rect(20, 20, 560, 760), cell);
    }

    [Fact]
    public void CellFor_TwoUpWithMargins_SplitsInsideTheMargins()
    {
        // Portrait sheet => stacked; usable area 560x760 => two cells 560x380 offset by the margin.
        Rect top = SheetLayout.CellFor(2, 0, 600, 800, NUpOrder.RowMajor, PageMargins.Uniform(20));
        Rect bottom = SheetLayout.CellFor(2, 1, 600, 800, NUpOrder.RowMajor, PageMargins.Uniform(20));

        Assert.Equal(new Rect(20, 20, 560, 380), top);
        Assert.Equal(new Rect(20, 400, 560, 380), bottom);
    }

    [Fact]
    public void CellFor_MarginsLargerThanSheet_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => SheetLayout.CellFor(1, 0, 100, 100, NUpOrder.RowMajor, PageMargins.Uniform(60)));
    }
}
