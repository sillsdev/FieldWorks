# Per-build local library selection

## Problem

`Build/Manage-LocalLibraries.ps1` turns a one-build choice into persistent
machine and checkout state. It registers a user-level NuGet source, writes the
selected version into `Build/SilVersions.props`, and leaves the restored package
under `packages/`. NuGet identifies a package only by ID and version, so a local
build with the same version as a published package remains valid after its
source `.nupkg` is removed.

The affected path is shared by palaso, lcm, chorus, machine, and l10nsharp.

## User contract

Local selection belongs to one `build.ps1` invocation:

```powershell
.\build.ps1 -LocalLibraries machine
.\build.ps1 -LocalLibraries palaso,chorus
```

The selected libraries are packed before restore and used by that invocation.
Libraries omitted from `-LocalLibraries` are not eligible for restore. A build
without the parameter uses published packages and removes stale local packages
before restore. The local source path and library source paths continue to come
from the existing environment variables.

## Design

`Build/LocalLibraries.psm1` owns the library catalogue and cleanup behavior so
`build.ps1` and `Manage-LocalLibraries.ps1` cannot drift. Cache cleanup reads
each managed package version's `.nupkg.metadata`; only entries whose source is a
filesystem path are removed. Entries restored from HTTP sources remain cached.
Matching packages in `LOCAL_NUGET_REPO` are removed before each build, then the
explicitly selected libraries are repacked.

`Manage-LocalLibraries.ps1` gains an invocation mode that reports the detected
version properties without editing `Build/SilVersions.props`. `build.ps1`
passes those versions as global MSBuild properties to both restore and build.
This keeps the tracked version file clean and makes interruption safe.

The repository NuGet configuration clears inherited package sources. Ordinary
builds therefore use the repository's published source even if an older run
registered a user-level source named `local`. A local build adds
`LOCAL_NUGET_REPO` to its restore command only for that invocation.

## Failure behavior

An explicit local selection fails before restore when its source environment
variable or `LOCAL_NUGET_REPO` is missing, its checkout does not exist, packing
fails, or its packages do not share one version. Cleanup tolerates absent feed
and cache directories. Invalid cache metadata is left in place and reported as
a warning rather than guessed to be local.

## Verification

A fixture-based PowerShell test creates temporary local and published cache
entries and proves that cleanup removes only local entries and managed feed
packages for all five library groups. Script compatibility, the repository
build, and the repository test entry point run with `-CommentHygiene`.
