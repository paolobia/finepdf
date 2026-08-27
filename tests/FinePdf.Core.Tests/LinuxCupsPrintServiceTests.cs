using FinePdf;
using FinePdf.Core.Models;
using FinePdf.Core.Operations.Actions;
using FinePdf.Core.Services;
using FinePdf.Core.Tests.TestSupport;

namespace FinePdf.Core.Tests;

public class LinuxCupsPrintServiceTests
{
    private static Composition CompositionWith(TempDir dir, int pages)
    {
        var composition = new Composition();
        SourceDocument doc = new PdfSharpDocumentService().Load(PdfTestFactory.CreatePdf(dir.Path, "doc.pdf", pages));
        new AddFilePagesAction(composition, doc).Do();
        return composition;
    }

    [Fact]
    public void GetPrinters_ParsesLpstat_AndFlagsDefault()
    {
        var runner = new FakeProcessRunner()
            .Returns("lpstat", 0, stdout: "Office\nHomeInk\n", whenArg: "-e")
            .Returns("lpstat", 0, stdout: "system default destination: HomeInk\n", whenArg: "-d");
        var service = new LinuxCupsPrintService(runner, new PdfComposer());

        IReadOnlyList<PrinterInfo> printers = service.GetPrinters();

        Assert.Equal(new[] { "Office", "HomeInk" }, printers.Select(p => p.Name));
        Assert.Equal("HomeInk", printers.Single(p => p.IsDefault).Name);
    }

    [Fact]
    public void GetPrinters_WhenLpstatMissing_ThrowsPrintException()
    {
        var runner = new FakeProcessRunner().NotInstalled("lpstat");
        var service = new LinuxCupsPrintService(runner, new PdfComposer());

        Assert.Throws<PrintException>(() => service.GetPrinters());
    }

    [Fact]
    public void Print_BuildsLpCommandLine_WithCopiesDuplexAndRange()
    {
        using var dir = new TempDir();
        Composition composition = CompositionWith(dir, 6);
        var runner = new FakeProcessRunner().Returns("lp", 0, stdout: "request id is HomeInk-42");
        var service = new LinuxCupsPrintService(runner, new PdfComposer());

        service.Print(composition, new PrintOptions("HomeInk")
        {
            Copies = 3,
            Duplex = DuplexMode.LongEdge,
            Range = PageRange.Parse("1-3,5"),
        });

        string[] args = runner.ArgsFor("lp");
        Assert.Equal("-d", args[0]);
        Assert.Equal("HomeInk", args[1]);
        Assert.Contains("-n", args);
        Assert.Equal("3", args[Array.IndexOf(args, "-n") + 1]);
        Assert.Contains("sides=two-sided-long-edge", args);
        Assert.Contains("page-ranges=1-3,5", args);
        Assert.True(File.Exists(args[^1]) is false, "the temp PDF must be deleted after printing");
    }

    [Fact]
    public void Print_SimplexAllPages_OmitsPageRanges()
    {
        using var dir = new TempDir();
        Composition composition = CompositionWith(dir, 2);
        var runner = new FakeProcessRunner().Returns("lp", 0);
        var service = new LinuxCupsPrintService(runner, new PdfComposer());

        service.Print(composition, new PrintOptions("HomeInk"));

        string[] args = runner.ArgsFor("lp");
        Assert.Contains("sides=one-sided", args);
        Assert.DoesNotContain(args, a => a.StartsWith("page-ranges=", StringComparison.Ordinal));
    }

    [Fact]
    public void Print_WhenLpFails_ThrowsPrintExceptionWithStderr()
    {
        using var dir = new TempDir();
        Composition composition = CompositionWith(dir, 1);
        var runner = new FakeProcessRunner().Returns("lp", 1, stderr: "lp: The printer or class does not exist.");
        var service = new LinuxCupsPrintService(runner, new PdfComposer());

        PrintException ex = Assert.Throws<PrintException>(
            () => service.Print(composition, new PrintOptions("Ghost")));
        Assert.Contains("does not exist", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Print_RangeSelectsNoSheet_ThrowsBeforeSpooling()
    {
        using var dir = new TempDir();
        Composition composition = CompositionWith(dir, 3);
        var runner = new FakeProcessRunner().Returns("lp", 0);
        var service = new LinuxCupsPrintService(runner, new PdfComposer());

        Assert.Throws<PrintException>(
            () => service.Print(composition, new PrintOptions("HomeInk") { Range = PageRange.Parse("90-99") }));
        Assert.DoesNotContain(runner.Calls, c => c.FileName == "lp");
    }

    [Fact]
    public void Print_ZeroCopies_ThrowsArgumentException()
    {
        using var dir = new TempDir();
        Composition composition = CompositionWith(dir, 1);
        var service = new LinuxCupsPrintService(new FakeProcessRunner(), new PdfComposer());

        Assert.Throws<ArgumentException>(
            () => service.Print(composition, new PrintOptions("HomeInk") { Copies = 0 }));
    }
}
