![WhatArch](https://raw.githubusercontent.com/craigktreasure/WhatArch/main/assets/icon.png)

# WhatArch

A [.NET tool](https://docs.microsoft.com/dotnet/core/tools/global-tools) for Windows that detects the target architecture of native binaries and .NET assemblies.

[![CI](https://github.com/craigktreasure/WhatArch/actions/workflows/CI.yml/badge.svg?branch=main)](https://github.com/craigktreasure/WhatArch/actions/workflows/CI.yml)
[![codecov](https://codecov.io/gh/craigktreasure/WhatArch/branch/main/graph/badge.svg?token=OmUMjUGQb8)](https://codecov.io/gh/craigktreasure/WhatArch)
[![NuGet](https://img.shields.io/nuget/v/WhatArch)](https://www.nuget.org/packages/WhatArch/)
[![NuGet](https://img.shields.io/nuget/dt/WhatArch)](https://www.nuget.org/packages/WhatArch/)

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
