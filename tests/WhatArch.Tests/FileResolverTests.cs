namespace WhatArch.Tests;

using System.IO.Abstractions.TestingHelpers;
using WhatArch.Abstractions;
using WhatArch.Tests.TestUtilities;

public class FileResolverTests
{
    private readonly MockFileSystem _fileSystem = new();

    private readonly FakeEnvironmentVariableProvider _variableProvider = new();

    #region ShouldSearchPath Tests

    [Theory]
    [InlineData("notepad.exe", true)]
    [InlineData("cmd.exe", true)]
    [InlineData("myapp.dll", true)]
    public void ShouldSearchPath_BareFilename_ReturnsTrue(string path, bool expected)
    {
        // Act
        bool result = FileResolver.ShouldSearchPath(_fileSystem, path);

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
        bool result = FileResolver.ShouldSearchPath(_fileSystem, path);

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
        bool result = FileResolver.ShouldSearchPath(_fileSystem, path);

        // Assert
        Assert.False(result);
    }

    #endregion

    #region TryResolve Tests

    [Fact]
    public void TryResolve_NullPath_ThrowsArgumentNullException()
    {
        // Arrange
        FakeEnvironmentVariableProvider variableProvider = new();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => FileResolver.TryResolve(null!, "something", variableProvider, out _));
        Assert.Throws<ArgumentNullException>(() => FileResolver.TryResolve(_fileSystem, null!, variableProvider, out _));
        Assert.Throws<ArgumentNullException>(() => FileResolver.TryResolve(_fileSystem, "something", (IEnvironmentVariableProvider)null!, out _));
    }

    [Fact]
    public void TryResolve_ExistingAbsolutePath_ReturnsTrue()
    {
        // Arrange
        string filePath = PopulateFileSystemAndPathWithBinary();

        // Act
        bool result = FileResolver.TryResolve(_fileSystem, filePath, _variableProvider, out string? resolvedPath);

        // Assert
        Assert.True(result);
        Assert.NotNull(resolvedPath);
        Assert.True(_fileSystem.File.Exists(resolvedPath));
    }

    [Fact]
    public void TryResolve_NonExistingPath_ReturnsFalse()
    {
        // Arrange
        FakeEnvironmentVariableProvider variableProvider = new();
        string filePath = "C:/non_existent_file.exe";

        // Act
        bool result = FileResolver.TryResolve(_fileSystem, filePath, variableProvider, out string? resolvedPath);

        // Assert
        Assert.False(result);
        Assert.Null(resolvedPath);
    }

    [Fact]
    public void TryResolve_BareFilenameInPath_ReturnsTrue()
    {
        // Arrange
        string filePath = PopulateFileSystemAndPathWithBinary();
        string fileName = _fileSystem.Path.GetFileName(filePath);

        // Act
        bool result = FileResolver.TryResolve(_fileSystem, fileName, _variableProvider, out string? resolvedPath);

        // Assert
        Assert.True(result);
        Assert.NotNull(resolvedPath);
        Assert.EndsWith(fileName, resolvedPath, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void TryResolve_BareFilenameNotInPath_ReturnsFalse()
    {
        // Arrange
        string filePath = PopulateFileSystemAndPathWithBinary(skipAddFile: true);
        string fileName = _fileSystem.Path.GetFileName(filePath);

        // Act
        bool result = FileResolver.TryResolve(_fileSystem, fileName, _variableProvider, out string? resolvedPath);

        // Assert
        Assert.False(result);
        Assert.Null(resolvedPath);
    }

    [Fact]
    public void TryResolve_MultiplePaths_FindsInSecondPath()
    {
        // Arrange
        AddToPath(@"C:/foo");
        string filePath = PopulateFileSystemAndPathWithBinary();
        string fileName = _fileSystem.Path.GetFileName(filePath);
        string expectedDirectoryName = _fileSystem.Path.GetDirectoryName(filePath)!;

        // Act
        bool result = FileResolver.TryResolve(_fileSystem, fileName, _variableProvider, out string? resolvedPath);

        // Assert
        Assert.True(result);
        Assert.NotNull(resolvedPath);
        Assert.Contains(expectedDirectoryName, resolvedPath, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void TryResolve_EmptyPath_ReturnsFalse()
    {
        // Arrange
        _variableProvider.SetPath(string.Empty);
        string filename = "SampleApp.dll";

        // Act
        bool result = FileResolver.TryResolve(_fileSystem, filename, _variableProvider, out string? resolvedPath);

        // Assert
        Assert.False(result);
        Assert.Null(resolvedPath);
    }

    [Fact]
    public void TryResolve_EmptyPathValue_ReturnsFalse()
    {
        // Arrange
        _variableProvider.SetPath(string.Empty);
        string filePath = PopulateFileSystemAndPathWithBinary();
        string fileName = _fileSystem.Path.GetFileName(filePath);

        // Act
        bool result = FileResolver.TryResolve(_fileSystem, fileName, _variableProvider, out string? resolvedPath);

        // Assert
        Assert.True(result);
        Assert.NotNull(resolvedPath);
    }

    [Fact]
    public void TryResolve_NullPathEnvironment_ReturnsFalseForBareFilename()
    {
        // Arrange
        string filename = "nonexistent.exe";

        // Act
        bool result = FileResolver.TryResolve(_fileSystem, filename, _variableProvider, out string? resolvedPath);

        // Assert
        Assert.False(result);
        Assert.Null(resolvedPath);
    }

    [Fact]
    public void TryResolve_RelativePathWithSeparator_DoesNotSearchPath()
    {
        // Arrange - Even if the file exists in PATH, relative paths shouldn't trigger PATH search
        string filePath = PopulateFileSystemAndPathWithBinary();
        string fileName = _fileSystem.Path.GetFileName(filePath);
        string relativePath = $"subdir/{fileName}"; // This shouldn't search PATH

        // Act
        bool result = FileResolver.TryResolve(_fileSystem, relativePath, _variableProvider, out string? resolvedPath);

        // Assert
        Assert.False(result);
        Assert.Null(resolvedPath);
    }

    #endregion

    private void AddToPath(string directoryPath, bool createDirectory = true)
    {
        if (createDirectory && !_fileSystem.Directory.Exists(directoryPath))
        {
            _fileSystem.AddDirectory(directoryPath);
        }

        _variableProvider.AppendToPath(directoryPath);
    }

    private string PopulateFileSystemAndPathWithBinary(bool skipAddFile = false)
    {
        string binaryFilePath = @"C:\binaries\app.exe";
        string binariesDirectoryPath = _fileSystem.Path.GetDirectoryName(binaryFilePath)!;
        AddToPath(binariesDirectoryPath);

        if (!skipAddFile)
        {
            _fileSystem.AddFile(binaryFilePath, new MockFileData("Dummy content"));
        }

        return binaryFilePath;
    }
}
