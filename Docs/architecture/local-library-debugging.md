# Local Library Debugging

This document describes how to debug locally-modified versions of **liblcm**, **libpalaso**, **chorus**, or **machine** (SIL.Machine) in FieldWorks using a local NuGet feed.

## Overview

The workflow uses a single PowerShell script (`Build/Manage-LocalLibraries.ps1`) that:

1. Packs into `.localfeed` in this working tree and records that feed path in `SilVersions.props`.
2. Runs `dotnet pack` in Debug configuration with symbols, letting the library use its own version.
3. Detects the version from the produced packages.
4. Updates `SilVersions.props` so FieldWorks resolves that exact version.
5. Places `.nupkg` / `.snupkg` in your local NuGet feed folder.
6. Copies PDB files to `Output/Debug/` and `Downloads/` for debugger access.
7. Clears stale cached packages so the next restore picks up the local build.

This approach works identically for all three libraries.

## Setup (one-time)

### 1. Clone the library you need

```powershell
git clone https://github.com/sillsdev/liblcm.git
git clone https://github.com/sillsdev/libpalaso.git
git clone https://github.com/sillsdev/chorus.git
git clone https://github.com/sillsdev/machine.git
```

### 2. Nothing else

Packed packages go to `.localfeed` inside this working tree, which is created on
demand and gitignored. No environment variable and no user-level NuGet source are
needed: `Build/SilVersions.props` carries the feed path, and
`Build/PackageRestore.targets` imports it, so even a nested restore in its own
process finds it. Set `LOCAL_NUGET_REPO` if you would rather share one feed across
checkouts; it still wins.

## One command

```powershell
$env:SILMACHINE_PATH = "C:\Repos\machine"
.uild.ps1 -LocalLibraries machine
```

That packs the library and then builds. It is a wrapper over the two steps below:
the version is pinned in tracked `Build/SilVersions.props` exactly as when the pack
script is run by hand, and clearing that pin stays a manual step.

## Pack a local library

```powershell
# Single library — explicit path
.\Build\Manage-LocalLibraries.ps1 -Palaso -PalasoPath C:\Repos\libpalaso

# Multiple libraries (libpalaso is always packed first)
.\Build\Manage-LocalLibraries.ps1 -Palaso -PalasoPath C:\Repos\libpalaso -Chorus -ChorusPath C:\Repos\chorus
```

Or set environment variables so you can omit the paths:

```powershell
$env:LIBPALASO_PATH  = "C:\Repos\libpalaso"
$env:LIBLCM_PATH     = "C:\Repos\liblcm"
$env:LIBCHORUS_PATH  = "C:\Repos\chorus"
$env:SILMACHINE_PATH = "C:\Repos\machine"

# Switches still required — env vars only provide the path
.\Build\Manage-LocalLibraries.ps1 -Palaso -Chorus
```

The script:
- Stamps the pack with the source state, so the version can never be one a
  published package already uses (see Version stamping below).
- Detects the produced version and updates `Build/SilVersions.props` to match.
- Produces `.snupkg` symbol packages (same format as production).
- Copies PDB files to `Output/Debug/` and `Downloads/` for the debugger.
- Clears stale packages from the `packages/` cache.

## Version stamping

A local pack is never given the published version string. It is stamped from the
library checkout, so `3.9.2` packs as, for example:

```
3.9.2-my-branch.d3b7643    committed
3.9.2-my-branch.dirty      uncommitted changes present
```

NuGet keys an extracted package on (id, version) and, once it has unpacked one
into `packages/`, never consults the `.nupkg` again. A local build sharing the
published version therefore kept satisfying restores after its `.nupkg` was
deleted, silently, for as long as the folder survived. A stamped version cannot
collide, so an ordinary build resolves the published package again (LT-22728).

The stamp also makes the pin in `SilVersions.props` self-describing: a version
with a branch and commit in it is visibly not something anyone can restore from
nuget.org.

## Build FieldWorks

```powershell
.\build.ps1
```

The build prints a yellow message listing any local packages in the feed. NuGet restore will use your local packages because `SilVersions.props` was updated to request the exact version produced by the library.

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

When the library change is released, set the pin to the released version rather
than reverting. The dirty `SilVersions.props` is the reminder that FieldWorks is
still depending on something unpublished.

Or revert all libraries at once:

```powershell
git checkout Build/SilVersions.props
Remove-Item -Recurse packages/sil.*
.\build.ps1
```

If an older setup registered a user-level NuGet source, remove it too:

```powershell
dotnet nuget remove source local
```

## Supported libraries

| Library | Switch | Path parameter | Version property | Env var fallback |
|---------|--------|---------------|------------------|-----------------|
| liblcm | `-Lcm` | `-LcmPath` | `SilLcmVersion` | `LIBLCM_PATH` |
| libpalaso | `-Palaso` | `-PalasoPath` | `SilLibPalasoVersion` | `LIBPALASO_PATH` |
| chorus | `-Chorus` | `-ChorusPath` | `SilChorusVersion` | `LIBCHORUS_PATH` |
| machine | `-Machine` | `-MachinePath` | `SilMachineVersion` | `SILMACHINE_PATH` |

## See Also

- [Dependencies](dependencies.md) — overview of external dependencies
- [Build Instructions](../../.github/instructions/build.instructions.md) — building FieldWorks
