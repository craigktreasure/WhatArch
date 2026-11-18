namespace WhatArch.Tests;

using System.IO.Abstractions.TestingHelpers;
using System.Text;
using WhatArch.Tests.TestUtilities;

public class DotNetToolShimResolverTests
{
    private const string ShimPath = @"C:\Users\me\.dotnet\tools\mytool.cmd";
    private const string TargetPath = @"C:\Users\me\.dotnet\tools\.store\mytool\1.0.0\mytool.win-arm64\1.0.0\tools\any\win-arm64\MyTool.exe";
    private const string SdkShim = "@echo off\r\n\"%~dp0.store\\mytool\\1.0.0\\mytool.win-arm64\\1.0.0\\tools\\any\\win-arm64\\MyTool.exe\" %*\r\n";

    private readonly MockFileSystem _fileSystem = new();
    private readonly FakeEnvironmentVariableProvider _variableProvider = new();

    [Fact]
    public void ParseTargetPath_SdkShim_ReturnsTarget()
    {
        AddShimAndTarget(SdkShim);

        Assert.Equal(TargetPath, DotNetToolShimResolver.ParseTargetPath(_fileSystem, ShimPath));
    }

    [Fact]
    public void ParseTargetPath_LfLineEndings_ReturnsTarget()
    {
        AddShimAndTarget(SdkShim.Replace("\r\n", "\n", StringComparison.Ordinal));

        Assert.Equal(TargetPath, DotNetToolShimResolver.ParseTargetPath(_fileSystem, ShimPath));
    }

    [Theory]
    [InlineData("")]
    [InlineData("@echo off")]
    // Extra commands, even harmless-looking ones, mean this isn't the SDK's shim.
    [InlineData("@echo off\r\nset X=1\r\n\"%~dp0.store\\a\\MyTool.exe\" %*")]
    [InlineData("\"%~dp0.store\\a\\MyTool.exe\" %*")]
    // Absolute path instead of %~dp0-relative.
    [InlineData("@echo off\r\n\"C:\\Windows\\System32\\cmd.exe\" %*")]
    // Environment variable expansion.
    [InlineData("@echo off\r\n\"%~dp0%OTHER%\\MyTool.exe\" %*")]
    // Not an .exe (for example, another script).
    [InlineData("@echo off\r\n\"%~dp0.store\\a\\other.cmd\" %*")]
    // Command chaining after the call.
    [InlineData("@echo off\r\n\"%~dp0.store\\a\\MyTool.exe\" %* & calc.exe")]
    // Alternate data stream.
    [InlineData("@echo off\r\n\"%~dp0.store\\a\\MyTool.exe:hidden.exe\" %*")]
    public void ParseTargetPath_UnrecognizedContent_ReturnsNull(string content)
    {
        AddShimAndTarget(content);
        _fileSystem.AddFile(@"C:\Users\me\.dotnet\tools\.store\a\MyTool.exe", new MockFileData([0x4D, 0x5A]));

        Assert.Null(DotNetToolShimResolver.ParseTargetPath(_fileSystem, ShimPath));
    }

    [Fact]
    public void ParseTargetPath_PathTraversalOutsideShimDirectory_ReturnsNull()
    {
        _fileSystem.AddFile(ShimPath, new MockFileData("@echo off\r\n\"%~dp0..\\..\\..\\..\\Windows\\notepad.exe\" %*"));
        _fileSystem.AddFile(@"C:\Windows\notepad.exe", new MockFileData([0x4D, 0x5A]));

        Assert.Null(DotNetToolShimResolver.ParseTargetPath(_fileSystem, ShimPath));
    }

    [Fact]
    public void ParseTargetPath_TraversalToSiblingWithSharedPrefix_ReturnsNull()
    {
        // C:\...\tools-evil starts with C:\...\tools but is not inside it.
        _fileSystem.AddFile(ShimPath, new MockFileData("@echo off\r\n\"%~dp0..\\tools-evil\\MyTool.exe\" %*"));
        _fileSystem.AddFile(@"C:\Users\me\.dotnet\tools-evil\MyTool.exe", new MockFileData([0x4D, 0x5A]));

        Assert.Null(DotNetToolShimResolver.ParseTargetPath(_fileSystem, ShimPath));
    }

    [Fact]
    public void ParseTargetPath_MissingTarget_StillReturnsPath()
    {
        // Existence is the caller's job, so it can report a broken shim distinctly.
        _fileSystem.AddFile(ShimPath, new MockFileData(SdkShim));

        Assert.Equal(TargetPath, DotNetToolShimResolver.ParseTargetPath(_fileSystem, ShimPath));
    }

    [Fact]
    public void ParseTargetPath_ShimAtDriveRoot_ReturnsTarget()
    {
        _fileSystem.AddFile(@"C:\mytool.cmd", new MockFileData("@echo off\r\n\"%~dp0.store\\a\\MyTool.exe\" %*\r\n"));

        Assert.Equal(@"C:\.store\a\MyTool.exe", DotNetToolShimResolver.ParseTargetPath(_fileSystem, @"C:\mytool.cmd"));
    }

    [Fact]
    public void ParseTargetPath_Utf8Bom_ReturnsTarget()
    {
        byte[] content = [.. Encoding.UTF8.GetPreamble(), .. Encoding.UTF8.GetBytes(SdkShim)];
        _fileSystem.AddFile(ShimPath, new MockFileData(content));

        Assert.Equal(TargetPath, DotNetToolShimResolver.ParseTargetPath(_fileSystem, ShimPath));
    }

