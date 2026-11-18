namespace WhatArch;

using System.Diagnostics.CodeAnalysis;
using System.IO.Abstractions;
using WhatArch.Abstractions;

/// <summary>
/// Core application logic for WhatArch, exposed for testability.
/// </summary>
internal static class WhatArchRunner
{
    /// <summary>
    /// Result of running the WhatArch analysis.
    /// </summary>
    /// <param name="ExitCode">The exit code (0 for success, 1 for error).</param>
    /// <param name="Architecture">The architecture of the binary, or of a shim's target (null on error).</param>
    /// <param name="ShimLabel">The shim's own label, such as "x86" or ".cmd" (null when not a shim).</param>
    /// <param name="Error">The error message (null on success).</param>
    [ExcludeFromCodeCoverage(Justification = "Code coverage improperly calculated for records constructors and parameters.")]
    internal record RunResult(int ExitCode, string? Architecture, string? ShimLabel, string? Error)
    {
        /// <summary>
        /// Creates a successful run result.
        /// </summary>
        /// <param name="architecture">The architecture of the binary, or of a shim's target.</param>
        /// <param name="shimLabel">The shim's own label, or null when not a shim.</param>
        /// <returns>A <see cref="RunResult"/> representing success.</returns>
        public static RunResult ForSuccess(string architecture, string? shimLabel = null) => new(0, architecture, shimLabel, null);

        /// <summary>
        /// Creates an error run result.
        /// </summary>
        /// <param name="error">The error message.</param>
        /// <returns>A <see cref="RunResult"/> representing an error.</returns>
        public static RunResult ForError(string error) => new(1, null, null, error);

        /// <summary>
        /// Formats the result for display.
        /// </summary>
        /// <param name="shortOutput">When true, a shim reports only its target's architecture.</param>
        /// <returns>The text to print, or null on error.</returns>
        public string? FormatOutput(bool shortOutput)
            => ShimLabel is null || shortOutput ? Architecture : $"{ShimLabel} (shim) -> {Architecture}";
    }

    /// <summary>
    /// Analyzes a binary file and returns its architecture.
    /// </summary>
    /// <param name="fileSystem">The file system abstraction.</param>
    /// <param name="path">The path to the binary file.</param>
    /// <param name="environmentVariableProvider">The environment variable provider.</param>
    /// <returns>A result containing the exit code, architecture, shim details, and any error message.</returns>
    public static RunResult Run(IFileSystem fileSystem, string path, IEnvironmentVariableProvider environmentVariableProvider)
    {
        if (!FileResolver.TryResolve(fileSystem, path, environmentVariableProvider, out string? resolvedPath))
        {
            string error = FileResolver.ShouldSearchPath(fileSystem, path)
                ? $"Error: File not found: {path} (searched current directory and PATH)"
                : $"Error: File not found: {path}";
            return RunResult.ForError(error);
        }

        try
        {
            if (DotNetToolShimResolver.IsCmdFile(fileSystem, resolvedPath))
            {
                string? cmdTargetPath = DotNetToolShimResolver.ParseTargetPath(fileSystem, resolvedPath);
                if (cmdTargetPath is null)
                {
                    return RunResult.ForError($"Error: {resolvedPath} is a batch script, not a binary, and is not a recognized .NET tool shim");
                }

                if (!fileSystem.File.Exists(cmdTargetPath))
                {
                    return RunResult.ForError($"Error: .NET tool shim target not found: {cmdTargetPath}");
                }

                string cmdTargetArchitecture = PeArchitectureReader.GetArchitecture(fileSystem, cmdTargetPath);
                return RunResult.ForSuccess(cmdTargetArchitecture, shimLabel: ".cmd");
            }

            string architecture = PeArchitectureReader.GetArchitecture(fileSystem, resolvedPath);

            // Check for Scoop shim
            ScoopShimResolver.ShimResult shimResult = ScoopShimResolver.TryResolveShim(fileSystem, resolvedPath);

            if (shimResult.FollowShimToTarget)
            {
                string shimTargetArchitecture = PeArchitectureReader.GetArchitecture(fileSystem, shimResult.TargetPath);
                return RunResult.ForSuccess(shimTargetArchitecture, shimLabel: architecture);
            }

            return RunResult.ForSuccess(architecture);
        }
#pragma warning disable CA1031 // Do not catch general exception types
        catch (Exception ex)
#pragma warning restore CA1031 // Do not catch general exception types
        {
            return RunResult.ForError($"Error: {ex.Message}");
        }
    }
}
