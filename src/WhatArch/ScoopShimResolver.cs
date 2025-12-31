namespace WhatArch;

using System.Diagnostics.CodeAnalysis;
using System.IO.Abstractions;

/// <summary>
/// Resolves Scoop shim files to find the target executable.
/// </summary>
internal static class ScoopShimResolver
{
    /// <summary>
    /// Result of shim resolution.
    /// </summary>
    /// <param name="IsShim">True if the file has a corresponding .shim file.</param>
    /// <param name="TargetPath">The path to the target executable, or null if not a shim or parsing failed.</param>
    [ExcludeFromCodeCoverage(Justification = "Code coverage improperly calculated for records constructors and parameters.")]
    internal record ShimResult(bool IsShim, string? TargetPath)
    {
        /// <summary>
        /// Indicates whether a shim should be followed to its target executable.
        /// </summary>
        [MemberNotNullWhen(true, nameof(TargetPath))]
        public bool FollowShimToTarget => IsShim && TargetPath is not null;

        /// <summary>
        /// Indicates that no shim file was found.
        /// </summary>
        public static ShimResult NoShim => new(false, null);

        /// <summary>
        /// Indicates that a shim file was found but the target executable could not be determined.
        /// </summary>
        public static ShimResult ShimWithoutTarget => new(true, null);

        /// <summary>
        /// Indicates that a shim file was found and the target executable was determined.
        /// </summary>
        /// <param name="targetPath">The path to the target executable.</param>
        /// <returns>A ShimResult representing a shim with a target.</returns>
        public static ShimResult ShimWithTarget(string targetPath) => new(true, targetPath);
    }

    /// <summary>
    /// Attempts to resolve a shim file to find the target executable.
    /// </summary>
    /// <param name="fileSystem">The file system abstraction.</param>
    /// <param name="executablePath">The path to the executable to check.</param>
    /// <returns>A result indicating whether a shim was found and the target path.</returns>
    public static ShimResult TryResolveShim(IFileSystem fileSystem, string executablePath)
    {
        ArgumentNullException.ThrowIfNull(fileSystem);
        ArgumentNullException.ThrowIfNull(executablePath);

        // Check for a corresponding .shim file
        string shimPath = fileSystem.Path.ChangeExtension(executablePath, ".shim");

        if (!fileSystem.File.Exists(shimPath))
        {
            return ShimResult.NoShim;
        }

        // Parse the .shim file
        string? targetPath = ParseShimFile(fileSystem, shimPath);

        if (targetPath is null || !fileSystem.File.Exists(targetPath))
        {
            return ShimResult.ShimWithoutTarget;
        }

        return ShimResult.ShimWithTarget(targetPath);
    }

    /// <summary>
    /// Parses a .shim file to extract the target path.
    /// </summary>
    private static string? ParseShimFile(IFileSystem fileSystem, string shimPath)
    {
        try
        {
            foreach (string line in fileSystem.File.ReadLines(shimPath))
            {
                ReadOnlySpan<char> span = line.AsSpan().Trim();

                // Look for "path = <value>" format
                if (span.StartsWith("path", StringComparison.OrdinalIgnoreCase))
                {
                    int equalsIndex = span.IndexOf('=');
                    if (equalsIndex > 0)
                    {
                        ReadOnlySpan<char> pathValue = span[(equalsIndex + 1)..].Trim();

                        // Remove surrounding quotes if present
                        if (pathValue.Length >= 2 &&
                            ((pathValue[0] == '"' && pathValue[^1] == '"') ||
                             (pathValue[0] == '\'' && pathValue[^1] == '\'')))
                        {
                            pathValue = pathValue[1..^1];
                        }

                        if (!pathValue.IsEmpty && !pathValue.IsWhiteSpace())
                        {
                            return pathValue.ToString();
                        }
                    }
                }
            }
        }
#pragma warning disable CA1031 // Do not catch general exception types
        catch
#pragma warning restore CA1031 // Do not catch general exception types
        {
            // If we can't read the file, treat it as no shim found
        }

        return null;
    }
}
