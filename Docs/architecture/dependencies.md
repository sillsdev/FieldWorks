# Dependencies on Other Repositories

FieldWorks depends on several external libraries and related repositories. This document describes those dependencies and the supported local-development workflow for them.

## Overview

Most dependencies are automatically downloaded as NuGet packages during the build process. If you need to debug into or modify these libraries locally, use the local package workflow driven by `build.ps1`.

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

## Local Package Validation Workflow

Use a local package workflow when you are changing `libpalaso`, `liblcm`, or `chorus` and want FieldWorks to consume those changes exactly the way it consumes released packages.

### Step 1: Clone the Repositories

```bash
# Clone to any location
git clone https://github.com/sillsdev/liblcm.git
git clone https://github.com/sillsdev/libpalaso.git
git clone https://github.com/sillsdev/chorus.git
```

### Step 2: Set the Repository Environment Variables

Set one environment variable for each local dependency checkout you want FieldWorks to pack:

```powershell
$env:FW_LOCAL_PALASO = 'C:\src\libpalaso'
$env:FW_LOCAL_LCM = 'C:\src\liblcm'
$env:FW_LOCAL_CHORUS = 'C:\src\chorus'
```

`build.ps1` validates these paths before it tries to pack anything. If you enable `-LocalPalaso`, `-LocalLcm`, or `-LocalChorus` without the matching environment variable, the build stops with an error.

### Step 3: Build in Order Through `build.ps1`

The supported control surface is `build.ps1`. It packs selected dependency repos into `Output/LocalNuGetFeed`, writes `Build/SilVersions.Local.props` with the temporary version overrides, then restores and builds FieldWorks against those local packages.

Dependencies are packed in this order:

1. `libpalaso`
2. `liblcm` and `chorus` in parallel
3. FieldWorks

Examples:

```powershell
# Use only a local liblcm checkout
.\build.ps1 -LocalLcm

# Use all three local repos with the default local version
.\build.ps1 -LocalPalaso -LocalLcm -LocalChorus

# Override the temporary package version written into the local feed
.\build.ps1 -LocalPalaso -LocalLcm -LocalChorus -LocalPackageVersion 99.0.0-dev42
```

### Step 4: Run Tests the Same Way

`test.ps1` accepts the same switches and forwards them to `build.ps1` before running the selected test pass.

```powershell
.\test.ps1 -LocalPalaso -LocalLcm -LocalChorus
```

The local package workflow is intended for local development only. CI stays on the pinned versions from `Build/SilVersions.props`.

## Debugging Dependencies

For the detailed `liblcm` debugging workflow, see `Docs/architecture/liblcm-debugging.md`.

Short version:

1. Use Visual Studio 2022 as the primary debugger for `.NET Framework` plus native FieldWorks work.
2. If you need to step into local `liblcm` code, build FieldWorks with `./build.ps1 -LocalLcm` so the loaded package contains your local symbols.
3. Use VS Code only for limited managed-only sessions in this repo, and only with the legacy C# extension path.
4. If breakpoints show "No symbols loaded", verify the loaded module path and PDB match before changing debugger settings.

## Dependency Configuration

Build dependency information is also available in:
- `Build/Agent/dependencies.config`
- `Build/mkall.targets` (target `CopyDlls`)

## GitHub Actions Integration

FieldWorks uses GitHub Actions for CI/CD. The workflow files are in `.github/workflows/`.

Dependencies are restored automatically from NuGet during CI builds. CI does not use the local package feed or the generated `Build/SilVersions.Local.props` override file.

## See Also

- [Data Migrations](data-migrations.md) - Working with the data model
- [Build Instructions](../../.github/instructions/build.instructions.md) - Building FieldWorks
- [CONTRIBUTING.md](../CONTRIBUTING.md) - Getting started
