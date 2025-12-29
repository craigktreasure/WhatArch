# WhatArch Technical Documentation

This document explains how WhatArch detects the target architecture of Windows executables and DLLs.

## The PE Format

Windows executables (.exe) and dynamic libraries (.dll) use the **Portable Executable (PE)** format. This format has been used since Windows NT and contains all the information needed to load and execute a program.

### PE File Structure

```
┌─────────────────────────┐
│      DOS Header         │  Legacy header for DOS compatibility
│      (64 bytes)         │  Contains "MZ" signature and PE offset
├─────────────────────────┤
│      DOS Stub           │  Optional DOS program (usually just prints
│                         │  "This program cannot be run in DOS mode")
├─────────────────────────┤
│      PE Signature       │  "PE\0\0" (4 bytes)
├─────────────────────────┤
│   COFF File Header      │  20 bytes - contains Machine type
├─────────────────────────┤
│    Optional Header      │  PE32 (32-bit) or PE32+ (64-bit)
│                         │  Contains Data Directories
├─────────────────────────┤
│    Section Headers      │  Describes .text, .data, .rsrc, etc.
├─────────────────────────┤
│      Sections           │  Actual code and data
└─────────────────────────┘
```

### Finding the Architecture

The target architecture is stored in the **COFF File Header** in a field called `Machine`. WhatArch reads this value by:

1. **Reading the DOS Header** - The first two bytes are "MZ" (0x5A4D). At offset 0x3C, there's a 4-byte value pointing to the PE signature.

2. **Verifying the PE Signature** - At the offset from step 1, we expect "PE\0\0" (0x00004550).

3. **Reading the Machine Field** - Immediately after the PE signature is the COFF header. The first 2 bytes are the Machine type.

### Machine Type Values

| Value | Constant | Architecture |
|-------|----------|--------------|
| 0x014c | IMAGE_FILE_MACHINE_I386 | x86 (32-bit Intel) |
| 0x8664 | IMAGE_FILE_MACHINE_AMD64 | x64 (64-bit Intel/AMD) |
| 0xAA64 | IMAGE_FILE_MACHINE_ARM64 | ARM64 (64-bit ARM) |
| 0x01c4 | IMAGE_FILE_MACHINE_ARMNT | ARM (32-bit ARM Thumb-2) |

## .NET Assemblies

.NET assemblies are PE files with additional metadata. A .NET "AnyCPU" assembly contains platform-independent IL (Intermediate Language) code that the CLR JIT-compiles at runtime for the host architecture.

### The Problem with Machine Type Alone

A .NET AnyCPU assembly still has a Machine type in its PE header - typically 0x014c (x86). But this doesn't mean it only runs on x86; it runs on any platform with a compatible .NET runtime.

To properly identify .NET assemblies, we need to check for the **CLR Header**.

### Detecting .NET Assemblies

The Optional Header contains an array of **Data Directories** - pointers to various tables and structures. Directory entry #14 (index 14, zero-based) is the **CLR Runtime Header** (also called COM Descriptor).

```
Optional Header
├── Standard Fields
├── Windows-Specific Fields
└── Data Directories (16 entries)
    ├── [0]  Export Table
    ├── [1]  Import Table
    ├── ...
    ├── [14] CLR Runtime Header  ← Points to .NET metadata
    └── [15] Reserved
```

If the CLR Runtime Header has a non-zero RVA (Relative Virtual Address), the file is a .NET assembly.

### PE32 vs PE32+

The Optional Header comes in two variants:
- **PE32** (magic 0x10b) - Used by 32-bit executables and AnyCPU .NET assemblies
- **PE32+** (magic 0x20b) - Used by 64-bit executables

The Data Directory is at different offsets:
- PE32: offset 96 from start of Optional Header
- PE32+: offset 112 from start of Optional Header

### Reading CorFlags

The CLR Header contains a **Flags** field (called CorFlags) at offset 16 within the header. These flags determine how the assembly runs:

| Flag | Value | Meaning |
|------|-------|---------|
| COMIMAGE_FLAGS_ILONLY | 0x00000001 | Contains only IL code (no native code) |
| COMIMAGE_FLAGS_32BITREQUIRED | 0x00000002 | Must run in 32-bit process |
| COMIMAGE_FLAGS_32BITPREFERRED | 0x00020000 | Prefers 32-bit, can run 64-bit |

### Interpreting CorFlags

WhatArch combines the Machine type and CorFlags to determine the actual target:

| Machine | ILONLY | 32BITREQUIRED | 32BITPREFERRED | Result |
|---------|--------|---------------|----------------|--------|
| x86 | Yes | No | No | AnyCPU |
| x86 | Yes | No | Yes | AnyCPU (32-bit preferred) |
| x86 | Yes | Yes | - | x86 (.NET) |
| x86 | No | - | - | x86 (.NET) |
| x64 | - | - | - | x64 (.NET) |
| ARM64 | - | - | - | ARM64 (.NET) |

### RVA to File Offset Conversion

The CLR Header RVA is a virtual address, not a file offset. To read it, we must:

1. Parse the Section Headers (after the Optional Header)
2. Find which section contains the RVA
3. Calculate: `FileOffset = PointerToRawData + (RVA - VirtualAddress)`

Each section header (40 bytes) contains:
- `VirtualAddress` - Where the section is loaded in memory
- `VirtualSize` - Size in memory
- `PointerToRawData` - Where the section is in the file
- `SizeOfRawData` - Size in the file

## Code Structure

### PeArchitectureReader.cs

The main parsing logic:

```
GetArchitecture(filePath)
├── Read DOS header, verify "MZ"
├── Seek to PE offset (from 0x3C)
├── Verify "PE\0\0" signature
├── Read Machine type from COFF header
├── Read Optional Header magic (PE32 vs PE32+)
├── Navigate to Data Directory entry 14
├── If CLR header exists:
│   ├── Convert RVA to file offset
│   ├── Read CorFlags
│   └── Return managed architecture name
└── Else:
    └── Return native architecture name
```

### Program.cs

Simple CLI wrapper:
- Validates arguments
- Checks file existence
- Calls `PeArchitectureReader.GetArchitecture()`
- Outputs result or error message

## References

- [PE Format - Microsoft Docs](https://docs.microsoft.com/en-us/windows/win32/debug/pe-format)
- [ECMA-335 (CLI Specification)](https://www.ecma-international.org/publications-and-standards/standards/ecma-335/)
- [CorFlags.exe Tool](https://docs.microsoft.com/en-us/dotnet/framework/tools/corflags-exe-corflags-conversion-tool)
