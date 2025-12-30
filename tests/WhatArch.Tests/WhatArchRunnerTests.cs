namespace WhatArch.Tests;

using System.IO.Abstractions;
using WhatArch.Abstractions;

public class WhatArchRunnerTests
{
    private static readonly string TestBinariesPath = TestHelpers.GetTestBinariesPath();

    private static readonly string[] ValidNativeArchitectures = ["x86", "x64", "ARM64", "ARM"];

    private static readonly FileSystem FileSystem = new();

    private static readonly EnvironmentVariableProvider VariableProvider = new();

    private static IPath Path => FileSystem.Path;

    #region Success Cases - Test Binaries

    [Theory]
    [InlineData("release_net10.0_anycpu", "SampleApp.dll", "AnyCPU (.NET)")]
    [InlineData("release_net10.0_win-x64", "SampleApp.dll", "x64 (.NET)")]
    [InlineData("release_net10.0_win-x86", "SampleApp.dll", "x86 (.NET)")]
    [InlineData("release_net10.0_win-arm64", "SampleApp.dll", "ARM64 (.NET)")]
    public void Run_ManagedTestBinary_ReturnsExpectedArchitecture(string folder, string filename, string expectedArch)
    {
        // Arrange
        string filePath = Path.Combine(TestBinariesPath, folder, filename);

        // Act
        var result = WhatArchRunner.Run(FileSystem, filePath, VariableProvider);

        // Assert
        Assert.Equal(0, result.ExitCode);
        Assert.Equal(expectedArch, result.Output);
        Assert.Null(result.Error);
    }

    [Theory]
    [InlineData("release_net10.0_win-x64", "SampleApp.exe", "x64")]
    [InlineData("release_net10.0_win-x86", "SampleApp.exe", "x86")]
    [InlineData("release_net10.0_win-arm64", "SampleApp.exe", "ARM64")]
    public void Run_NativeTestBinary_ReturnsExpectedArchitecture(string folder, string filename, string expectedArch)
    {
        // Arrange
        string filePath = Path.Combine(TestBinariesPath, folder, filename);

        // Act
        var result = WhatArchRunner.Run(FileSystem, filePath, VariableProvider);

        // Assert
        Assert.Equal(0, result.ExitCode);
        Assert.Equal(expectedArch, result.Output);
        Assert.Null(result.Error);
    }

    #endregion

    #region Success Cases - PATH Resolution

    [Fact]
    public void Run_SystemExecutableByName_ResolvesAndReturnsValidArchitecture()
    {
        // Arrange - cmd.exe should be in PATH on all Windows systems
        string filename = "cmd.exe";

        // Act
        var result = WhatArchRunner.Run(FileSystem, filename, VariableProvider);

        // Assert
        Assert.Equal(0, result.ExitCode);
        Assert.NotNull(result.Output);
        Assert.Contains(result.Output, ValidNativeArchitectures);
        Assert.Null(result.Error);
    }

    [Fact]
    public void Run_NotepadByName_ResolvesAndReturnsValidArchitecture()
    {
        // Arrange
        string filename = "notepad.exe";

        // Act
        var result = WhatArchRunner.Run(FileSystem, filename, VariableProvider);

        // Assert
        Assert.Equal(0, result.ExitCode);
        Assert.NotNull(result.Output);
        Assert.Contains(result.Output, ValidNativeArchitectures);
        Assert.Null(result.Error);
    }

    [Fact]
    public void Run_DotnetByName_ResolvesAndReturnsValidArchitecture()
    {
        // Arrange - dotnet.exe should be in PATH on systems with .NET SDK
        string filename = "dotnet.exe";

        // Act
        var result = WhatArchRunner.Run(FileSystem, filename, VariableProvider);

        // Assert - May fail if dotnet isn't in PATH, which is acceptable
        if (result.ExitCode == 0)
        {
            Assert.NotNull(result.Output);
            Assert.Contains(result.Output, ValidNativeArchitectures);
            Assert.Null(result.Error);
        }
    }

    #endregion

    #region Error Cases

    [Fact]
    public void Run_NonExistentFile_ReturnsError()
    {
        // Arrange
        string filename = "this_file_definitely_does_not_exist_12345.exe";

        // Act
        var result = WhatArchRunner.Run(FileSystem, filename, VariableProvider);

        // Assert
        Assert.Equal(1, result.ExitCode);
        Assert.Null(result.Output);
        Assert.NotNull(result.Error);
        Assert.Contains("File not found", result.Error, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("searched current directory and PATH", result.Error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Run_NonExistentAbsolutePath_ReturnsError()
    {
        // Arrange
        string filePath = @"C:\NonExistent\Directory\file.exe";

        // Act
        var result = WhatArchRunner.Run(FileSystem, filePath, VariableProvider);

        // Assert
        Assert.Equal(1, result.ExitCode);
        Assert.Null(result.Output);
        Assert.NotNull(result.Error);
        Assert.Contains("File not found", result.Error, StringComparison.OrdinalIgnoreCase);
        // Should NOT mention "searched current directory and PATH" for absolute paths
        Assert.DoesNotContain("searched current directory and PATH", result.Error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Run_InvalidPeFile_ReturnsError()
    {
        // Arrange
        string filePath = Path.Combine(TestBinariesPath, "..", "not_a_pe.txt");

        // Act
        var result = WhatArchRunner.Run(FileSystem, filePath, VariableProvider);

        // Assert
        Assert.Equal(1, result.ExitCode);
        Assert.Null(result.Output);
        Assert.NotNull(result.Error);
        Assert.Contains("Error:", result.Error, StringComparison.OrdinalIgnoreCase);
    }

    #endregion

    #region Relative Path Tests

    [Fact]
    public void Run_RelativePathWithDirectorySeparator_DoesNotSearchPath()
    {
        // Arrange - This relative path doesn't exist and shouldn't search PATH
        string relativePath = @"subdir\cmd.exe";

        // Act
        var result = WhatArchRunner.Run(FileSystem, relativePath, VariableProvider);

        // Assert
        Assert.Equal(1, result.ExitCode);
        Assert.Null(result.Output);
        Assert.NotNull(result.Error);
        // Should NOT mention "searched current directory and PATH" for paths with directory separators
        Assert.DoesNotContain("searched current directory and PATH", result.Error, StringComparison.OrdinalIgnoreCase);
    }

    #endregion
}
