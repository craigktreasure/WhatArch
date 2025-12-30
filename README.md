<p align="center">
  <img src="https://raw.githubusercontent.com/craigktreasure/WhatArch/main/assets/icon.png" alt="WhatArch logo" width="128" height="128">
</p>

<h1 align="center">WhatArch</h1>

<p align="center">
  A <a href="https://docs.microsoft.com/dotnet/core/tools/global-tools">.NET tool</a> for Windows that detects the target architecture of native binaries and .NET assemblies.
</p>

<p align="center">
  <a href="https://github.com/craigktreasure/WhatArch/actions/workflows/CI.yml"><img src="https://github.com/craigktreasure/WhatArch/actions/workflows/CI.yml/badge.svg?branch=main" alt="CI"></a>
  <a href="https://codecov.io/gh/craigktreasure/WhatArch"><img src="https://codecov.io/gh/craigktreasure/WhatArch/branch/main/graph/badge.svg?token=OmUMjUGQb8" alt="codecov"></a>
  <a href="https://www.nuget.org/packages/WhatArch/"><img src="https://img.shields.io/nuget/v/WhatArch" alt="NuGet"></a>
  <a href="https://www.nuget.org/packages/WhatArch/"><img src="https://img.shields.io/nuget/dt/WhatArch" alt="NuGet Downloads"></a>
</p>

## Features

- Detects native binary architectures: **x86**, **x64**, **ARM64**, **ARM**
- Identifies .NET assembly configurations: **AnyCPU (.NET)**, **AnyCPU (.NET - 32-bit preferred)**, **x86 (.NET)**, **x64 (.NET)**, **ARM64 (.NET)**
- Simple, script-friendly output
- No external dependencies - parses PE headers directly

## Requirements

- Windows (PE files are Windows-specific)
- .NET 10 [SDK](https://dotnet.microsoft.com/download/dotnet/10.0)

## Installation

```shell
dotnet tool install --global WhatArch
```

## Update the tool

```shell
dotnet tool update --global WhatArch
```

## Usage

```shell
whatarch <path-to-binary>
```

### Examples

```shell
# Native binaries
whatarch C:\Windows\System32\kernel32.dll
> x64

whatarch C:\Windows\SysWOW64\kernel32.dll
> x86

# .NET assemblies
whatarch MyApp.dll
> AnyCPU (.NET)

whatarch MyApp.x64.dll
> x64 (.NET)
```

### Exit Codes

| Code | Meaning                                  |
|------|------------------------------------------|
| 0    | Success                                  |
| 1    | Error (file not found, invalid PE, etc.) |

## How It Works

WhatArch reads the PE (Portable Executable) file headers to determine the target architecture. For .NET assemblies, it also examines the CLR header and CorFlags to distinguish between AnyCPU and platform-specific builds.
