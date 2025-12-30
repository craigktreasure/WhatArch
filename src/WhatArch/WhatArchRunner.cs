namespace WhatArch;

using System.IO.Abstractions;

/// <summary>
/// Core application logic for WhatArch, exposed for testability.
/// </summary>
internal static class WhatArchRunner
{
    /// <summary>
    /// Result of running the WhatArch analysis.
    /// </summary>
    /// <param name="ExitCode">The exit code (0 for success, 1 for error).</param>
    /// <param name="Output">The output message (architecture or null on error).</param>
    /// <param name="Error">The error message (null on success).</param>
    internal record RunResult(int ExitCode, string? Output, string? Error);

    /// <summary>
    /// Analyzes a binary file and returns its architecture.
    /// </summary>
    /// <param name="fileSystem">The file system abstraction.</param>
    /// <param name="path">The path to the binary file.</param>
    /// <returns>A result containing the exit code, output, and any error message.</returns>
    public static RunResult Run(IFileSystem fileSystem, string path)
    {
        if (!FileResolver.TryResolve(fileSystem, path, out string? resolvedPath))
        {
            string error = FileResolver.ShouldSearchPath(fileSystem, path)
                ? $"Error: File not found: {path} (searched current directory and PATH)"
                : $"Error: File not found: {path}";
            return new RunResult(1, null, error);
        }

        try
        {
            string architecture = PeArchitectureReader.GetArchitecture(fileSystem, resolvedPath!);
            return new RunResult(0, architecture, null);
        }
#pragma warning disable CA1031 // Do not catch general exception types
        catch (Exception ex)
#pragma warning restore CA1031 // Do not catch general exception types
        {
            return new RunResult(1, null, $"Error: {ex.Message}");
        }
    }
}
