# AGENTS.md

This file provides guidance to AI coding agents when working with code in this repository.

## Project Overview

WhatArch is a .NET global tool for Windows that detects the target architecture of native PE binaries and .NET assemblies. It parses PE headers directly without external dependencies.

## Build and Test Commands

```shell
# Build the solution
dotnet build

# Run all tests
dotnet test

# Run a specific test
dotnet test --filter "FullyQualifiedName~TestMethodName"

# Run tests with code coverage
dotnet test --collect:"XPlat Code Coverage"

# Run the tool locally (after build)
dotnet run --project src\WhatArch -- <path-to-binary>
```

## Architecture

### Core Components

- **PeArchitectureReader** (`src/WhatArch/PeArchitectureReader.cs`) - Core PE parsing logic that reads DOS/PE headers, COFF machine type, and CLR header/CorFlags for .NET assemblies. Determines architecture (x86, x64, ARM, ARM64, AnyCPU variants).

- **WhatArchRunner** (`src/WhatArch/WhatArchRunner.cs`) - Orchestrates the analysis: resolves file paths, calls `PeArchitectureReader`, handles Scoop shims, and returns structured results.

- **FileResolver** (`src/WhatArch/FileResolver.cs`) - Resolves input paths by checking current directory and searching PATH. Auto-appends `.exe` on Windows for extensionless inputs.

- **ScoopShimResolver** (`src/WhatArch/ScoopShimResolver.cs`) - Detects Scoop shims (x86 launcher executables) and resolves to the actual target binary by parsing `.shim` files.

- **Program** (`src/WhatArch/Program.cs`) - CLI entry point using System.CommandLine.

### File System Abstraction

The codebase uses `System.IO.Abstractions` throughout for testability. All file operations go through `IFileSystem`. Tests use `MockFileSystem` from `TestableIO.System.IO.Abstractions.TestingHelpers`.

### Build Configuration

- Uses SDK-style artifacts output (`__artifacts` directory)
- Central package management via `Directory.Packages.props`
- Build defaults configured in `eng/Defaults.props`, `eng/DotNetDefaults.props`, `eng/DotNetAnalyzers.props`

### Test Patterns

Tests are in `tests/WhatArch.Tests/`. Key patterns:
- `TestPeBuilder` creates synthetic PE files in memory for testing different architectures
- Tests use `MockFileSystem` to avoid actual file system access
- `FakeEnvironmentVariableProvider` for PATH resolution tests
