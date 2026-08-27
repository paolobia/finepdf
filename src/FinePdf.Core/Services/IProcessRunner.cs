namespace FinePdf.Core.Services;

/// <summary>The captured result of running an external process to completion.</summary>
public readonly record struct ProcessResult(int ExitCode, string StandardOutput, string StandardError)
{
    public bool Succeeded => ExitCode == 0;
}

/// <summary>
/// Runs a child process and captures its output. Abstracted so the CUPS-based print service
/// can be unit-tested without a real <c>lp</c>/<c>lpstat</c> on the box.
/// </summary>
public interface IProcessRunner
{
    /// <exception cref="System.ComponentModel.Win32Exception">The executable was not found.</exception>
    ProcessResult Run(string fileName, IReadOnlyList<string> arguments, CancellationToken cancellationToken = default);
}
