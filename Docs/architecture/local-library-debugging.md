# Local Library Debugging

Use `build.ps1 -LocalLibraries` to rebuild locally modified SIL libraries and
use them for one FieldWorks build. A later build that does not select a library
automatically removes its local packages and restores the published version.

## One-time setup

Packed packages go to `.localfeed` inside this working tree, so no feed setup is
needed. The feed is per working tree on purpose: a machine-wide folder lets one
working tree's build delete packages another working tree just produced.
`LOCAL_NUGET_REPO` still overrides the location when it is set, but sharing one
folder across working trees reintroduces that problem.

Set the path variable for each local checkout you use:

```powershell
$env:LIBPALASO_PATH  = "C:\Repos\libpalaso"
$env:LIBLCM_PATH     = "C:\Repos\liblcm"
$env:LIBCHORUS_PATH  = "C:\Repos\chorus"
$env:SILMACHINE_PATH = "C:\Repos\machine"
$env:L10NSHARP_PATH  = "C:\Repos\L10NSharp"
```

## Build with local libraries

Name every local library that this invocation should use:

```powershell
# Rebuild and use Machine locally for this build.
.\build.ps1 -LocalLibraries machine

# Rebuild and use Palaso and Chorus locally for this build.
.\build.ps1 -LocalLibraries palaso,chorus
```

Every selected library is repacked from its configured checkout. The packed
versions are passed to restore and MSBuild without modifying
`Build/SilVersions.props`. The local feed is a restore source only for that
invocation.

Each pack is versioned from the checkout it came from, as
`<base>-<branch>.<commit>` — for example `3.9.2-docs-hc-llm-guide.9358825`. This
matters because NuGet resolves an already-extracted `(id, version)` in
`packages/` before it consults a folder feed: if a local pack reused the
published version string, restore would silently serve whichever copy landed
first. Deriving the version from the branch and commit makes the two
distinguishable, and makes repacking the same commit a correct cache hit.

Each selected library is built before it is packed, because a package may
include output from a target framework its own project does not build.

The core comes from the library's own GitVersion where it has one, so a version
bump in the library is reflected in the local package. A library without
GitVersion, such as SIL.Machine, falls back to the version FieldWorks consumes.

Because a clean commit identifies its contents, a second build from the same
commit reuses the package already in the feed instead of repacking it.

A checkout with uncommitted changes has no stable identity, so it is packed as
`<base>-<branch>.dirty` and repacked on every build. The build lists the paths
responsible, because committing or removing them is what restores the faster
path. Untracked files count, which means anything `.gitignore` does not cover:
a stray note beside the source is enough to keep repacking.

An ordinary build selects no local libraries:

```powershell
.\build.ps1
```

Before restore, every build removes managed packages from the local feed and
removes managed cache versions whose NuGet metadata identifies a filesystem
source. Published packages restored from an HTTP source stay cached, so an
ordinary build does not redownload them every time. Package sources inherited
from user-level configuration are left alone, because a local build adds its own
feed for that build only.

The selected versions and that feed are written to a generated
`Build/LocalLibraries.props`, which `Build/SilVersions.props` imports. They are
written to a file rather than passed on the command line because a restore
launched through `Exec` is a new MSBuild process and does not inherit global
properties, so a version passed that way would not reach it.

## Debug and iterate

The local build copies PDBs to `Output/Debug/` and `Downloads/`. If Visual
Studio reports that symbols are not loaded, disable **Debug > Options > Enable
Just My Code**.

After each local-library change, rerun `build.ps1` with the same
`-LocalLibraries` selection. Omit a library whenever FieldWorks should return to
its published package.

## Set an explicit published version

The lower-level script still supports changing `SilVersions.props` deliberately:

```powershell
.\Build\Manage-LocalLibraries.ps1 -Library palaso -Version 17.0.0
```

Local packing through `Manage-LocalLibraries.ps1` is build-internal. Run
`build.ps1 -LocalLibraries` instead.

## Supported libraries

| Library | Selection | Version property | Checkout environment variable |
|---------|-----------|------------------|-------------------------------|
| libpalaso | `palaso` | `SilLibPalasoVersion` | `LIBPALASO_PATH` |
| L10NSharp | `l10nsharp` | `L10NSharpVersion` | `L10NSHARP_PATH` |
| liblcm | `lcm` | `SilLcmVersion` | `LIBLCM_PATH` |
| chorus | `chorus` | `SilChorusVersion` | `LIBCHORUS_PATH` |
| Machine | `machine` | `SilMachineVersion` | `SILMACHINE_PATH` |

## See also

- [Dependencies](dependencies.md)
- [Build Instructions](../../.github/instructions/build.instructions.md)