    [Fact]
    public void ParseTargetPath_UpperCase_ReturnsTarget()
    {
        string upperShimPath = @"C:\Users\me\.dotnet\tools\MYTOOL.CMD";
        _fileSystem.AddFile(upperShimPath, new MockFileData("@ECHO OFF\r\n\"%~DP0.store\\a\\MYTOOL.EXE\" %*\r\n"));

        Assert.True(DotNetToolShimResolver.IsCmdFile(_fileSystem, upperShimPath));
        Assert.Equal(@"C:\Users\me\.dotnet\tools\.store\a\MYTOOL.EXE", DotNetToolShimResolver.ParseTargetPath(_fileSystem, upperShimPath));
    }

    [Fact]
    public void ParseTargetPath_DotDotThatStaysInside_ReturnsTarget()
    {
        AddShimAndTarget("@echo off\r\n\"%~dp0sub\\..\\.store\\mytool\\1.0.0\\mytool.win-arm64\\1.0.0\\tools\\any\\win-arm64\\MyTool.exe\" %*\r\n");

        Assert.Equal(TargetPath, DotNetToolShimResolver.ParseTargetPath(_fileSystem, ShimPath));
    }

    [Fact]
    public void ParseTargetPath_ForwardSlashTraversal_ReturnsNull()
    {
        _fileSystem.AddFile(ShimPath, new MockFileData("@echo off\r\n\"%~dp0../../../../Windows/notepad.exe\" %*"));

        Assert.Null(DotNetToolShimResolver.ParseTargetPath(_fileSystem, ShimPath));
    }

    [Theory]
    [InlineData(4096, true)]
    [InlineData(4097, false)]
    public void ParseTargetPath_SizeLimit_IsInclusive(int length, bool expectRecognized)
    {
        // Trailing whitespace after the last line is ignored, so it pads without changing meaning.
        AddShimAndTarget(SdkShim.PadRight(length));

        Assert.Equal(expectRecognized ? TargetPath : null, DotNetToolShimResolver.ParseTargetPath(_fileSystem, ShimPath));
    }

    [Fact]
    public void Run_SdkShimByName_ReturnsTargetArchitecture()
    {
        AddShimAndTarget(SdkShim);
        _variableProvider.AppendToPath(@"C:\Users\me\.dotnet\tools");

        var result = WhatArchRunner.Run(_fileSystem, "mytool", _variableProvider);

        Assert.Equal(0, result.ExitCode);
        Assert.Equal(".cmd (shim) -> ARM64", result.FormatOutput(shortOutput: false));
    }

    [Fact]
    public void Run_SdkShimWithShortOutput_ReturnsTargetArchitectureOnly()
    {
        AddShimAndTarget(SdkShim);

        var result = WhatArchRunner.Run(_fileSystem, ShimPath, _variableProvider);

        Assert.Equal(0, result.ExitCode);
        Assert.Equal("ARM64", result.FormatOutput(shortOutput: true));
    }

    [Fact]
    public void Run_SdkShimByNameInCurrentDirectory_ReturnsTargetArchitecture()
    {
        AddShimAndTarget(SdkShim);
        _fileSystem.Directory.SetCurrentDirectory(@"C:\Users\me\.dotnet\tools");

        var result = WhatArchRunner.Run(_fileSystem, "mytool", _variableProvider);

        Assert.Equal(0, result.ExitCode);
        Assert.Equal(".cmd (shim) -> ARM64", result.FormatOutput(shortOutput: false));
    }

    [Fact]
    public void Run_ShimWithMissingTarget_ReturnsTargetNotFoundError()
    {
        _fileSystem.AddFile(ShimPath, new MockFileData(SdkShim));

        var result = WhatArchRunner.Run(_fileSystem, ShimPath, _variableProvider);

        Assert.Equal(1, result.ExitCode);
        Assert.Equal($"Error: .NET tool shim target not found: {TargetPath}", result.Error);
    }

    [Fact]
    public void Run_ShimWithNonPeTarget_ReturnsError()
    {
        _fileSystem.AddFile(ShimPath, new MockFileData(SdkShim));
        _fileSystem.AddFile(TargetPath, new MockFileData("not a PE file"));

        var result = WhatArchRunner.Run(_fileSystem, ShimPath, _variableProvider);

        Assert.Equal(1, result.ExitCode);
        Assert.Equal("Error: Not a valid PE file", result.Error);
    }

    [Fact]
    public void Run_UnrecognizedCmd_ReturnsError()
    {
        _fileSystem.AddFile(@"C:\scripts\build.cmd", new MockFileData("@echo off\r\nmsbuild %*"));

        var result = WhatArchRunner.Run(_fileSystem, @"C:\scripts\build.cmd", _variableProvider);

        Assert.Equal(1, result.ExitCode);
        Assert.Contains("not a recognized .NET tool shim", result.Error, StringComparison.Ordinal);
    }

    private void AddShimAndTarget(string shimContent)
    {
        _fileSystem.AddFile(ShimPath, new MockFileData(shimContent));
        _fileSystem.AddFile(TargetPath, new MockFileData(TestPeBuilder.CreateMinimalPe(TestPeBuilder.IMAGE_FILE_MACHINE_ARM64, isPe32Plus: true)));
    }
}
