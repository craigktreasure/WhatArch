namespace WhatArch;

using System.IO.Abstractions;
using System.Text.RegularExpressions;

/// <summary>
/// Resolves the .cmd shims the .NET SDK creates for RID-specific (for example, Native AOT) tools.
/// </summary>
/// <remarks>
/// The SDK writes a two-line batch file into the tools directory:
/// <code>
/// @echo off
/// "%~dp0.store\tool\1.0.0\tool.win-arm64\1.0.0\tools\any\win-arm64\Tool.exe" %*
/// </code>
/// Batch files can do anything, so only this exact shape is accepted. Anything else
/// (a changed SDK format, a hand-written script, a tampered file) is reported as unrecognized
/// rather than guessed at. The target is only ever read, never executed, and must resolve
/// to an .exe inside the shim's own directory.
/// </remarks>
internal static partial class DotNetToolShimResolver
{
    // The real shim is ~150 bytes. Anything much larger is not a shim; don't read it.
    private const int MaxShimLength = 4096;

    /// <summary>
    /// Determines whether the path is a batch script this resolver handles.
    /// </summary>
    /// <param name="fileSystem">The file system abstraction.</param>
    /// <param name="path">The file path to check.</param>
    /// <returns>True if the file has a .cmd extension.</returns>
    public static bool IsCmdFile(IFileSystem fileSystem, string path)
        => string.Equals(fileSystem.Path.GetExtension(path), ".cmd", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Parses a .NET tool .cmd shim to find its target executable.
    /// </summary>
    /// <param name="fileSystem">The file system abstraction.</param>
    /// <param name="cmdPath">The full path to the .cmd file.</param>
    /// <returns>
    /// The full path to the target executable, or null if the file is not a recognized shim.
    /// The target is not checked for existence, so callers can report a broken shim distinctly.
    /// </returns>
    /// <exception cref="IOException">The .cmd file could not be read.</exception>
    public static string? ParseTargetPath(IFileSystem fileSystem, string cmdPath)
    {
        ArgumentNullException.ThrowIfNull(fileSystem);
        ArgumentNullException.ThrowIfNull(cmdPath);

        // Read errors propagate so the caller reports the real cause, not "unrecognized shim".
        if (fileSystem.FileInfo.New(cmdPath).Length > MaxShimLength)
        {
            return null;
        }

        string content = fileSystem.File.ReadAllText(cmdPath);
        string[] lines = content.Split('\n', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if (lines.Length != 2 || !string.Equals(lines[0], "@echo off", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        Match match = ShimLineRegex().Match(lines[1]);
        if (!match.Success)
        {
            return null;
        }

        // %~dp0 expands to the shim's directory, with a trailing separator.
        string shimDirectory = fileSystem.Path.GetDirectoryName(fileSystem.Path.GetFullPath(cmdPath))!;
        // A root such as C:\ already ends in a separator; don't double it.
        string shimDirectoryPrefix = fileSystem.Path.EndsInDirectorySeparator(shimDirectory)
            ? shimDirectory
            : shimDirectory + fileSystem.Path.DirectorySeparatorChar;
        string targetPath = fileSystem.Path.GetFullPath(shimDirectoryPrefix + match.Groups["path"].Value);

        // Containment: after normalization (which collapses any ".." segments), the target path must
        // still be under the shim's directory, so the shim text can't point elsewhere. This is a lexical
        // check: a junction or symlink inside the directory can still lead outside it, but creating one
        // there takes the same access as editing the shim, and the target is only read.
        if (!targetPath.StartsWith(shimDirectoryPrefix, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        return targetPath;
    }

    // A quoted path relative to %~dp0, ending in .exe, followed by %*. The path may not contain
    // quotes, % (no variable expansion), drive/stream colons, wildcards, redirection, or control characters.
    [GeneratedRegex(@"^""%~dp0(?<path>[^""%:*?<>|\x00-\x1F]+\.exe)"" %\*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex ShimLineRegex();
}
