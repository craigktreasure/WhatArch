namespace WhatArch;

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
    /// <param name="path">The file path to resolve.</param>
    /// <param name="resolvedPath">The resolved absolute path if found.</param>
    /// <returns>True if the file was found, false otherwise.</returns>
    public static bool TryResolve(string path, out string? resolvedPath)
    {
        return TryResolve(path, Environment.GetEnvironmentVariable("PATH"), out resolvedPath);
    }

    /// <summary>
    /// Attempts to resolve a file path by searching:
    /// 1. As-is (absolute or relative to current directory)
    /// 2. PATH environment variable (for bare filenames only)
    /// </summary>
    /// <param name="path">The file path to resolve.</param>
    /// <param name="pathEnvironment">The PATH environment variable value.</param>
    /// <param name="resolvedPath">The resolved absolute path if found.</param>
    /// <returns>True if the file was found, false otherwise.</returns>
    public static bool TryResolve(string path, string? pathEnvironment, out string? resolvedPath)
    {
        ArgumentNullException.ThrowIfNull(path);

        // Try as-is (absolute or relative to current directory)
        if (File.Exists(path))
        {
            resolvedPath = Path.GetFullPath(path);
            return true;
        }

        // Only search PATH for bare filenames (no directory separators)
        if (ShouldSearchPath(path) && !string.IsNullOrEmpty(pathEnvironment))
        {
            foreach (string directory in pathEnvironment.Split(Path.PathSeparator))
            {
                if (string.IsNullOrWhiteSpace(directory))
                {
                    continue;
                }

                string fullPath = Path.Combine(directory, path);
                if (File.Exists(fullPath))
                {
                    resolvedPath = Path.GetFullPath(fullPath);
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
    internal static bool ShouldSearchPath(string path)
    {
        if (Path.IsPathRooted(path))
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
