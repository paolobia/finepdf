using FinePdf.Core.Layout;
using FinePdf.Core.Models;

namespace FinePdf.Core.Tests;

public class SheetLayoutTests
{
    [Fact]
    public void CellFor_SinglePlacement_FillsWholeSheet()
    {
        Rect cell = SheetLayout.CellFor(1, 0, 600, 800);

        Assert.Equal(new Rect(0, 0, 600, 800), cell);
    }

    [Fact]
    public void CellFor_TwoOnPortraitSheet_StacksVertically()
    {
        Rect top = SheetLayout.CellFor(2, 0, 600, 800);
        Rect bottom = SheetLayout.CellFor(2, 1, 600, 800);

        Assert.Equal(new Rect(0, 0, 600, 400), top);
        Assert.Equal(new Rect(0, 400, 600, 400), bottom);
    }

    [Fact]
    public void CellFor_TwoOnLandscapeSheet_SplitsSideBySide()
    {
        Rect left = SheetLayout.CellFor(2, 0, 800, 600);
        Rect right = SheetLayout.CellFor(2, 1, 800, 600);

        Assert.Equal(new Rect(0, 0, 400, 600), left);
        Assert.Equal(new Rect(400, 0, 400, 600), right);
    }

    [Fact]
    public void CellFor_FourPlacements_FormsTwoByTwoGrid()
    {
        Assert.Equal(new Rect(0, 0, 300, 400), SheetLayout.CellFor(4, 0, 600, 800));
        Assert.Equal(new Rect(300, 0, 300, 400), SheetLayout.CellFor(4, 1, 600, 800));
        Assert.Equal(new Rect(0, 400, 300, 400), SheetLayout.CellFor(4, 2, 600, 800));
        Assert.Equal(new Rect(300, 400, 300, 400), SheetLayout.CellFor(4, 3, 600, 800));
    }

    [Fact]
    public void CellFor_IndexOutOfRange_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => SheetLayout.CellFor(2, 2, 600, 800));
    }

    [Fact]
    public void Place_Fit_ScalesToFitAndCentres()
    {
        var cell = new Rect(0, 0, 300, 300);

        PlacedContent placed = SheetLayout.Place(cell, 100, 200, 0, FitMode.Fit);

        Assert.Equal(150, placed.CenterX);
        Assert.Equal(150, placed.CenterY);
        Assert.Equal(150, placed.Width, 3);   // 100 * (300/200)
        Assert.Equal(300, placed.Height, 3);  // 200 * 1.5
        Assert.Equal(0, placed.RotationDegrees);
    }

    [Fact]
    public void Place_Fill_CoversCell()
    {
        var cell = new Rect(0, 0, 300, 300);

        PlacedContent placed = SheetLayout.Place(cell, 100, 200, 0, FitMode.Fill);

        Assert.Equal(300, placed.Width, 3);   // scale = max(3, 1.5) = 3
        Assert.Equal(600, placed.Height, 3);
    }

    [Fact]
    public void Place_ActualSize_KeepsSourceDimensions()
    {
        var cell = new Rect(0, 0, 300, 300);

        PlacedContent placed = SheetLayout.Place(cell, 120, 90, 0, FitMode.ActualSize);

        Assert.Equal(120, placed.Width, 3);
        Assert.Equal(90, placed.Height, 3);
    }

    [Fact]
    public void Place_QuarterTurn_FitsRotatedBoundingBox()
    {
        var cell = new Rect(0, 0, 300, 300);

        // Source 100x200; rotated 90° its bounding box is 200x100, so scale = min(300/200, 300/100) = 1.5.
        PlacedContent placed = SheetLayout.Place(cell, 100, 200, 90, FitMode.Fit);

        Assert.Equal(150, placed.Width, 3);   // pre-rotation width 100 * 1.5
        Assert.Equal(300, placed.Height, 3);  // pre-rotation height 200 * 1.5
        Assert.Equal(90, placed.RotationDegrees);
    }

    [Fact]
    public void Place_RotationNotMultipleOf90_Throws()
    {
        var cell = new Rect(0, 0, 300, 300);

        Assert.Throws<ArgumentOutOfRangeException>(() => SheetLayout.Place(cell, 100, 100, 45, FitMode.Fit));
    }
}
