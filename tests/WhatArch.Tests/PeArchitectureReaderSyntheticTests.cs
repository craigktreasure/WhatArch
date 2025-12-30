namespace WhatArch.Tests;

using System.IO.Abstractions;

/// <summary>
/// Tests using synthetically constructed PE files to cover edge cases
/// that are difficult to test with real binaries.
/// </summary>
public sealed class PeArchitectureReaderSyntheticTests : IDisposable
{
    private static readonly FileSystem FileSystem = new();

    private readonly List<string> _tempFiles = [];

    public void Dispose()
    {
        foreach (string file in _tempFiles)
        {
            try
            {
                File.Delete(file);
            }
            catch (IOException)
            {
                // Ignore cleanup failures
            }
        }
    }

    private string SaveTempFile(byte[] data, string name)
    {
        string path = TestPeBuilder.SaveToTempFile(FileSystem, data, $"{Guid.NewGuid()}_{name}");
        _tempFiles.Add(path);
        return path;
    }

    #region Native Binary Tests with Synthetic PEs

    [Theory]
    [InlineData(TestPeBuilder.IMAGE_FILE_MACHINE_I386, "x86")]
    [InlineData(TestPeBuilder.IMAGE_FILE_MACHINE_AMD64, "x64")]
    [InlineData(TestPeBuilder.IMAGE_FILE_MACHINE_ARM64, "ARM64")]
    [InlineData(TestPeBuilder.IMAGE_FILE_MACHINE_ARMNT, "ARM")]
    public void GetArchitecture_SyntheticNativePe_ReturnsExpectedArchitecture(ushort machineType, string expectedArch)
    {
        // Arrange
        byte[] peData = TestPeBuilder.CreateMinimalPe(machineType);
        string filePath = SaveTempFile(peData, $"native_{expectedArch}.exe");

        // Act
        string actualArch = PeArchitectureReader.GetArchitecture(FileSystem, filePath);

        // Assert
        Assert.Equal(expectedArch, actualArch);
    }

    [Fact]
    public void GetArchitecture_UnknownMachineType_ReturnsUnknownWithHex()
    {
        // Arrange
        ushort unknownMachine = 0x9999;
        byte[] peData = TestPeBuilder.CreateMinimalPe(unknownMachine);
        string filePath = SaveTempFile(peData, "unknown_machine.exe");

        // Act
        string actualArch = PeArchitectureReader.GetArchitecture(FileSystem, filePath);

        // Assert
        Assert.Equal("Unknown (0x9999)", actualArch);
    }

    [Fact]
    public void GetArchitecture_PeWithNoOptionalHeader_ReturnsNativeArch()
    {
        // Arrange
        byte[] peData = TestPeBuilder.CreatePeWithNoOptionalHeader(TestPeBuilder.IMAGE_FILE_MACHINE_AMD64);
        string filePath = SaveTempFile(peData, "no_optional_header.exe");

        // Act
        string actualArch = PeArchitectureReader.GetArchitecture(FileSystem, filePath);

        // Assert
        Assert.Equal("x64", actualArch);
    }

    [Fact]
    public void GetArchitecture_Pe32PlusFormat_ReturnsCorrectArchitecture()
    {
        // Arrange - PE32+ is used for 64-bit binaries
        byte[] peData = TestPeBuilder.CreateMinimalPe(TestPeBuilder.IMAGE_FILE_MACHINE_AMD64, isPe32Plus: true);
        string filePath = SaveTempFile(peData, "pe32plus.exe");

        // Act
        string actualArch = PeArchitectureReader.GetArchitecture(FileSystem, filePath);

        // Assert
        Assert.Equal("x64", actualArch);
    }

    #endregion

    #region Managed (.NET) Binary Tests with Synthetic PEs

    [Fact]
    public void GetArchitecture_SyntheticManagedAnyCpu_ReturnsAnyCpu()
    {
        // Arrange - AnyCPU: x86 machine, ILONLY flag, no 32-bit flags
        const uint ILONLY = 0x00000001;
        byte[] peData = TestPeBuilder.CreateMinimalManagedPe(TestPeBuilder.IMAGE_FILE_MACHINE_I386, ILONLY);
        string filePath = SaveTempFile(peData, "managed_anycpu.dll");

        // Act
        string actualArch = PeArchitectureReader.GetArchitecture(FileSystem, filePath);

        // Assert
        Assert.Equal("AnyCPU (.NET)", actualArch);
    }

    [Fact]
    public void GetArchitecture_SyntheticManagedAnyCpu32BitPreferred_ReturnsAnyCpu32BitPreferred()
    {
        // Arrange - AnyCPU 32-bit preferred: x86 machine, ILONLY + 32BITPREFERRED
        const uint ILONLY = 0x00000001;
        const uint PREFER32BIT = 0x00020000;
        byte[] peData = TestPeBuilder.CreateMinimalManagedPe(TestPeBuilder.IMAGE_FILE_MACHINE_I386, ILONLY | PREFER32BIT);
        string filePath = SaveTempFile(peData, "managed_anycpu_prefer32.dll");

        // Act
        string actualArch = PeArchitectureReader.GetArchitecture(FileSystem, filePath);

        // Assert
        Assert.Equal("AnyCPU (.NET - 32-bit preferred)", actualArch);
    }

