using FinePdf.Core.Models;
using FinePdf.Core.Operations.Actions;
using FinePdf.Core.Services;
using FinePdf.Core.Tests.TestSupport;

namespace FinePdf.Core.Tests;

public class NUpActionTests
{
    private static Composition CompositionWith(TempDir dir, int pages)
    {
        var composition = new Composition();
        SourceDocument doc = new PdfSharpDocumentService().Load(PdfTestFactory.CreatePdf(dir.Path, "doc.pdf", pages));
        new AddFilePagesAction(composition, doc).Do();
        return composition;
    }

    [Fact]
    public void NUp_TwoPerSheet_ChunksPlacementsAcrossSheets()
    {
        using var dir = new TempDir();
        Composition composition = CompositionWith(dir, 5);

        new NUpAction(composition, [0, 1, 2, 3, 4], perSheet: 2).Do();

        Assert.Equal(3, composition.Sheets.Count);
        Assert.Equal(2, composition.Sheets[0].Placements.Count);
        Assert.Equal(2, composition.Sheets[1].Placements.Count);
        Assert.Single(composition.Sheets[2].Placements);
        Assert.Equal(new[] { 0, 1, 2, 3, 4 }, composition.Sheets.SelectMany(s => s.Placements).Select(p => p.SourcePageIndex));
    }

    [Fact]
    public void NUp_FourPerSheet_SetsOrderOnCreatedSheets()
    {
        using var dir = new TempDir();
        Composition composition = CompositionWith(dir, 8);

        new NUpAction(composition, [0, 1, 2, 3, 4, 5, 6, 7], perSheet: 4, order: NUpOrder.ColumnMajor).Do();

        Assert.Equal(2, composition.Sheets.Count);
        Assert.All(composition.Sheets, s => Assert.Equal(NUpOrder.ColumnMajor, s.NUpOrder));
    }

    [Fact]
    public void NUp_Undo_RestoresOriginalSheets()
    {
        using var dir = new TempDir();
        Composition composition = CompositionWith(dir, 4);

        var action = new NUpAction(composition, [0, 1, 2, 3], perSheet: 2);
        action.Do();
        Assert.Equal(2, composition.Sheets.Count);

        action.Undo();
        Assert.Equal(4, composition.Sheets.Count);
        Assert.Equal(new[] { 0, 1, 2, 3 }, composition.Sheets.Select(s => s.Placements[0].SourcePageIndex));
    }

    [Fact]
    public void NUp_PartialSelection_LeavesOtherSheetsInPlace()
    {
        using var dir = new TempDir();
        Composition composition = CompositionWith(dir, 5);

        new NUpAction(composition, [1, 2, 3], perSheet: 2).Do();

        // sheets 1..3 -> 2 N-up sheets; sheet 0 and sheet 4 untouched at the ends.
        Assert.Equal(4, composition.Sheets.Count);
        Assert.Equal(0, composition.Sheets[0].Placements[0].SourcePageIndex);
        Assert.Equal(4, composition.Sheets[^1].Placements[0].SourcePageIndex);
    }

    [Fact]
    public void NUp_InvalidPerSheet_Throws()
    {
        using var dir = new TempDir();
        Composition composition = CompositionWith(dir, 3);

        Assert.Throws<ArgumentOutOfRangeException>(() => new NUpAction(composition, [0, 1, 2], perSheet: 3));
    }

    [Fact]
    public void SetNUpOrder_AppliesAndUndoes()
    {
        using var dir = new TempDir();
        Composition composition = CompositionWith(dir, 4);
        new NUpAction(composition, [0, 1, 2, 3], perSheet: 2).Do();

        var action = new SetNUpOrderAction(composition.Sheets, NUpOrder.ColumnMajor);
        action.Do();
        Assert.All(composition.Sheets, s => Assert.Equal(NUpOrder.ColumnMajor, s.NUpOrder));

        action.Undo();
        Assert.All(composition.Sheets, s => Assert.Equal(NUpOrder.RowMajor, s.NUpOrder));
    }

    [Fact]
    public void NUp_TwoPerSheet_AutoOrientsPortraitPagesInLandscapeCells()
    {
        using var dir = new TempDir();
        Composition composition = CompositionWith(dir, 4); // A4 portrait pages, A4 portrait sheet

        new NUpAction(composition, [0, 1, 2, 3], perSheet: 2).Do();

        // 2-up on a portrait sheet => stacked landscape cells => each portrait page rotated 90°.
        Assert.All(
            composition.Sheets.SelectMany(s => s.Placements),
            p => Assert.Equal(90, p.Rotation.Value));
    }

    [Fact]
    public void NUp_AutoOrientDisabled_LeavesRotationUntouched()
    {
        using var dir = new TempDir();
        Composition composition = CompositionWith(dir, 4);

        new NUpAction(composition, [0, 1, 2, 3], perSheet: 2, order: NUpOrder.RowMajor, autoOrient: false).Do();

        Assert.All(
            composition.Sheets.SelectMany(s => s.Placements),
            p => Assert.Equal(0, p.Rotation.Value));
    }

    [Fact]
    public void NUp_FourPerSheet_PortraitPagesKeepOrientation()
    {
        using var dir = new TempDir();
        Composition composition = CompositionWith(dir, 4);

        new NUpAction(composition, [0, 1, 2, 3], perSheet: 4).Do();

        // 2x2 on a portrait sheet => portrait cells => portrait pages need no turn.
        Assert.All(composition.Sheets[0].Placements, p => Assert.Equal(0, p.Rotation.Value));
    }

    [Fact]
    public void FitOrientation_RotatesMismatchedPlacements_AndUndoes()
    {
        using var dir = new TempDir();
        Composition composition = CompositionWith(dir, 2);
        new MergeSheetsAction(composition, [0, 1]).Do(); // plain merge: no auto-orient

        var action = new FitOrientationAction(composition, [composition.Sheets[0]]);
        action.Do();
        Assert.All(composition.Sheets[0].Placements, p => Assert.Equal(90, p.Rotation.Value));

        action.Undo();
        Assert.All(composition.Sheets[0].Placements, p => Assert.Equal(0, p.Rotation.Value));
    }

    [Fact]
    public void NUp_ThenCompose_ProducesExpectedPageCount()
    {
        using var dir = new TempDir();
        Composition composition = CompositionWith(dir, 6);
        new NUpAction(composition, [0, 1, 2, 3, 4, 5], perSheet: 4).Do();

        string outPath = dir.File("out.pdf");
        new PdfComposer().Compose(composition, outPath);

        Assert.Equal(2, new PdfSharpDocumentService().Load(outPath).PageCount);
    }
}
