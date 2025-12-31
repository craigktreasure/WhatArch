namespace WhatArch.Tests;

using System.IO.Abstractions.TestingHelpers;

public class ScoopShimResolverTests
{
    [Fact]
    public void ShimResult_IsShimProperty_IsCorrect()
    {
        // Arrange & Act
        var noShim = ScoopShimResolver.ShimResult.NoShim;
        var shimWithoutTarget = ScoopShimResolver.ShimResult.ShimWithoutTarget;
        var shimWithTarget = ScoopShimResolver.ShimResult.ShimWithTarget(@"C:\path\to\target.exe");
        // Assert
        Assert.False(noShim.IsShim);
        Assert.True(shimWithoutTarget.IsShim);
        Assert.True(shimWithTarget.IsShim);
    }

    [Fact]
    public void ShimResult_TargetPathProperty_IsCorrect()
    {
        // Arrange & Act
        var noShim = ScoopShimResolver.ShimResult.NoShim;
        var shimWithoutTarget = ScoopShimResolver.ShimResult.ShimWithoutTarget;
        var shimWithTarget = ScoopShimResolver.ShimResult.ShimWithTarget(@"C:\path\to\target.exe");
        // Assert
        Assert.Null(noShim.TargetPath);
        Assert.Null(shimWithoutTarget.TargetPath);
        Assert.Equal(@"C:\path\to\target.exe", shimWithTarget.TargetPath);
    }

    [Fact]
    public void ShimResult_FollowShimToTarget_IsCorrect()
    {
        // Arrange & Act
        var noShim = ScoopShimResolver.ShimResult.NoShim;
        var shimWithoutTarget = ScoopShimResolver.ShimResult.ShimWithoutTarget;
        var shimWithTarget = ScoopShimResolver.ShimResult.ShimWithTarget(@"C:\path\to\target.exe");

        // Assert
        Assert.False(noShim.FollowShimToTarget);
        Assert.False(shimWithoutTarget.FollowShimToTarget);
        Assert.True(shimWithTarget.FollowShimToTarget);
    }

    [Fact]
    public void TryResolveShim_NoShimFile_ReturnsFalse()
    {
        // Arrange
        var fileSystem = new MockFileSystem();
        byte[] peData = TestPeBuilder.CreateMinimalPe(TestPeBuilder.IMAGE_FILE_MACHINE_I386);
        fileSystem.AddFile(@"C:\test\app.exe", new MockFileData(peData));

        // Act
        var result = ScoopShimResolver.TryResolveShim(fileSystem, @"C:\test\app.exe");

        // Assert
        Assert.False(result.IsShim);
        Assert.Null(result.TargetPath);
    }

    [Fact]
    public void TryResolveShim_WithValidShimFile_ReturnsTargetPath()
    {
        // Arrange
        var fileSystem = new MockFileSystem();
        byte[] shimPeData = TestPeBuilder.CreateMinimalPe(TestPeBuilder.IMAGE_FILE_MACHINE_I386);
        byte[] targetPeData = TestPeBuilder.CreateMinimalPe(TestPeBuilder.IMAGE_FILE_MACHINE_ARM64, isPe32Plus: true);

        fileSystem.AddFile(@"C:\scoop\shims\app.exe", new MockFileData(shimPeData));
        fileSystem.AddFile(@"C:\scoop\shims\app.shim", new MockFileData("path = C:\\scoop\\apps\\app\\current\\app.exe"));
        fileSystem.AddFile(@"C:\scoop\apps\app\current\app.exe", new MockFileData(targetPeData));

        // Act
        var result = ScoopShimResolver.TryResolveShim(fileSystem, @"C:\scoop\shims\app.exe");

        // Assert
        Assert.True(result.IsShim);
        Assert.Equal(@"C:\scoop\apps\app\current\app.exe", result.TargetPath);
    }

