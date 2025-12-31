namespace WhatArch.Tests;

using System.IO.Abstractions.TestingHelpers;
using WhatArch.Tests.TestUtilities;

public class WhatArchRunnerShimTests
{
    private readonly MockFileSystem _fileSystem = new();
    private readonly FakeEnvironmentVariableProvider _variableProvider = new();

    [Fact]
    public void Run_ShimWithX86ToArm64Target_ReturnsShimOutput()
    {
        // Arrange
        byte[] shimPeData = TestPeBuilder.CreateMinimalPe(TestPeBuilder.IMAGE_FILE_MACHINE_I386);
        byte[] targetPeData = TestPeBuilder.CreateMinimalPe(TestPeBuilder.IMAGE_FILE_MACHINE_ARM64, isPe32Plus: true);

        _fileSystem.AddFile(@"C:\scoop\shims\app.exe", new MockFileData(shimPeData));
        _fileSystem.AddFile(@"C:\scoop\shims\app.shim", new MockFileData("path = C:\\scoop\\apps\\app\\current\\app.exe"));
        _fileSystem.AddFile(@"C:\scoop\apps\app\current\app.exe", new MockFileData(targetPeData));

        // Act
        var result = WhatArchRunner.Run(_fileSystem, @"C:\scoop\shims\app.exe", _variableProvider);

        // Assert
        Assert.Equal(0, result.ExitCode);
        Assert.Equal("x86 (shim) -> ARM64", result.Output);
        Assert.Null(result.Error);
    }

    [Fact]
    public void Run_ShimWithX86ToX64Target_ReturnsShimOutput()
    {
        // Arrange
        byte[] shimPeData = TestPeBuilder.CreateMinimalPe(TestPeBuilder.IMAGE_FILE_MACHINE_I386);
        byte[] targetPeData = TestPeBuilder.CreateMinimalPe(TestPeBuilder.IMAGE_FILE_MACHINE_AMD64, isPe32Plus: true);

        _fileSystem.AddFile(@"C:\scoop\shims\app.exe", new MockFileData(shimPeData));
        _fileSystem.AddFile(@"C:\scoop\shims\app.shim", new MockFileData("path = C:\\scoop\\apps\\app\\current\\app.exe"));
        _fileSystem.AddFile(@"C:\scoop\apps\app\current\app.exe", new MockFileData(targetPeData));

        // Act
        var result = WhatArchRunner.Run(_fileSystem, @"C:\scoop\shims\app.exe", _variableProvider);

        // Assert
        Assert.Equal(0, result.ExitCode);
        Assert.Equal("x86 (shim) -> x64", result.Output);
        Assert.Null(result.Error);
    }

    [Fact]
    public void Run_ShimWithManagedTarget_ReturnsShimOutputWithDotNet()
    {
        // Arrange
        byte[] shimPeData = TestPeBuilder.CreateMinimalPe(TestPeBuilder.IMAGE_FILE_MACHINE_I386);
        byte[] targetPeData = TestPeBuilder.CreateMinimalManagedPe(
            TestPeBuilder.IMAGE_FILE_MACHINE_I386,
            0x00000001, // ILONLY - AnyCPU
            isPe32Plus: false);

        _fileSystem.AddFile(@"C:\scoop\shims\dotnetapp.exe", new MockFileData(shimPeData));
        _fileSystem.AddFile(@"C:\scoop\shims\dotnetapp.shim", new MockFileData("path = C:\\scoop\\apps\\dotnetapp\\current\\dotnetapp.dll"));
        _fileSystem.AddFile(@"C:\scoop\apps\dotnetapp\current\dotnetapp.dll", new MockFileData(targetPeData));

        // Act
        var result = WhatArchRunner.Run(_fileSystem, @"C:\scoop\shims\dotnetapp.exe", _variableProvider);

        // Assert
        Assert.Equal(0, result.ExitCode);
        Assert.Equal("x86 (shim) -> AnyCPU (.NET)", result.Output);
        Assert.Null(result.Error);
    }

