namespace WhatArch.Tests;

using System.IO.Abstractions;

public class FileResolverTests
{
    private static readonly FileSystem FileSystem = new();

    private static readonly string TestBinariesPath = TestHelpers.GetTestBinariesPath();

    #region ShouldSearchPath Tests

    [Theory]
    [InlineData("notepad.exe", true)]
    [InlineData("cmd.exe", true)]
    [InlineData("myapp.dll", true)]
    public void ShouldSearchPath_BareFilename_ReturnsTrue(string path, bool expected)
    {
        // Act
        bool result = FileResolver.ShouldSearchPath(FileSystem, path);

        // Assert
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(@"C:\Windows\notepad.exe")]
    [InlineData(@"D:\Apps\myapp.exe")]
    [InlineData(@"/usr/bin/app")]
    public void ShouldSearchPath_AbsolutePath_ReturnsFalse(string path)
    {
        // Act
        bool result = FileResolver.ShouldSearchPath(FileSystem, path);

        // Assert
        Assert.False(result);
    }

    [Theory]
    [InlineData(@"subdir\myapp.exe")]
    [InlineData(@".\myapp.exe")]
    [InlineData(@"..\myapp.exe")]
    [InlineData(@"folder/myapp.exe")]
    public void ShouldSearchPath_RelativePathWithSeparator_ReturnsFalse(string path)
    {
        // Act
        bool result = FileResolver.ShouldSearchPath(FileSystem, path);

        // Assert
        Assert.False(result);
    }

    #endregion

    #region TryResolve Tests

    [Fact]
    public void TryResolve_NullPath_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => FileResolver.TryResolve(null!, "something", out _));
        Assert.Throws<ArgumentNullException>(() => FileResolver.TryResolve(FileSystem, null!, out _));
    }

    [Fact]
    public void TryResolve_ExistingAbsolutePath_ReturnsTrue()
    {
        // Arrange
        string filePath = Path.Combine(TestBinariesPath, "release_net10.0_anycpu", "SampleApp.dll");

        // Act
        bool result = FileResolver.TryResolve(FileSystem, filePath, out string? resolvedPath);

        // Assert
        Assert.True(result);
        Assert.NotNull(resolvedPath);
        Assert.True(File.Exists(resolvedPath));
    }

    [Fact]
    public void TryResolve_NonExistingPath_ReturnsFalse()
    {
        // Arrange
        string filePath = Path.Combine(TestBinariesPath, "non_existent_file.exe");

        // Act
        bool result = FileResolver.TryResolve(FileSystem, filePath, out string? resolvedPath);

        // Assert
        Assert.False(result);
        Assert.Null(resolvedPath);
    }

    [Fact]
    public void TryResolve_BareFilenameInPath_ReturnsTrue()
    {
        // Arrange - Use the test binaries directory as a mock PATH
        string mockPath = Path.Combine(TestBinariesPath, "release_net10.0_anycpu");
        string filename = "SampleApp.dll";

        // Act
        bool result = FileResolver.TryResolve(FileSystem, filename, mockPath, out string? resolvedPath);

        // Assert
        Assert.True(result);
        Assert.NotNull(resolvedPath);
        Assert.EndsWith("SampleApp.dll", resolvedPath, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void TryResolve_BareFilenameNotInPath_ReturnsFalse()
    {
        // Arrange
        string mockPath = TestBinariesPath; // Directory exists but doesn't contain the file directly
        string filename = "nonexistent.exe";

        // Act
        bool result = FileResolver.TryResolve(FileSystem, filename, mockPath, out string? resolvedPath);

        // Assert
        Assert.False(result);
        Assert.Null(resolvedPath);
    }

    [Fact]
    public void TryResolve_MultiplePaths_FindsInSecondPath()
    {
        // Arrange
        string emptyDir = Path.GetTempPath();
        string validDir = Path.Combine(TestBinariesPath, "release_net10.0_anycpu");
        string mockPath = $"{emptyDir}{Path.PathSeparator}{validDir}";
        string filename = "SampleApp.dll";

        // Act
        bool result = FileResolver.TryResolve(FileSystem, filename, mockPath, out string? resolvedPath);

        // Assert
        Assert.True(result);
        Assert.NotNull(resolvedPath);
        Assert.Contains("release_net10.0_anycpu", resolvedPath, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void TryResolve_EmptyPath_ReturnsFalse()
    {
        // Arrange
        string filename = "SampleApp.dll";

        // Act
        bool result = FileResolver.TryResolve(FileSystem, filename, "", out string? resolvedPath);

        // Assert
        Assert.False(result);
        Assert.Null(resolvedPath);
    }

    [Fact]
    public void TryResolve_NullPathEnvironment_ReturnsFalseForBareFilename()
    {
        // Arrange
        string filename = "nonexistent.exe";

        // Act
        bool result = FileResolver.TryResolve(FileSystem, filename, null, out string? resolvedPath);

        // Assert
        Assert.False(result);
        Assert.Null(resolvedPath);
    }

    [Fact]
    public void TryResolve_RelativePathWithSeparator_DoesNotSearchPath()
    {
        // Arrange - Even if the file exists in PATH, relative paths shouldn't trigger PATH search
        string validDir = Path.Combine(TestBinariesPath, "release_net10.0_anycpu");
        string mockPath = validDir;
        string relativePath = @"subdir\SampleApp.dll"; // This shouldn't search PATH

        // Act
        bool result = FileResolver.TryResolve(FileSystem, relativePath, mockPath, out string? resolvedPath);

        // Assert
        Assert.False(result);
        Assert.Null(resolvedPath);
    }

    #endregion
}
