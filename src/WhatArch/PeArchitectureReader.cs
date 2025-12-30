namespace WhatArch;

using System.IO.Abstractions;

/// <summary>
/// Provides methods for determining the processor architecture of a Portable Executable (PE) file, including native
/// binaries and .NET assemblies.
/// </summary>
/// <remarks>This class is intended for use with Windows PE files, such as EXE and DLL files. It supports
/// detection of both native and managed (.NET) architectures, including x86, x64, ARM, ARM64, and AnyCPU variants. All
/// members are static and thread-safe.</remarks>
internal static class PeArchitectureReader
{
    // Machine type constants
    private const ushort IMAGE_FILE_MACHINE_I386 = 0x014c;
    private const ushort IMAGE_FILE_MACHINE_AMD64 = 0x8664;
    private const ushort IMAGE_FILE_MACHINE_ARM64 = 0xAA64;
    private const ushort IMAGE_FILE_MACHINE_ARMNT = 0x01c4;

    // PE format constants
    // private const ushort PE32_MAGIC = 0x10b;
    private const ushort PE32PLUS_MAGIC = 0x20b;
    private const int CLR_HEADER_DIRECTORY_INDEX = 14;

    // CorFlags constants
    private const uint COMIMAGE_FLAGS_ILONLY = 0x00000001;
    private const uint COMIMAGE_FLAGS_32BITREQUIRED = 0x00000002;
    private const uint COMIMAGE_FLAGS_32BITPREFERRED = 0x00020000;

    /// <summary>
    /// Determines the processor architecture of a Portable Executable (PE) file, including native binaries and .NET
    /// assemblies.
    /// </summary>
    /// <remarks>For .NET assemblies, the returned architecture reflects the target platform as specified in
    /// the assembly's metadata (e.g., "AnyCPU", "x86", "x64"). For native binaries, the architecture is determined from
    /// the PE header. This method does not validate whether the file is a managed or unmanaged binary beyond its PE
    /// format.</remarks>
    /// <param name="fileSystem">The file system abstraction to use for file operations. Cannot be null.</param>
    /// <param name="filePath">The path to the PE file whose architecture is to be identified. Cannot be null. The file must exist and be
    /// accessible for reading.</param>
    /// <returns>A string representing the processor architecture of the specified file, such as "x86", "x64", "ARM", or "AnyCPU"
    /// for .NET assemblies.</returns>
    /// <exception cref="FileNotFoundException">Thrown if the file specified by <paramref name="filePath"/> does not exist.</exception>
    /// <exception cref="InvalidOperationException">Thrown if the specified file is not a valid PE file.</exception>
    public static string GetArchitecture(IFileSystem fileSystem, string filePath)
    {
        ArgumentNullException.ThrowIfNull(fileSystem);
        ArgumentNullException.ThrowIfNull(filePath);

        if (!fileSystem.File.Exists(filePath))
        {
            throw new FileNotFoundException("The specified file does not exist.", filePath);
        }

        using FileSystemStream stream = fileSystem.File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
        using BinaryReader reader = new(stream);

        // Read DOS header
        if (reader.ReadUInt16() != 0x5A4D) // "MZ"
        {
            throw new InvalidOperationException("Not a valid PE file");
        }

        // Seek to PE header offset (at 0x3C in DOS header)
        stream.Seek(0x3C, SeekOrigin.Begin);
        int peOffset = reader.ReadInt32();

        // Verify PE signature
        stream.Seek(peOffset, SeekOrigin.Begin);
        if (reader.ReadUInt32() != 0x00004550) // "PE\0\0"
        {
            throw new InvalidOperationException("Not a valid PE file");
        }

        // Read IMAGE_FILE_HEADER
        ushort machine = reader.ReadUInt16();
        ushort numberOfSections = reader.ReadUInt16();
        stream.Seek(12, SeekOrigin.Current); // Skip TimeDateStamp, PointerToSymbolTable, NumberOfSymbols
        ushort sizeOfOptionalHeader = reader.ReadUInt16();
        stream.Seek(2, SeekOrigin.Current); // Skip Characteristics

        // Check if we have an optional header
        if (sizeOfOptionalHeader == 0)
        {
            return GetNativeArchName(machine);
        }

        // Read Optional Header magic to determine PE32 vs PE32+
        long optionalHeaderStart = stream.Position;
        ushort magic = reader.ReadUInt16();

        bool isPE32Plus = magic == PE32PLUS_MAGIC;

        // Navigate to Data Directory
        // PE32: OptionalHeader is 96 bytes before DataDirectory
        // PE32+: OptionalHeader is 112 bytes before DataDirectory
        int dataDirectoryOffset = isPE32Plus ? 112 : 96;
        stream.Seek(optionalHeaderStart + dataDirectoryOffset, SeekOrigin.Begin);

        // Read CLR Runtime Header entry (index 14)
        stream.Seek(CLR_HEADER_DIRECTORY_INDEX * 8, SeekOrigin.Current);
        uint clrRva = reader.ReadUInt32();
        uint clrSize = reader.ReadUInt32();

        // If no CLR header, it's a native binary
        if (clrRva == 0 || clrSize == 0)
        {
            return GetNativeArchName(machine);
        }

        // It's a .NET assembly - read CorFlags
        uint corFlags = ReadCorFlags(reader, stream, peOffset, numberOfSections, clrRva);

        return GetManagedArchName(machine, corFlags);
    }