    [Fact]
    public void Run_ShimWithMissingTarget_FallsBackToShimArchitecture()
    {
        // Arrange
        byte[] shimPeData = TestPeBuilder.CreateMinimalPe(TestPeBuilder.IMAGE_FILE_MACHINE_I386);

        _fileSystem.AddFile(@"C:\scoop\shims\app.exe", new MockFileData(shimPeData));
        _fileSystem.AddFile(@"C:\scoop\shims\app.shim", new MockFileData("path = C:\\nonexistent\\app.exe"));
        // Note: Target file does not exist

        // Act
        var result = WhatArchRunner.Run(_fileSystem, @"C:\scoop\shims\app.exe", _variableProvider);

        // Assert - Should fall back to just showing shim architecture
        Assert.Equal(0, result.ExitCode);
        Assert.Equal("x86", result.Output);
        Assert.Null(result.Error);
    }

    [Fact]
    public void Run_ExecutableWithNoShimFile_ReturnsNormalArchitecture()
    {
        // Arrange
        byte[] peData = TestPeBuilder.CreateMinimalPe(TestPeBuilder.IMAGE_FILE_MACHINE_ARM64, isPe32Plus: true);

        _fileSystem.AddFile(@"C:\apps\app.exe", new MockFileData(peData));
        // No .shim file

        // Act
        var result = WhatArchRunner.Run(_fileSystem, @"C:\apps\app.exe", _variableProvider);

        // Assert
        Assert.Equal(0, result.ExitCode);
        Assert.Equal("ARM64", result.Output);
        Assert.Null(result.Error);
    }

    [Fact]
    public void Run_ShimFoundViaPath_ReturnsShimOutput()
    {
        // Arrange
        byte[] shimPeData = TestPeBuilder.CreateMinimalPe(TestPeBuilder.IMAGE_FILE_MACHINE_I386);
        byte[] targetPeData = TestPeBuilder.CreateMinimalPe(TestPeBuilder.IMAGE_FILE_MACHINE_ARM64, isPe32Plus: true);

        _fileSystem.AddDirectory(@"C:\scoop\shims");
        _fileSystem.AddFile(@"C:\scoop\shims\app.exe", new MockFileData(shimPeData));
        _fileSystem.AddFile(@"C:\scoop\shims\app.shim", new MockFileData("path = C:\\scoop\\apps\\app\\current\\app.exe"));
        _fileSystem.AddFile(@"C:\scoop\apps\app\current\app.exe", new MockFileData(targetPeData));
        _variableProvider.SetPath(@"C:\scoop\shims");

        // Act - Search by bare filename
        var result = WhatArchRunner.Run(_fileSystem, "app.exe", _variableProvider);

        // Assert
        Assert.Equal(0, result.ExitCode);
        Assert.Equal("x86 (shim) -> ARM64", result.Output);
        Assert.Null(result.Error);
    }

    [Fact]
    public void Run_ShimWithEmptyShimFile_FallsBackToShimArchitecture()
    {
        // Arrange
        byte[] shimPeData = TestPeBuilder.CreateMinimalPe(TestPeBuilder.IMAGE_FILE_MACHINE_I386);

        _fileSystem.AddFile(@"C:\scoop\shims\app.exe", new MockFileData(shimPeData));
        _fileSystem.AddFile(@"C:\scoop\shims\app.shim", new MockFileData(""));

        // Act
        var result = WhatArchRunner.Run(_fileSystem, @"C:\scoop\shims\app.exe", _variableProvider);

        // Assert - Should fall back to just showing shim architecture
        Assert.Equal(0, result.ExitCode);
        Assert.Equal("x86", result.Output);
        Assert.Null(result.Error);
    }

    [Fact]
    public void Run_ShimWithInvalidShimFile_FallsBackToShimArchitecture()
    {
        // Arrange
        byte[] shimPeData = TestPeBuilder.CreateMinimalPe(TestPeBuilder.IMAGE_FILE_MACHINE_I386);

        _fileSystem.AddFile(@"C:\scoop\shims\app.exe", new MockFileData(shimPeData));
        _fileSystem.AddFile(@"C:\scoop\shims\app.shim", new MockFileData("not a valid shim file"));

        // Act
        var result = WhatArchRunner.Run(_fileSystem, @"C:\scoop\shims\app.exe", _variableProvider);

        // Assert - Should fall back to just showing shim architecture
        Assert.Equal(0, result.ExitCode);
        Assert.Equal("x86", result.Output);
        Assert.Null(result.Error);
    }
}