    [Fact]
    public void TryResolveShim_ShimFileWithQuotedPath_ReturnsTargetPath()
    {
        // Arrange
        var fileSystem = new MockFileSystem();
        byte[] shimPeData = TestPeBuilder.CreateMinimalPe(TestPeBuilder.IMAGE_FILE_MACHINE_I386);
        byte[] targetPeData = TestPeBuilder.CreateMinimalPe(TestPeBuilder.IMAGE_FILE_MACHINE_AMD64, isPe32Plus: true);

        fileSystem.AddFile(@"C:\scoop\shims\app.exe", new MockFileData(shimPeData));
        fileSystem.AddFile(@"C:\scoop\shims\app.shim", new MockFileData("path = \"C:\\scoop\\apps\\app\\current\\app.exe\""));
        fileSystem.AddFile(@"C:\scoop\apps\app\current\app.exe", new MockFileData(targetPeData));

        // Act
        var result = ScoopShimResolver.TryResolveShim(fileSystem, @"C:\scoop\shims\app.exe");

        // Assert
        Assert.True(result.IsShim);
        Assert.Equal(@"C:\scoop\apps\app\current\app.exe", result.TargetPath);
    }

    [Fact]
    public void TryResolveShim_ShimFileWithSingleQuotedPath_ReturnsTargetPath()
    {
        // Arrange
        var fileSystem = new MockFileSystem();
        byte[] shimPeData = TestPeBuilder.CreateMinimalPe(TestPeBuilder.IMAGE_FILE_MACHINE_I386);
        byte[] targetPeData = TestPeBuilder.CreateMinimalPe(TestPeBuilder.IMAGE_FILE_MACHINE_AMD64, isPe32Plus: true);

        fileSystem.AddFile(@"C:\scoop\shims\app.exe", new MockFileData(shimPeData));
        fileSystem.AddFile(@"C:\scoop\shims\app.shim", new MockFileData("path = 'C:\\scoop\\apps\\app\\current\\app.exe'"));
        fileSystem.AddFile(@"C:\scoop\apps\app\current\app.exe", new MockFileData(targetPeData));

        // Act
        var result = ScoopShimResolver.TryResolveShim(fileSystem, @"C:\scoop\shims\app.exe");

        // Assert
        Assert.True(result.IsShim);
        Assert.Equal(@"C:\scoop\apps\app\current\app.exe", result.TargetPath);
    }

    [Fact]
    public void TryResolveShim_ShimFileWithArgs_ReturnsTargetPath()
    {
        // Arrange
        var fileSystem = new MockFileSystem();
        byte[] shimPeData = TestPeBuilder.CreateMinimalPe(TestPeBuilder.IMAGE_FILE_MACHINE_I386);
        byte[] targetPeData = TestPeBuilder.CreateMinimalPe(TestPeBuilder.IMAGE_FILE_MACHINE_ARM64, isPe32Plus: true);

        string shimContent = """
            path = C:\scoop\apps\app\current\app.exe
            args = --some-arg --another
            """;

        fileSystem.AddFile(@"C:\scoop\shims\app.exe", new MockFileData(shimPeData));
        fileSystem.AddFile(@"C:\scoop\shims\app.shim", new MockFileData(shimContent));
        fileSystem.AddFile(@"C:\scoop\apps\app\current\app.exe", new MockFileData(targetPeData));

        // Act
        var result = ScoopShimResolver.TryResolveShim(fileSystem, @"C:\scoop\shims\app.exe");

        // Assert
        Assert.True(result.IsShim);
        Assert.Equal(@"C:\scoop\apps\app\current\app.exe", result.TargetPath);
    }

    [Fact]
    public void TryResolveShim_ShimFileWithMissingTarget_ReturnsNullTargetPath()
    {
        // Arrange
        var fileSystem = new MockFileSystem();
        byte[] shimPeData = TestPeBuilder.CreateMinimalPe(TestPeBuilder.IMAGE_FILE_MACHINE_I386);

        fileSystem.AddFile(@"C:\scoop\shims\app.exe", new MockFileData(shimPeData));
        fileSystem.AddFile(@"C:\scoop\shims\app.shim", new MockFileData("path = C:\\nonexistent\\app.exe"));
        // Note: Target file does not exist

        // Act
        var result = ScoopShimResolver.TryResolveShim(fileSystem, @"C:\scoop\shims\app.exe");

        // Assert
        Assert.True(result.IsShim);
        Assert.Null(result.TargetPath);
    }

