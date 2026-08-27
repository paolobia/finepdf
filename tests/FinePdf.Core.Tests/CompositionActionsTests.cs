using FinePdf.Core.Models;
using FinePdf.Core.Operations;
using FinePdf.Core.Operations.Actions;
using FinePdf.Core.Services;
using FinePdf.Core.Tests.TestSupport;

namespace FinePdf.Core.Tests;

public class CompositionActionsTests
{
    private static SourceDocument LoadSample(TempDir dir, string name, int pages)
    {
        string path = PdfTestFactory.CreatePdf(dir.Path, name, pages);
        return new PdfSharpDocumentService().Load(path);
    }

    [Fact]
    public void AddFilePages_AddsOneSheetPerPage_UndoRemovesThem()
    {
        using var dir = new TempDir();
        SourceDocument source = LoadSample(dir, "doc.pdf", 3);
        var composition = new Composition();
        var stack = new UndoRedoStack();

        stack.Execute(new AddFilePagesAction(composition, source));

        Assert.Equal(3, composition.Sheets.Count);
        Assert.All(composition.Sheets, s => Assert.Single(s.Placements));
        Assert.Equal(source, composition.Sheets[0].Placements[0].Source);
        Assert.Equal(2, composition.Sheets[2].Placements[0].SourcePageIndex);

        stack.Undo();
        Assert.Empty(composition.Sheets);

        stack.Redo();
        Assert.Equal(3, composition.Sheets.Count);
    }

    [Fact]
    public void AddFilePages_InsertsAtRequestedIndex()
    {
        using var dir = new TempDir();
        SourceDocument first = LoadSample(dir, "first.pdf", 2);
        SourceDocument middle = LoadSample(dir, "middle.pdf", 1);
        var composition = new Composition();

        new AddFilePagesAction(composition, first).Do();
        new AddFilePagesAction(composition, middle, insertIndex: 1).Do();

        Assert.Equal(3, composition.Sheets.Count);
        Assert.Equal(middle, composition.Sheets[1].Placements[0].Source);
    }

    [Fact]
    public void MoveSheet_ReordersAndUndoes()
    {
        using var dir = new TempDir();
        SourceDocument source = LoadSample(dir, "doc.pdf", 4);
        var composition = new Composition();
        new AddFilePagesAction(composition, source).Do();

        var action = new MoveSheetAction(composition, fromIndex: 0, toIndex: 2);
        action.Do();

        Assert.Equal([1, 2, 0, 3], PageOrder(composition));

        action.Undo();
        Assert.Equal([0, 1, 2, 3], PageOrder(composition));
    }

    [Fact]
    public void DeleteSheets_RemovesMultiSelection_UndoRestoresOrder()
    {
        using var dir = new TempDir();
        SourceDocument source = LoadSample(dir, "doc.pdf", 5);
        var composition = new Composition();
        new AddFilePagesAction(composition, source).Do();

        var action = new DeleteSheetsAction(composition, [1, 3]);
        action.Do();

        Assert.Equal([0, 2, 4], PageOrder(composition));

        action.Undo();
        Assert.Equal([0, 1, 2, 3, 4], PageOrder(composition));
    }

    [Fact]
    public void DeleteSheets_EmptySelection_Throws()
    {
        var composition = new Composition();

        Assert.Throws<ArgumentException>(() => new DeleteSheetsAction(composition, []));
    }

    private static int[] PageOrder(Composition composition) =>
        [.. composition.Sheets.Select(s => s.Placements[0].SourcePageIndex)];
}
