# WhatArch

A .NET 10 command-line tool for Windows that detects the target architecture of native binaries and .NET assemblies.

## Features

- Detects native binary architectures: **x86**, **x64**, **ARM64**, **ARM**
- Identifies .NET assembly configurations: **AnyCPU**, **AnyCPU (32-bit preferred)**, **x86 (.NET)**, **x64 (.NET)**, **ARM64 (.NET)**
- Simple, script-friendly output
- No external dependencies - parses PE headers directly

## Installation

### As a Global Tool

```powershell
dotnet pack
dotnet tool install --global --add-source ./nupkg WhatArch
```

### Build and Run Locally

```powershell
dotnet build
dotnet run -- <path-to-binary>
```

## Usage

```powershell
whatarch <path-to-binary>
```

### Examples

```powershell
# Native binaries
whatarch C:\Windows\System32\kernel32.dll
> x64

whatarch C:\Windows\SysWOW64\kernel32.dll
> x86

# .NET assemblies
whatarch MyApp.dll
> AnyCPU

whatarch MyApp.x64.dll
> x64 (.NET)
```

### Exit Codes

| Code | Meaning |
|------|---------|
| 0 | Success |
| 1 | Error (file not found, invalid PE, etc.) |

## How It Works

WhatArch reads the PE (Portable Executable) file headers to determine the target architecture. For .NET assemblies, it also examines the CLR header and CorFlags to distinguish between AnyCPU and platform-specific builds.

See [docs/architecture.md](docs/architecture.md) for technical details.

## Requirements

- .NET 10 SDK
- Windows (PE files are Windows-specific)

## License

MIT
