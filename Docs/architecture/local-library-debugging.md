# Local Library Debugging

This document describes how to debug locally-modified versions of **liblcm**, **libpalaso**, or **chorus** in FieldWorks using a local NuGet feed.

## Overview

The workflow uses a single PowerShell script (`Build/Pack-LocalLibrary.ps1`) that:

1. Reads the base version from `Build/SilVersions.props` and appends a `-local` pre-release suffix (e.g., `17.0.0` becomes `17.0.0-local`).
2. Updates `SilVersions.props` so FieldWorks resolves the local version — the `-local` version only exists in your local feed, eliminating any ambiguity with upstream packages.
3. Runs `dotnet pack` in Debug configuration with symbols.
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
- Appends `-local` to the version and updates `Build/SilVersions.props` so NuGet unambiguously resolves from the local feed.
- Produces `.snupkg` symbol packages (same format as production).
- Copies PDB files to `Output/Debug/` and `Downloads/` for the debugger.
- Clears stale packages from the `packages/` cache.

## Build FieldWorks

```powershell
.\build.ps1
```

The build will print a yellow message listing any local packages detected in `LOCAL_NUGET_REPO`. NuGet restore will use your local packages because `SilVersions.props` now requests the `-local` version which only exists in your local feed.

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

1. Restore the original version in `SilVersions.props`:
   ```powershell
   git checkout Build/SilVersions.props
   ```
2. Clear the cached packages:
   ```powershell
   Remove-Item -Recurse packages/sil.*
   ```
3. Run `.\build.ps1` — NuGet will restore from nuget.org.

You can also clean up your local feed, but it's not required — the `-local` packages won't be resolved once `SilVersions.props` is reverted.

## Supported libraries

| Library | `-Library` value | Version property | Env var for source path |
|---------|-----------------|------------------|------------------------|
| liblcm | `liblcm` | `SilLcmVersion` | `LIBLCM_PATH` |
| libpalaso | `libpalaso` | `SilLibPalasoVersion` | `LIBPALASO_PATH` |
| chorus | `chorus` | `SilChorusVersion` | `LIBCHORUS_PATH` |

## See Also

- [Dependencies](dependencies.md) — overview of external dependencies
- [Build Instructions](../../.github/instructions/build.instructions.md) — building FieldWorks
