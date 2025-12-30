namespace WhatArch.Tests;

using System.Diagnostics.CodeAnalysis;

/// <summary>
/// Shared test utilities and helpers.
/// </summary>
internal static class TestHelpers
{
    /// <summary>
    /// Gets the path to the test binaries directory.
    /// </summary>
    [SuppressMessage("System.IO.Abstractions", "IO0006:Replace Path class with IFileSystem.Path for improved testability", Justification = "<Pending>")]
    [SuppressMessage("System.IO.Abstractions", "IO0003:Replace Directory class with IFileSystem.Directory for improved testability", Justification = "<Pending>")]
    public static string GetTestBinariesPath()
    {
        string? currentDir = AppContext.BaseDirectory;

        while (currentDir != null)
        {
            if (Directory.Exists(Path.Combine(currentDir, ".git")))
            {
                return Path.Combine(currentDir, "tests", "assets", "binaries");
            }

            currentDir = Path.GetDirectoryName(currentDir);
        }

        throw new InvalidOperationException("Could not locate the root directory of the repository.");
    }
}
