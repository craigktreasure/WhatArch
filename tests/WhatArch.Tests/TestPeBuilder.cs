namespace WhatArch.Tests;

/// <summary>
/// Builds minimal PE files for testing edge cases and error scenarios.
/// </summary>
internal static class TestPeBuilder
{
    // Machine types
    public const ushort IMAGE_FILE_MACHINE_I386 = 0x014c;
    public const ushort IMAGE_FILE_MACHINE_AMD64 = 0x8664;
    public const ushort IMAGE_FILE_MACHINE_ARM64 = 0xAA64;
    public const ushort IMAGE_FILE_MACHINE_ARMNT = 0x01c4; // ARM32 Thumb-2
    public const ushort IMAGE_FILE_MACHINE_UNKNOWN = 0x0000;

    /// <summary>
    /// Creates a minimal valid native PE file with the specified machine type.
    /// </summary>
    public static byte[] CreateMinimalPe(ushort machineType, bool isPe32Plus = false)
    {
        using MemoryStream ms = new();
        using BinaryWriter writer = new(ms);

        int optionalHeaderBase = isPe32Plus ? 112 : 96;
        int dataDirectoriesSize = 16 * 8; // 16 entries, 8 bytes each
        ushort optionalHeaderSize = (ushort)(optionalHeaderBase + dataDirectoriesSize);

        // DOS Header
        writer.Write((ushort)0x5A4D); // MZ signature
        writer.Write(new byte[58]);    // DOS header padding
        writer.Write((int)64);         // PE header offset at 0x3C

        // PE Signature
        writer.Write((uint)0x00004550); // "PE\0\0"

        // COFF File Header (20 bytes)
        writer.Write(machineType);      // Machine
        writer.Write((ushort)0);        // NumberOfSections
        writer.Write((uint)0);          // TimeDateStamp
        writer.Write((uint)0);          // PointerToSymbolTable
        writer.Write((uint)0);          // NumberOfSymbols
        writer.Write(optionalHeaderSize); // SizeOfOptionalHeader
        writer.Write((ushort)0);        // Characteristics

        // Optional Header
        writer.Write(isPe32Plus ? (ushort)0x20b : (ushort)0x10b); // Magic (PE32 or PE32+)

        // Fill optional header standard fields
        writer.Write(new byte[optionalHeaderBase - 2]);

        // Data Directories (16 entries, 8 bytes each) - all zeros for native binary
        writer.Write(new byte[dataDirectoriesSize]);

        return ms.ToArray();
    }