    private static uint ReadCorFlags(BinaryReader reader, FileSystemStream stream, int peOffset, ushort numberOfSections, uint clrRva)
    {
        // Need to convert RVA to file offset using section headers
        long sectionHeadersStart = peOffset + 24 + reader.BaseStream.Position - reader.BaseStream.Position;

        // Go back to read optional header size to find section headers
        stream.Seek(peOffset + 4 + 16, SeekOrigin.Begin); // PE sig + COFF header up to SizeOfOptionalHeader
        ushort optHeaderSize = reader.ReadUInt16();

        // Section headers start after optional header
        long sectionStart = peOffset + 24 + optHeaderSize; // 24 = PE sig (4) + COFF header (20)
        stream.Seek(sectionStart, SeekOrigin.Begin);

        // Find the section containing the CLR header RVA
        for (int i = 0; i < numberOfSections; i++)
        {
            stream.Seek(sectionStart + (i * 40), SeekOrigin.Begin);
            stream.Seek(8, SeekOrigin.Current); // Skip Name
            uint virtualSize = reader.ReadUInt32();
            uint virtualAddress = reader.ReadUInt32();
            uint sizeOfRawData = reader.ReadUInt32();
            uint pointerToRawData = reader.ReadUInt32();

            if (clrRva >= virtualAddress && clrRva < virtualAddress + virtualSize)
            {
                // Found the section - calculate file offset
                uint fileOffset = pointerToRawData + (clrRva - virtualAddress);

                // CLR header structure: first 4 bytes are size, then major/minor version (4 bytes)
                // then MetaData RVA/Size (8 bytes), then Flags (4 bytes) at offset 16
                stream.Seek(fileOffset + 16, SeekOrigin.Begin);
                return reader.ReadUInt32();
            }
        }

        return 0; // Couldn't read CorFlags, treat as native
    }

    private static string GetNativeArchName(ushort machine) => machine switch
    {
        IMAGE_FILE_MACHINE_I386 => "x86",
        IMAGE_FILE_MACHINE_AMD64 => "x64",
        IMAGE_FILE_MACHINE_ARM64 => "ARM64",
        IMAGE_FILE_MACHINE_ARMNT => "ARM",
        _ => $"Unknown (0x{machine:X4})"
    };

    private static string GetManagedArchName(ushort machine, uint corFlags)
    {
        bool isILOnly = (corFlags & COMIMAGE_FLAGS_ILONLY) != 0;
        bool is32BitRequired = (corFlags & COMIMAGE_FLAGS_32BITREQUIRED) != 0;
        bool is32BitPreferred = (corFlags & COMIMAGE_FLAGS_32BITPREFERRED) != 0;

        // AnyCPU variants (IL-only x86 assemblies)
        if (isILOnly && machine == IMAGE_FILE_MACHINE_I386)
        {
            if (is32BitPreferred)
            {
                return "AnyCPU (.NET - 32-bit preferred)";
            }

            if (is32BitRequired)
            {
                return "x86 (.NET)";
            }

            return "AnyCPU (.NET)";
        }

        // Platform-specific .NET assemblies
        return machine switch
        {
            IMAGE_FILE_MACHINE_I386 => "x86 (.NET)",
            IMAGE_FILE_MACHINE_AMD64 => "x64 (.NET)",
            IMAGE_FILE_MACHINE_ARM64 => "ARM64 (.NET)",
            IMAGE_FILE_MACHINE_ARMNT => "ARM (.NET)",
            _ => $"Unknown (.NET, 0x{machine:X4})"
        };
    }
}
