namespace WhatArch;

using System.Diagnostics.CodeAnalysis;
using System.IO.Abstractions;
using WhatArch.Abstractions;

/// <summary>
/// Resolves file paths by searching the current directory and PATH environment variable.
/// </summary>
internal static class FileResolver
{
    /// <summary>
    /// Attempts to resolve a file path by searching:
    /// 1. As-is (absolute or relative to current directory)
    /// 2. PATH environment variable (for bare filenames only)
    /// </summary>
    /// <param name="fileSystem">The file system abstraction.</param>
    /// <param name="path">The file path to resolve.</param>
    /// <param name="environmentVariableProvider">The environment variable provider.</param>
    /// <param name="resolvedPath">The resolved absolute path if found.</param>
    /// <returns>True if the file was found, false otherwise.</returns>
    public static bool TryResolve(
        IFileSystem fileSystem,
        string path,
        IEnvironmentVariableProvider environmentVariableProvider,
        [NotNullWhen(true)] out string? resolvedPath)
    {
        ArgumentNullException.ThrowIfNull(fileSystem);
        ArgumentNullException.ThrowIfNull(path);
        ArgumentNullException.ThrowIfNull(environmentVariableProvider);

        string? pathEnvironment = environmentVariableProvider.GetEnvironmentVariable("PATH");

        // Try as-is (absolute or relative to current directory)
        if (fileSystem.File.Exists(path))
        {
            resolvedPath = fileSystem.Path.GetFullPath(path);
            return true;
        }

        // Only search PATH for bare filenames (no directory separators)
        if (ShouldSearchPath(fileSystem, path) && !string.IsNullOrEmpty(pathEnvironment))
        {
            foreach (string directory in pathEnvironment.Split(Path.PathSeparator))
            {
                if (string.IsNullOrWhiteSpace(directory))
                {
                    continue;
                }

                string fullPath = fileSystem.Path.Combine(directory, path);
                if (fileSystem.File.Exists(fullPath))
                {
                    resolvedPath = fileSystem.Path.GetFullPath(fullPath);
                    return true;
                }
            }
        }

        resolvedPath = null;
        return false;
    }

    /// <summary>
    /// Determines if PATH should be searched for the given path.
    /// Only bare filenames (no directory separators or rooted paths) should trigger PATH search.
    /// </summary>
    /// <param name="fileSystem">The file system abstraction.</param>
    /// <param name="path">The file path to check.</param>
    internal static bool ShouldSearchPath(IFileSystem fileSystem, string path)
    {
        if (fileSystem.Path.IsPathRooted(path))
        {
            return false;
        }

        if (path.Contains(Path.DirectorySeparatorChar, StringComparison.Ordinal))
        {
            return false;
        }

        if (path.Contains(Path.AltDirectorySeparatorChar, StringComparison.Ordinal))
        {
            return false;
        }

        return true;
    }
}
