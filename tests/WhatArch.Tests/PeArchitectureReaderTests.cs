namespace WhatArch.Tests;

public class PeArchitectureReaderTests
{
    private static readonly string TestBinariesPath = GetTestBinariesPath();

    private const string SampleAppName = "SampleApp";

    [Fact]
    public void GetArchitecture_FileNotFound_ThrowsFileNotFoundException()
    {
        // Arrange
        string filePath = Path.Combine(TestBinariesPath, "non_existent_file.exe");

        // Act & Assert
        Assert.Throws<FileNotFoundException>(() => PeArchitectureReader.GetArchitecture(filePath));
    }

    [Fact]
    public void GetArchitecture_InvalidPeFile_ThrowsInvalidOperationException()
    {
        // Arrange
        string filePath = Path.Combine(TestBinariesPath, "..", "not_a_pe.txt");

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => PeArchitectureReader.GetArchitecture(filePath));
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
        string actualArch = PeArchitectureReader.GetArchitecture(filePath);

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
        string actualArch = PeArchitectureReader.GetArchitecture(filePath);

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
        string actualArch = PeArchitectureReader.GetArchitecture(filePath);

        // Assert
        Assert.Equal(expectedArch, actualArch);
    }

    [Fact]
    public void GetArchitecture_NullParameter_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => PeArchitectureReader.GetArchitecture(null!));
    }

    private static string GetTestBinariesPath()
    {
        string LocateRootDirectory()
        {
            string? currentDir = AppContext.BaseDirectory;

            while (currentDir != null)
            {
                if (Directory.Exists(Path.Combine(currentDir, ".git")))
                {
                    return currentDir;
                }

                currentDir = Path.GetDirectoryName(currentDir);
            }

            throw new InvalidOperationException("Could not locate the root directory of the repository.");
        }

        return Path.Combine(LocateRootDirectory(), "tests", "assets", "binaries");
    }
}
