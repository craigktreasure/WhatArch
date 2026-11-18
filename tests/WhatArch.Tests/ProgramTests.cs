namespace WhatArch.Tests;

using System.IO.Abstractions.TestingHelpers;
using WhatArch.Tests.TestUtilities;

public sealed class ProgramTests : IDisposable
{
    private readonly MockFileSystem _fileSystem = new();
    private readonly FakeEnvironmentVariableProvider _variableProvider = new();
    private readonly StringWriter _output = new();
    private readonly StringWriter _error = new();

    public ProgramTests()
    {
        byte[] shimPeData = TestPeBuilder.CreateMinimalPe(TestPeBuilder.IMAGE_FILE_MACHINE_I386);
        byte[] targetPeData = TestPeBuilder.CreateMinimalPe(TestPeBuilder.IMAGE_FILE_MACHINE_ARM64, isPe32Plus: true);

        _fileSystem.AddFile(@"C:\scoop\shims\app.exe", new MockFileData(shimPeData));
        _fileSystem.AddFile(@"C:\scoop\shims\app.shim", new MockFileData("path = C:\\scoop\\apps\\app\\current\\app.exe"));
        _fileSystem.AddFile(@"C:\scoop\apps\app\current\app.exe", new MockFileData(targetPeData));
    }

    public void Dispose()
    {
        _output.Dispose();
        _error.Dispose();
    }

    [Theory]
    [InlineData(new[] { @"C:\scoop\shims\app.exe" }, "x86 (shim) -> ARM64")]
    [InlineData(new[] { "--short", @"C:\scoop\shims\app.exe" }, "ARM64")]
    [InlineData(new[] { @"C:\scoop\shims\app.exe", "-s" }, "ARM64")]
    public void Invoke_Shim_WritesFormattedOutput(string[] args, string expected)
    {
        int exitCode = Program.Invoke(args, _fileSystem, _variableProvider, _output, _error);

        Assert.Equal(0, exitCode);
        Assert.Equal(expected + Environment.NewLine, _output.ToString());
        Assert.Empty(_error.ToString());
    }

    [Fact]
    public void Invoke_MissingFile_WritesErrorOnly()
    {
        int exitCode = Program.Invoke(["--short", @"C:\missing.exe"], _fileSystem, _variableProvider, _output, _error);

        Assert.Equal(1, exitCode);
        Assert.Empty(_output.ToString());
        Assert.Equal($"Error: File not found: C:\\missing.exe{Environment.NewLine}", _error.ToString());
    }
}
