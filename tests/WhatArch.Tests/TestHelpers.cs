namespace WhatArch.Tests;

/// <summary>
/// Shared test utilities and helpers.
/// </summary>
internal static class TestHelpers
{
    /// <summary>
    /// Gets the path to the test binaries directory.
    /// </summary>
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
