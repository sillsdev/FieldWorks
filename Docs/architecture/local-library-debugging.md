# Local Library Debugging

This document describes how to debug locally-modified versions of **liblcm**, **libpalaso**, or **chorus** in FieldWorks using a local NuGet feed.

## Overview

The workflow uses a single PowerShell script (`Build/Pack-LocalLibrary.ps1`) that:

1. Reads the exact version from `Build/SilVersions.props` — no version edits needed in FieldWorks.
2. Runs `dotnet pack` in Debug configuration with symbols.
3. Places `.nupkg` / `.snupkg` in your local NuGet feed folder.
4. Copies PDB files to `Output/Debug/` and `Downloads/` for debugger access.
5. Clears stale cached packages so the next restore picks up the local build.

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
.\Build\Pack-LocalLibrary.ps1 -Library libpalaso -SourcePath C:\Repos\libpalaso
```

Or set environment variables so you can omit `-SourcePath`:

```powershell
$env:LIBPALASO_PATH = "C:\Repos\libpalaso"
$env:LIBLCM_PATH    = "C:\Repos\liblcm"
$env:LIBCHORUS_PATH = "C:\Repos\chorus"

.\Build\Pack-LocalLibrary.ps1 -Library libpalaso
```

The script:
- Packs with the version from `Build/SilVersions.props`, so the local package shadows the upstream one.
- Produces `.snupkg` symbol packages (same format as production).
- Copies PDB files to `Output/Debug/` and `Downloads/` for the debugger.
- Clears stale packages from the `packages/` cache.

## Build FieldWorks

```powershell
.\build.ps1
```

The build will print a yellow message listing any local packages detected in `LOCAL_NUGET_REPO`. NuGet restore will prefer your local packages because they have the same version as the upstream ones.

## Debug

1. Open FieldWorks in Visual Studio.
2. PDB files are already in `Output/Debug/` — the debugger will find them automatically.
3. If breakpoints show "No symbols loaded", disable **Debug > Options > Enable Just My Code**.
4. You can also open the library solution side-by-side and use **Debug > Attach to Process**.

## Iterating

After each change to the library:

1. Re-run `Pack-LocalLibrary.ps1` (~30-60 seconds).
2. Re-run `.\build.ps1`.

## Reverting to upstream packages

1. Delete the library's `.nupkg` files from `LOCAL_NUGET_REPO`.
2. Clear the cached packages:
   ```powershell
   Remove-Item -Recurse packages/sil.*
   ```
3. Run `.\build.ps1` — NuGet will restore from nuget.org.

Alternatively, unset `LOCAL_NUGET_REPO` entirely to disable the local feed.

## Supported libraries

| Library | `-Library` value | Version property | Env var for source path |
|---------|-----------------|------------------|------------------------|
| liblcm | `liblcm` | `SilLcmVersion` | `LIBLCM_PATH` |
| libpalaso | `libpalaso` | `SilLibPalasoVersion` | `LIBPALASO_PATH` |
| chorus | `chorus` | `SilChorusVersion` | `LIBCHORUS_PATH` |

## See Also

- [Dependencies](dependencies.md) — overview of external dependencies
- [Build Instructions](../../.github/instructions/build.instructions.md) — building FieldWorks