    [Fact]
    public void TryResolveShim_ShimFileWithNoPathLine_ReturnsNullTargetPath()
    {
        // Arrange
        var fileSystem = new MockFileSystem();
        byte[] shimPeData = TestPeBuilder.CreateMinimalPe(TestPeBuilder.IMAGE_FILE_MACHINE_I386);

        fileSystem.AddFile(@"C:\scoop\shims\app.exe", new MockFileData(shimPeData));
        fileSystem.AddFile(@"C:\scoop\shims\app.shim", new MockFileData("args = --some-arg"));

        // Act
        var result = ScoopShimResolver.TryResolveShim(fileSystem, @"C:\scoop\shims\app.exe");

        // Assert
        Assert.True(result.IsShim);
        Assert.Null(result.TargetPath);
    }

    [Fact]
    public void TryResolveShim_EmptyShimFile_ReturnsNullTargetPath()
    {
        // Arrange
        var fileSystem = new MockFileSystem();
        byte[] shimPeData = TestPeBuilder.CreateMinimalPe(TestPeBuilder.IMAGE_FILE_MACHINE_I386);

        fileSystem.AddFile(@"C:\scoop\shims\app.exe", new MockFileData(shimPeData));
        fileSystem.AddFile(@"C:\scoop\shims\app.shim", new MockFileData(""));

        // Act
        var result = ScoopShimResolver.TryResolveShim(fileSystem, @"C:\scoop\shims\app.exe");

        // Assert
        Assert.True(result.IsShim);
        Assert.Null(result.TargetPath);
    }

    [Fact]
    public void TryResolveShim_ShimFileWithWhitespace_ReturnsTargetPath()
    {
        // Arrange
        var fileSystem = new MockFileSystem();
        byte[] shimPeData = TestPeBuilder.CreateMinimalPe(TestPeBuilder.IMAGE_FILE_MACHINE_I386);
        byte[] targetPeData = TestPeBuilder.CreateMinimalPe(TestPeBuilder.IMAGE_FILE_MACHINE_AMD64, isPe32Plus: true);

        fileSystem.AddFile(@"C:\scoop\shims\app.exe", new MockFileData(shimPeData));
        fileSystem.AddFile(@"C:\scoop\shims\app.shim", new MockFileData("  path   =   C:\\scoop\\apps\\app\\current\\app.exe  \r\n"));
        fileSystem.AddFile(@"C:\scoop\apps\app\current\app.exe", new MockFileData(targetPeData));

        // Act
        var result = ScoopShimResolver.TryResolveShim(fileSystem, @"C:\scoop\shims\app.exe");

        // Assert
        Assert.True(result.IsShim);
        Assert.Equal(@"C:\scoop\apps\app\current\app.exe", result.TargetPath);
    }

    [Fact]
    public void TryResolveShim_CaseInsensitivePath_ReturnsTargetPath()
    {
        // Arrange
        var fileSystem = new MockFileSystem();
        byte[] shimPeData = TestPeBuilder.CreateMinimalPe(TestPeBuilder.IMAGE_FILE_MACHINE_I386);
        byte[] targetPeData = TestPeBuilder.CreateMinimalPe(TestPeBuilder.IMAGE_FILE_MACHINE_AMD64, isPe32Plus: true);

        fileSystem.AddFile(@"C:\scoop\shims\app.exe", new MockFileData(shimPeData));
        fileSystem.AddFile(@"C:\scoop\shims\app.shim", new MockFileData("PATH = C:\\scoop\\apps\\app\\current\\app.exe"));
        fileSystem.AddFile(@"C:\scoop\apps\app\current\app.exe", new MockFileData(targetPeData));

        // Act
        var result = ScoopShimResolver.TryResolveShim(fileSystem, @"C:\scoop\shims\app.exe");

        // Assert
        Assert.True(result.IsShim);
        Assert.Equal(@"C:\scoop\apps\app\current\app.exe", result.TargetPath);
    }

    [Fact]
    public void TryResolveShim_NullFileSystem_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => ScoopShimResolver.TryResolveShim(null!, @"C:\test\app.exe"));
    }

    [Fact]
    public void TryResolveShim_NullPath_ThrowsArgumentNullException()
    {
        // Arrange
        var fileSystem = new MockFileSystem();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => ScoopShimResolver.TryResolveShim(fileSystem, null!));
    }
}
