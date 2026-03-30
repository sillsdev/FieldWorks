# Local Library Debugging

This document describes how to debug locally-modified versions of **liblcm**, **libpalaso**, or **chorus** in FieldWorks using a local NuGet feed.

## Overview

The workflow uses a single PowerShell script (`Build/Manage-LocalLibraries.ps1`) that:

1. Runs `dotnet pack` in Debug configuration with symbols, letting the library use its own version.
2. Detects the version from the produced packages.
3. Updates `SilVersions.props` so FieldWorks resolves that exact version.
4. Places `.nupkg` / `.snupkg` in your local NuGet feed folder.
5. Copies PDB files to `Output/Debug/` and `Downloads/` for debugger access.
6. Clears stale cached packages so the next restore picks up the local build.

This approach works identically for all three libraries.

## Setup (one-time)

### 1. Create a local NuGet folder

Pick any folder, for example:

```
C:\localnugetpackages
```

### 2. Set the `LOCAL_NUGET_REPO` environment variable

```powershell
# Current session
$env:LOCAL_NUGET_REPO = "C:\localnugetpackages"

# Persistent (user-level)
[System.Environment]::SetEnvironmentVariable("LOCAL_NUGET_REPO", "C:\localnugetpackages", "User")
```

The `nuget.config` in the FieldWorks repo already references `%LOCAL_NUGET_REPO%` as a package source. NuGet silently ignores it when the variable is unset.

### 3. Clone the library you need

```bash
git clone https://github.com/sillsdev/liblcm.git
git clone https://github.com/sillsdev/libpalaso.git
git clone https://github.com/sillsdev/chorus.git
```

## Pack a local library

```powershell
# Single library — explicit path
.\Build\Manage-LocalLibraries.ps1 -Palaso -PalasoPath C:\Repos\libpalaso

# Multiple libraries (libpalaso is always packed first)
.\Build\Manage-LocalLibraries.ps1 -Palaso -PalasoPath C:\Repos\libpalaso -Chorus -ChorusPath C:\Repos\chorus
```

Or set environment variables so you can omit the paths:

```powershell
$env:LIBPALASO_PATH = "C:\Repos\libpalaso"
$env:LIBLCM_PATH    = "C:\Repos\liblcm"
$env:LIBCHORUS_PATH = "C:\Repos\chorus"

# Switches still required — env vars only provide the path
.\Build\Manage-LocalLibraries.ps1 -Palaso -Chorus
```

The script:
- Lets the library build with its own version (no version override).
- Detects the produced version and updates `Build/SilVersions.props` to match.
- Produces `.snupkg` symbol packages (same format as production).
- Copies PDB files to `Output/Debug/` and `Downloads/` for the debugger.
- Clears stale packages from the `packages/` cache.

## Build FieldWorks

```powershell
.\build.ps1
```

The build will print a yellow message listing any local packages detected in `LOCAL_NUGET_REPO`. NuGet restore will use your local packages because `SilVersions.props` was updated to request the exact version produced by the library.

## Debug

1. Open FieldWorks in Visual Studio.
2. PDB files are already in `Output/Debug/` — the debugger will find them automatically.
3. If breakpoints show "No symbols loaded", disable **Debug > Options > Enable Just My Code**.
4. You can also open the library solution side-by-side and use **Debug > Attach to Process**.

## Iterating

After each change to the library:

1. Re-run `Manage-LocalLibraries.ps1` (~30-60 seconds).
2. Re-run `.\build.ps1`.

## Setting a specific version

Use `-Version` to set any library to a specific version in `SilVersions.props` without packing:

```powershell
# Revert libpalaso to an upstream version
.\Build\Manage-LocalLibraries.ps1 -Library libpalaso -Version 17.0.0

# Set liblcm to a specific pre-release version
.\Build\Manage-LocalLibraries.ps1 -Library liblcm -Version 11.0.0-beta0159
```

This updates `SilVersions.props` and clears stale cached packages. Run `.\build.ps1` afterward to restore and build with the new version.

## Reverting to upstream packages

Use `-Version` to set the library back to its upstream version:

```powershell
.\Build\Manage-LocalLibraries.ps1 -Library libpalaso -Version 17.0.0
```

Or revert all libraries at once:

```powershell
git checkout Build/SilVersions.props
Remove-Item -Recurse packages/sil.*
.\build.ps1
```

## Supported libraries

| Library | Switch | Path parameter | Version property | Env var fallback |
|---------|--------|---------------|------------------|-----------------|
| liblcm | `-Lcm` | `-LcmPath` | `SilLcmVersion` | `LIBLCM_PATH` |
| libpalaso | `-Palaso` | `-PalasoPath` | `SilLibPalasoVersion` | `LIBPALASO_PATH` |
| chorus | `-Chorus` | `-ChorusPath` | `SilChorusVersion` | `LIBCHORUS_PATH` |

## See Also

- [Dependencies](dependencies.md) — overview of external dependencies
- [Build Instructions](../../.github/instructions/build.instructions.md) — building FieldWorks
