using System.ComponentModel;
using FinePdf.Core.Services;

namespace FinePdf.Core.Tests.TestSupport;

/// <summary>
/// Records every <see cref="Run"/> invocation and replays canned <see cref="ProcessResult"/>s,
/// matched by executable name and (optionally) a required argument, so the CUPS print service
/// can be tested without a real spooler.
/// </summary>
internal sealed class FakeProcessRunner : IProcessRunner
{
    private readonly List<(string FileName, string? WhenArg, ProcessResult Result)> _responses = [];
    private readonly HashSet<string> _missing = new(StringComparer.Ordinal);

    public List<(string FileName, string[] Arguments)> Calls { get; } = [];

    public FakeProcessRunner Returns(
        string fileName,
        int exitCode,
        string stdout = "",
        string stderr = "",
        string? whenArg = null)
    {
        _responses.Add((fileName, whenArg, new ProcessResult(exitCode, stdout, stderr)));
        return this;
    }

    public FakeProcessRunner NotInstalled(string fileName)
    {
        _missing.Add(fileName);
        return this;
    }

    public ProcessResult Run(string fileName, IReadOnlyList<string> arguments, CancellationToken cancellationToken = default)
    {
        Calls.Add((fileName, [.. arguments]));

        if (_missing.Contains(fileName))
        {
            throw new Win32Exception(2, $"{fileName}: not found");
        }

        foreach ((string name, string? whenArg, ProcessResult result) in _responses)
        {
            if (name == fileName && (whenArg is null || arguments.Contains(whenArg)))
            {
                return result;
            }
        }

        return new ProcessResult(0, string.Empty, string.Empty);
    }

    public string[] ArgsFor(string fileName) =>
        Calls.LastOrDefault(c => c.FileName == fileName).Arguments ?? [];
}
