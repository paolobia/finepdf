using System.ComponentModel;
using FinePdf.Core.Models;

namespace FinePdf.Core.Services;

/// <summary>
/// CUPS-based printing for Linux: <c>lpstat</c> enumerates destinations, <c>lp</c> submits the
/// job. The composition is first written to a temporary PDF via <see cref="IPdfComposer"/> so
/// what prints is exactly what the preview showed. All process calls go through
/// <see cref="IProcessRunner"/> for testability.
/// </summary>
public sealed class LinuxCupsPrintService : IPrintService
{
    private readonly IProcessRunner _runner;
    private readonly IPdfComposer _composer;

    public LinuxCupsPrintService(IProcessRunner runner, IPdfComposer composer)
    {
        ArgumentNullException.ThrowIfNull(runner);
        ArgumentNullException.ThrowIfNull(composer);
        _runner = runner;
        _composer = composer;
    }

    /// <inheritdoc />
    public IReadOnlyList<PrinterInfo> GetPrinters()
    {
        ProcessResult list = RunOrThrow("lpstat", ["-e"], "Impossibile elencare le stampanti.");

        string[] names = list.StandardOutput
            .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        string? defaultName = TryGetDefaultPrinter();

        return [.. names.Select(n => new PrinterInfo(n, string.Equals(n, defaultName, StringComparison.Ordinal)))];
    }

    /// <inheritdoc />
    public void Print(Composition composition, PrintOptions options)
    {
        ArgumentNullException.ThrowIfNull(composition);
        ArgumentNullException.ThrowIfNull(options);
        options.Validated();

        if (!options.Range.IsAll && options.Range.Resolve(composition.Sheets.Count).Count == 0)
        {
            throw new PrintException("L'intervallo indicato non seleziona alcun foglio.");
        }

        string tempPath = Path.Combine(Path.GetTempPath(), $"finepdf-{Guid.NewGuid():N}.pdf");
        try
        {
            _composer.Compose(composition, tempPath);

            var args = new List<string>
            {
                "-d", options.PrinterName,
                "-n", options.Copies.ToString(System.Globalization.CultureInfo.InvariantCulture),
                "-o", $"sides={SidesOption(options.Duplex)}",
            };

            if (!options.Range.IsAll)
            {
                args.Add("-o");
                args.Add($"page-ranges={options.Range.Text}");
            }

            foreach (string raw in options.RawOptions)
            {
                string trimmed = raw.Trim();
                if (trimmed.Length > 0)
                {
                    args.Add("-o");
                    args.Add(trimmed);
                }
            }

            args.Add(tempPath);

            ProcessResult job = RunOrThrow("lp", args, "Invio alla stampante non riuscito.");
            if (!job.Succeeded)
            {
                throw new PrintException(Describe("Invio alla stampante non riuscito.", job));
            }
        }
        finally
        {
            TryDelete(tempPath);
        }
    }

    private static string SidesOption(DuplexMode duplex) => duplex switch
    {
        DuplexMode.LongEdge => "two-sided-long-edge",
        DuplexMode.ShortEdge => "two-sided-short-edge",
        _ => "one-sided",
    };

    private string? TryGetDefaultPrinter()
    {
        try
        {
            ProcessResult result = _runner.Run("lpstat", ["-d"]);

            // "system default destination: <name>" | "no system default destination"
            int colon = result.StandardOutput.IndexOf(':', StringComparison.Ordinal);
            if (colon < 0)
            {
                return null;
            }

            string name = result.StandardOutput[(colon + 1)..].Trim();
            return name.Length > 0 ? name : null;
        }
        catch (Win32Exception)
        {
            return null;
        }
    }

    private ProcessResult RunOrThrow(string tool, IReadOnlyList<string> args, string failure)
    {
        try
        {
            ProcessResult result = _runner.Run(tool, args);
            if (!result.Succeeded && tool == "lpstat")
            {
                throw new PrintException(Describe(failure, result));
            }

            return result;
        }
        catch (Win32Exception ex)
        {
            throw new PrintException($"CUPS non disponibile: comando «{tool}» non trovato.", ex);
        }
    }

    private static string Describe(string prefix, ProcessResult result)
    {
        string detail = string.IsNullOrWhiteSpace(result.StandardError)
            ? result.StandardOutput.Trim()
            : result.StandardError.Trim();
        return string.IsNullOrEmpty(detail) ? prefix : $"{prefix} {detail}";
    }

    private static void TryDelete(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch (IOException)
        {
            // A transient lock from the spooler; the OS will clean temp eventually.
        }
    }
}
