using FinePdf.Core.Models;
using FinePdf.Core.Operations.Actions;
using FinePdf.Core.Services;
using FinePdf.Core.Tests.TestSupport;

namespace FinePdf.Core.Tests;

public class SheetOperationsTests
{
    private static SourceDocument Sample(TempDir dir, string name, int pages) =>
        new PdfSharpDocumentService().Load(PdfTestFactory.CreatePdf(dir.Path, name, pages));

    private static Composition CompositionWith(TempDir dir, int pages)
    {
        var composition = new Composition();
        new AddFilePagesAction(composition, Sample(dir, "doc.pdf", pages)).Do();
        return composition;
    }

    [Fact]
    public void RotateSheet_RotatesSinglePlacement_AndUndoes()
    {
        using var dir = new TempDir();
        Composition composition = CompositionWith(dir, 1);

        var action = new RotateSheetAction(composition.Sheets[0], 90);
        action.Do();
        Assert.Equal(90, composition.Sheets[0].Placements[0].Rotation.Value);

        action.Undo();
        Assert.Equal(0, composition.Sheets[0].Placements[0].Rotation.Value);
    }

    [Fact]
    public void RotateSheet_180Twice_ReturnsToZero()
    {
        using var dir = new TempDir();
        Composition composition = CompositionWith(dir, 1);

        new RotateSheetAction(composition.Sheets[0], 180).Do();
        new RotateSheetAction(composition.Sheets[0], 180).Do();

        Assert.Equal(0, composition.Sheets[0].Placements[0].Rotation.Value);
    }

    [Fact]
    public void RotateSheet_AppliesToEveryPlacementOnAnNUpSheet()
    {
        using var dir = new TempDir();
        Composition composition = CompositionWith(dir, 2);
        new MergeSheetsAction(composition, [0, 1]).Do();

        new RotateSheetAction(composition.Sheets[0], 90).Do();

        Assert.All(composition.Sheets[0].Placements, p => Assert.Equal(90, p.Rotation.Value));
    }

    [Fact]
    public void RotateSheet_BlankSheet_Throws()
    {
        var composition = new Composition();
        composition.AddSheet(new Sheet());

        Assert.Throws<InvalidOperationException>(() => new RotateSheetAction(composition.Sheets[0], 90));
    }

    [Fact]
    public void InsertBlankSheet_AddsBlank_AndUndoRemoves()
    {
        using var dir = new TempDir();
        Composition composition = CompositionWith(dir, 2);

        var action = new InsertBlankSheetAction(composition, index: 1);
        action.Do();

        Assert.Equal(3, composition.Sheets.Count);
        Assert.True(composition.Sheets[1].IsBlank);

        action.Undo();
        Assert.Equal(2, composition.Sheets.Count);
        Assert.DoesNotContain(composition.Sheets, s => s.IsBlank);
    }

    [Fact]
    public void MergeSheets_CombinesAllPlacements_AndUndoRestoresOrder()
    {
        using var dir = new TempDir();
        Composition composition = CompositionWith(dir, 4);

        var action = new MergeSheetsAction(composition, [0, 1, 2, 3]);
        action.Do();

        Assert.Single(composition.Sheets);
        Assert.Equal(4, composition.Sheets[0].Placements.Count);
        Assert.Equal(new[] { 0, 1, 2, 3 }, composition.Sheets[0].Placements.Select(p => p.SourcePageIndex));

        action.Undo();
        Assert.Equal(4, composition.Sheets.Count);
        Assert.Equal(new[] { 0, 1, 2, 3 }, composition.Sheets.Select(s => s.Placements[0].SourcePageIndex));
    }

    [Fact]
    public void MergeSheets_PartialSelection_KeepsOtherSheetsInPlace()
    {
        using var dir = new TempDir();
        Composition composition = CompositionWith(dir, 4);

        new MergeSheetsAction(composition, [1, 2]).Do();

        Assert.Equal(3, composition.Sheets.Count);
        Assert.Single(composition.Sheets[0].Placements);
        Assert.Equal(2, composition.Sheets[1].Placements.Count);
        Assert.Equal(3, composition.Sheets[2].Placements[0].SourcePageIndex);
    }

    [Fact]
    public void MergeSheets_FewerThanTwo_Throws()
    {
        var composition = new Composition();

        Assert.Throws<ArgumentException>(() => new MergeSheetsAction(composition, [0]));
    }

    [Fact]
    public void SetSheetFitMode_AppliesOverride_AndUndoClears()
    {
        using var dir = new TempDir();
        Composition composition = CompositionWith(dir, 3);

        var action = new SetSheetFitModeAction([composition.Sheets[0], composition.Sheets[2]], FitMode.Fill);
        action.Do();

        Assert.Equal(FitMode.Fill, composition.Sheets[0].FitModeOverride);
        Assert.Null(composition.Sheets[1].FitModeOverride);
        Assert.Equal(FitMode.Fill, composition.Sheets[2].FitModeOverride);

        action.Undo();
        Assert.Null(composition.Sheets[0].FitModeOverride);
        Assert.Null(composition.Sheets[2].FitModeOverride);
    }

    [Fact]
    public void SetPaper_ChangesSizeAndOrientation_AndUndoes()
    {
        var composition = new Composition();

        var action = new SetPaperAction(composition, PaperSize.A3, PageOrientation.Landscape);
        action.Do();

        Assert.Equal("A3", composition.PaperSize.Name);
        Assert.Equal(PageOrientation.Landscape, composition.Orientation);
        Assert.Equal(PaperSize.A3.HeightPoints, composition.SheetSizePoints.Width, 1);

        action.Undo();
        Assert.Equal("A4", composition.PaperSize.Name);
        Assert.Equal(PageOrientation.Portrait, composition.Orientation);
    }

    [Fact]
    public void SetMargins_ChangesMargins_AndUndoes()
    {
        var composition = new Composition();
        Assert.True(composition.Margins.IsZero);

        var action = new SetMarginsAction(composition, PageMargins.FromMillimeters(12));
        action.Do();
        Assert.Equal(12, composition.Margins.MaxEdgeMillimeters, 1);

        action.Undo();
        Assert.True(composition.Margins.IsZero);
    }

    [Fact]
    public void SetDefaultFitMode_ChangesDefault_AndUndoes()
    {
        var composition = new Composition();

        var action = new SetDefaultFitModeAction(composition, FitMode.ActualSize);
        action.Do();
        Assert.Equal(FitMode.ActualSize, composition.DefaultFitMode);

        action.Undo();
        Assert.Equal(FitMode.Fit, composition.DefaultFitMode);
    }

    [Fact]
    public void MergedSheet_ComposesToOnePagePerSheet()
    {
        using var dir = new TempDir();
        Composition composition = CompositionWith(dir, 4);
        new MergeSheetsAction(composition, [0, 1]).Do();

        string outPath = dir.File("out.pdf");
        new PdfComposer().Compose(composition, outPath);

        Assert.Equal(3, new PdfSharpDocumentService().Load(outPath).PageCount);
    }
}
