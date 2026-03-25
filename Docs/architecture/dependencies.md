# Dependencies on Other Repositories

FieldWorks depends on several external libraries and related repositories. This document describes those dependencies and how to work with them.

## Overview

Most dependencies are automatically downloaded as NuGet packages during the build process. However, if you need to debug into or modify these libraries, you may need to build them locally.

## Primary Dependencies

### Core Libraries

| Repository | Purpose | Branch |
|------------|---------|--------|
| [sillsdev/liblcm](https://github.com/sillsdev/liblcm) | Language/Culture Model - data access layer | `master` |
| [sillsdev/libpalaso](https://github.com/sillsdev/libpalaso) | SIL shared utilities | `master` |
| [sillsdev/chorus](https://github.com/sillsdev/chorus) | Version control for linguistic data | `master` |

### Related Projects

| Repository | Purpose | Notes |
|------------|---------|-------|
| [sillsdev/FwLocalizations](https://github.com/sillsdev/FwLocalizations) | Translations (Crowdin integration) | Localization workflow |

## Default Dependency Source

By default, dependencies are downloaded as NuGet packages during the build. The version numbers are specified once in `Build/SilVersions.props` and shared by both the managed build (`Directory.Packages.props`) and the native build (`Build/mkall.targets`):

```xml
<SilChorusVersion>...</SilChorusVersion>
<SilLibPalasoVersion>...</SilLibPalasoVersion>
<SilLcmVersion>...</SilLcmVersion>
```

## Building and Debugging Dependencies Locally

If you need to debug into or modify a dependency library, use the `Build/Pack-LocalLibrary.ps1` script. It packs a local checkout into a local NuGet feed using the exact version from `Build/SilVersions.props`, so no version edits are needed in FieldWorks.

Quick start:

```powershell
$env:LOCAL_NUGET_REPO = "C:\localnugetpackages"
.\Build\Pack-LocalLibrary.ps1 -Library libpalaso -SourcePath C:\Repos\libpalaso
.\build.ps1
```

For the full workflow (setup, pack, build, debug, revert), see **[Local Library Debugging](local-library-debugging.md)**.

## Dependency Configuration

Build dependency information is also available in:
- `Build/Agent/dependencies.config`
- `Build/mkall.targets` (target `CopyDlls`)

## GitHub Actions Integration

FieldWorks uses GitHub Actions for CI/CD. The workflow files are in `.github/workflows/`.

Dependencies are restored automatically from NuGet during CI builds.

## See Also

- [Data Migrations](data-migrations.md) - Working with the data model
- [Build Instructions](../../.github/instructions/build.instructions.md) - Building FieldWorks
- [CONTRIBUTING.md](../CONTRIBUTING.md) - Getting started
