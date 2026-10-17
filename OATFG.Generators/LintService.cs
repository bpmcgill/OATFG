using System.ComponentModel;
using System.Diagnostics;

namespace OATFG.Generators;

public enum LintTarget
{
    Python,
    JavaScript,
    Robot
}

public sealed record LintResult(string FilePath, string Language, bool ToolAvailable, bool Passed, string Output);

/// <summary>
/// Syntax-lints generated suites with the toolchain for each language:
/// Python → `python -m py_compile`, JavaScript → `node --check`, Robot Framework → `robot --dryrun`
/// (verifies test data syntax and library imports; the --lint option was removed in Robot Framework 7,
/// with a `python -m robot --dryrun` fallback for user-site installs). Reports a missing tool
/// instead of crashing.
/// </summary>
public static class LintService
{
    public static async Task<LintResult> LintAsync(string filePath, LintTarget target)
    {
        (string tool, string arguments) = CommandFor(target, filePath);
        (int? exitCode, string output) = await TryRunAsync(tool, arguments);

        if (exitCode is null)
        {
            return new LintResult(filePath, target.ToString(), ToolAvailable: false, Passed: false, "tool not installed");
        }

        return new LintResult(filePath, target.ToString(), ToolAvailable: true, Passed: exitCode == 0, output);
    }

    /// <summary>Probes whether the toolchain for a target can be started (used to skip tests cleanly).</summary>
    public static bool IsToolAvailable(LintTarget target) => target switch
    {
        LintTarget.Python => Probe("python", "--version"),
        LintTarget.JavaScript => Probe("node", "--version"),
        // robot is on PATH for regular installs; pip --user installs need `python -m robot`
        LintTarget.Robot => Probe("robot", "--version") || Probe("python", "-m robot --version"),
        _ => false
    };

    private static (string Tool, string Arguments) CommandFor(LintTarget target, string filePath) => target switch
    {
        LintTarget.Python => ("python", $"-m py_compile \"{filePath}\""),
        LintTarget.JavaScript => ("node", $"--check \"{filePath}\""),
        LintTarget.Robot when Probe("robot", "--version") => ("robot", $"--dryrun \"{filePath}\""),
        LintTarget.Robot => ("python", $"-m robot --dryrun \"{filePath}\""),
        _ => throw new ArgumentOutOfRangeException(nameof(target))
    };

    private static bool Probe(string tool, string arguments)
    {
        try
        {
            (int? exitCode, _) = TryRunAsync(tool, arguments).Result;
            return exitCode is not null;
        }
        catch
        {
            return false;
        }
    }

    private static async Task<(int? ExitCode, string Output)> TryRunAsync(string tool, string arguments)
    {
        try
        {
            using var process = new Process
            {
                StartInfo = new ProcessStartInfo(tool, arguments)
                {
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                }
            };
            process.Start();

            string stdout = await process.StandardOutput.ReadToEndAsync();
            string stderr = await process.StandardError.ReadToEndAsync();
            await process.WaitForExitAsync();

            return (process.ExitCode, (stdout + Environment.NewLine + stderr).Trim());
        }
        catch (Win32Exception)
        {
            // executable not found
            return (null, string.Empty);
        }
    }
}