    /// <summary>
    /// Creates a minimal .NET assembly PE file.
    /// </summary>
    public static byte[] CreateMinimalManagedPe(ushort machineType, uint corFlags, bool isPe32Plus = false)
    {
        using MemoryStream ms = new();
        using BinaryWriter writer = new(ms);

        int peHeaderOffset = 64;
        int optionalHeaderBase = isPe32Plus ? 112 : 96; // Standard fields before data directories
        int dataDirectoriesSize = 16 * 8; // 16 entries, 8 bytes each
        int optionalHeaderSize = optionalHeaderBase + dataDirectoriesSize;
        int sectionHeaderStart = peHeaderOffset + 24 + optionalHeaderSize; // PE sig (4) + COFF (20) + optional header
        int clrHeaderRva = 0x2000; // Virtual address of CLR header
        int sectionFileOffset = 0x200; // File offset where section data starts (typical alignment)
        int clrHeaderFileOffset = sectionFileOffset; // CLR header is at the start of section data

        // DOS Header
        writer.Write((ushort)0x5A4D); // MZ signature
        writer.Write(new byte[58]);    // DOS header padding
        writer.Write(peHeaderOffset);  // PE header offset at 0x3C

        // PE Signature
        writer.Write((uint)0x00004550); // "PE\0\0"

        // COFF File Header (20 bytes)
        writer.Write(machineType);      // Machine
        writer.Write((ushort)1);        // NumberOfSections
        writer.Write((uint)0);          // TimeDateStamp
        writer.Write((uint)0);          // PointerToSymbolTable
        writer.Write((uint)0);          // NumberOfSymbols
        writer.Write((ushort)optionalHeaderSize); // SizeOfOptionalHeader
        writer.Write((ushort)0);        // Characteristics

        // Optional Header
        writer.Write(isPe32Plus ? (ushort)0x20b : (ushort)0x10b); // Magic

        // Fill standard fields (we need to get to data directories)
        writer.Write(new byte[optionalHeaderBase - 2]);

        // Data Directories (16 entries, 8 bytes each)
        for (int i = 0; i < 14; i++)
        {
            writer.Write((uint)0); // RVA
            writer.Write((uint)0); // Size
        }
        // Entry 14: CLR Runtime Header
        writer.Write((uint)clrHeaderRva);  // RVA
        writer.Write((uint)72);            // Size (standard CLR header size)
        // Entry 15: Reserved
        writer.Write((uint)0);
        writer.Write((uint)0);

        // Section Header (.text) - 40 bytes
        writer.Write(new byte[8] { (byte)'.', (byte)'t', (byte)'e', (byte)'x', (byte)'t', 0, 0, 0 }); // Name
        writer.Write((uint)0x1000);        // VirtualSize
        writer.Write((uint)clrHeaderRva);  // VirtualAddress
        writer.Write((uint)0x200);         // SizeOfRawData
        writer.Write((uint)clrHeaderFileOffset); // PointerToRawData
        writer.Write((uint)0);             // PointerToRelocations
        writer.Write((uint)0);             // PointerToLinenumbers
        writer.Write((ushort)0);           // NumberOfRelocations
        writer.Write((ushort)0);           // NumberOfLinenumbers
        writer.Write((uint)0);             // Characteristics

        // Pad to CLR header location
        while (ms.Position < clrHeaderFileOffset)
        {
            writer.Write((byte)0);
        }

        // CLR Header (72 bytes)
        writer.Write((uint)72);            // cb (size)
        writer.Write((ushort)2);           // MajorRuntimeVersion
        writer.Write((ushort)5);           // MinorRuntimeVersion
        writer.Write((uint)0);             // MetaData RVA
        writer.Write((uint)0);             // MetaData Size
        writer.Write(corFlags);            // Flags
        writer.Write((uint)0);             // EntryPointToken
        // ... rest of CLR header (we only need up to Flags for our tests)
        writer.Write(new byte[72 - 24]);

        return ms.ToArray();
    }

    /// <summary>
    /// Creates a file with invalid MZ signature.
    /// </summary>
    public static byte[] CreateInvalidMzSignature()
    {
        return [0x00, 0x00, .. new byte[62]];
    }

    /// <summary>
    /// Creates a file with valid MZ but invalid PE signature.
    /// </summary>
    public static byte[] CreateInvalidPeSignature()
    {
        using MemoryStream ms = new();
        using BinaryWriter writer = new(ms);

        writer.Write((ushort)0x5A4D); // MZ signature
        writer.Write(new byte[58]);
        writer.Write((int)64);         // PE header offset
        writer.Write((uint)0x00000000); // Invalid PE signature (should be PE\0\0)

        return ms.ToArray();
    }

    /// <summary>
    /// Creates a truncated file (valid MZ but truncated before PE header).
    /// </summary>
    public static byte[] CreateTruncatedBeforePeHeader()
    {
        using MemoryStream ms = new();
        using BinaryWriter writer = new(ms);

        writer.Write((ushort)0x5A4D); // MZ signature
        writer.Write(new byte[58]);
        writer.Write((int)64);         // PE header offset
        // File ends here - no PE header

        return ms.ToArray();
    }

    /// <summary>
    /// Creates a PE with no optional header (SizeOfOptionalHeader = 0).
    /// </summary>
    public static byte[] CreatePeWithNoOptionalHeader(ushort machineType)
    {
        using MemoryStream ms = new();
        using BinaryWriter writer = new(ms);

        writer.Write((ushort)0x5A4D); // MZ signature
        writer.Write(new byte[58]);
        writer.Write((int)64);         // PE header offset

        writer.Write((uint)0x00004550); // PE signature
        writer.Write(machineType);      // Machine
        writer.Write((ushort)0);        // NumberOfSections
        writer.Write((uint)0);          // TimeDateStamp
        writer.Write((uint)0);          // PointerToSymbolTable
        writer.Write((uint)0);          // NumberOfSymbols
        writer.Write((ushort)0);        // SizeOfOptionalHeader = 0
        writer.Write((ushort)0);        // Characteristics

        return ms.ToArray();
    }

    /// <summary>
    /// Saves a byte array to a file in the test output directory.
    /// </summary>
    public static string SaveToTempFile(byte[] data, string filename)
    {
        string path = Path.Combine(Path.GetTempPath(), "WhatArchTests", filename);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllBytes(path, data);
        return path;
    }
}