    [Fact]
    public void GetArchitecture_SyntheticManagedX86_ReturnsX86Managed()
    {
        // Arrange - x86 .NET: x86 machine, ILONLY + 32BITREQUIRED
        const uint ILONLY = 0x00000001;
        const uint REQUIRE32BIT = 0x00000002;
        byte[] peData = TestPeBuilder.CreateMinimalManagedPe(TestPeBuilder.IMAGE_FILE_MACHINE_I386, ILONLY | REQUIRE32BIT);
        string filePath = SaveTempFile(peData, "managed_x86.dll");

        // Act
        string actualArch = PeArchitectureReader.GetArchitecture(FileSystem, filePath);

        // Assert
        Assert.Equal("x86 (.NET)", actualArch);
    }

    [Fact]
    public void GetArchitecture_SyntheticManagedX64_ReturnsX64Managed()
    {
        // Arrange - x64 .NET
        byte[] peData = TestPeBuilder.CreateMinimalManagedPe(TestPeBuilder.IMAGE_FILE_MACHINE_AMD64, 0x00000001, isPe32Plus: true);
        string filePath = SaveTempFile(peData, "managed_x64.dll");

        // Act
        string actualArch = PeArchitectureReader.GetArchitecture(FileSystem, filePath);

        // Assert
        Assert.Equal("x64 (.NET)", actualArch);
    }

    [Fact]
    public void GetArchitecture_SyntheticManagedArm64_ReturnsArm64Managed()
    {
        // Arrange - ARM64 .NET
        byte[] peData = TestPeBuilder.CreateMinimalManagedPe(TestPeBuilder.IMAGE_FILE_MACHINE_ARM64, 0x00000001, isPe32Plus: true);
        string filePath = SaveTempFile(peData, "managed_arm64.dll");

        // Act
        string actualArch = PeArchitectureReader.GetArchitecture(FileSystem, filePath);

        // Assert
        Assert.Equal("ARM64 (.NET)", actualArch);
    }

    [Fact]
    public void GetArchitecture_SyntheticManagedArm32_ReturnsArmManaged()
    {
        // Arrange - ARM32 .NET (Thumb-2)
        byte[] peData = TestPeBuilder.CreateMinimalManagedPe(TestPeBuilder.IMAGE_FILE_MACHINE_ARMNT, 0x00000001);
        string filePath = SaveTempFile(peData, "managed_arm.dll");

        // Act
        string actualArch = PeArchitectureReader.GetArchitecture(FileSystem, filePath);

        // Assert
        Assert.Equal("ARM (.NET)", actualArch);
    }

    [Fact]
    public void GetArchitecture_SyntheticManagedUnknownMachine_ReturnsUnknownManaged()
    {
        // Arrange - Unknown machine type for managed assembly
        ushort unknownMachine = 0x8888;
        byte[] peData = TestPeBuilder.CreateMinimalManagedPe(unknownMachine, 0x00000001);
        string filePath = SaveTempFile(peData, "managed_unknown.dll");

        // Act
        string actualArch = PeArchitectureReader.GetArchitecture(FileSystem, filePath);

        // Assert
        Assert.Equal("Unknown (.NET, 0x8888)", actualArch);
    }

    [Fact]
    public void GetArchitecture_ManagedX86NotIlOnly_ReturnsX86Managed()
    {
        // Arrange - x86 machine but NOT IL-only (mixed mode or native + managed)
        byte[] peData = TestPeBuilder.CreateMinimalManagedPe(TestPeBuilder.IMAGE_FILE_MACHINE_I386, 0x00000000);
        string filePath = SaveTempFile(peData, "managed_mixed_x86.dll");

        // Act
        string actualArch = PeArchitectureReader.GetArchitecture(FileSystem, filePath);

        // Assert
        Assert.Equal("x86 (.NET)", actualArch);
    }

    #endregion

    #region Error Cases with Synthetic PEs

    [Fact]
    public void GetArchitecture_InvalidMzSignature_ThrowsInvalidOperationException()
    {
        // Arrange
        byte[] data = TestPeBuilder.CreateInvalidMzSignature();
        string filePath = SaveTempFile(data, "invalid_mz.exe");

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => PeArchitectureReader.GetArchitecture(FileSystem, filePath));
    }

    [Fact]
    public void GetArchitecture_InvalidPeSignature_ThrowsInvalidOperationException()
    {
        // Arrange
        byte[] data = TestPeBuilder.CreateInvalidPeSignature();
        string filePath = SaveTempFile(data, "invalid_pe.exe");

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => PeArchitectureReader.GetArchitecture(FileSystem, filePath));
    }

    #endregion
}
