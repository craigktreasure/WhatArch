namespace WhatArch.Tests;

using System.IO.Abstractions;

public class PeArchitectureReaderTests
{
    private static readonly FileSystem FileSystem = new();

    private static readonly string TestBinariesPath = TestHelpers.GetTestBinariesPath();

    private const string SampleAppName = "SampleApp";

    private static IPath Path => FileSystem.Path;

    [Fact]
    public void GetArchitecture_FileNotFound_ThrowsFileNotFoundException()
    {
        // Arrange
        string filePath = Path.Combine(TestBinariesPath, "non_existent_file.exe");

        // Act & Assert
        Assert.Throws<FileNotFoundException>(() => PeArchitectureReader.GetArchitecture(FileSystem, filePath));
    }

    [Fact]
    public void GetArchitecture_InvalidPeFile_ThrowsInvalidOperationException()
    {
        // Arrange
        string filePath = Path.Combine(TestBinariesPath, "..", "not_a_pe.txt");

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => PeArchitectureReader.GetArchitecture(FileSystem, filePath));
    }

    [Theory]
    [InlineData("win-arm64", "ARM64")]
    [InlineData("win-x64", "x64")]
    [InlineData("win-x86", "x86")]
    public void GetArchitecture_NativeBinaries_ReturnsExpectedArchitecture(string rid, string expectedArch)
    {
        // Arrange
        string filePath = Path.Combine(TestBinariesPath, $"release_net10.0_{rid}", SampleAppName + ".exe");

        // Act
        string actualArch = PeArchitectureReader.GetArchitecture(FileSystem, filePath);

        // Assert
        Assert.Equal(expectedArch, actualArch);
    }

    [Theory]
    [InlineData("win-arm64", "ARM64 (.NET)")]
    [InlineData("win-x64", "x64 (.NET)")]
    [InlineData("win-x86", "x86 (.NET)")]
    [InlineData("anycpu", "AnyCPU (.NET)")]
    public void GetArchitecture_Net10Binaries_ReturnsExpectedArchitecture(string rid, string expectedArch)
    {
        // Arrange
        string filePath = Path.Combine(TestBinariesPath, $"release_net10.0_{rid}", SampleAppName + ".dll");

        // Act
        string actualArch = PeArchitectureReader.GetArchitecture(FileSystem, filePath);

        // Assert
        Assert.Equal(expectedArch, actualArch);
    }

    [Theory]
    [InlineData("arm64", "ARM64 (.NET)")]
    [InlineData("x64", "x64 (.NET)")]
    [InlineData("x86", "x86 (.NET)")]
    [InlineData("anycpu", "AnyCPU (.NET)")]
    [InlineData("anycpu_prefer32bit", "AnyCPU (.NET - 32-bit preferred)")]
    public void GetArchitecture_NetFrameworkBinaries_ReturnsExpectedArchitecture(string rid, string expectedArch)
    {
        // Arrange
        string filePath = Path.Combine(TestBinariesPath, $"release_net48_{rid}", SampleAppName + ".exe");

        // Act
        string actualArch = PeArchitectureReader.GetArchitecture(FileSystem, filePath);

        // Assert
        Assert.Equal(expectedArch, actualArch);
    }

    [Fact]
    public void GetArchitecture_NullParameter_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => PeArchitectureReader.GetArchitecture(null!, "something"));
        Assert.Throws<ArgumentNullException>(() => PeArchitectureReader.GetArchitecture(FileSystem, null!));
    }
}
