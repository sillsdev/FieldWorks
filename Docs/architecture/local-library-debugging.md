# Local Library Debugging

Use `build.ps1 -LocalLibraries` to rebuild locally modified SIL libraries and
use them for one FieldWorks build. A later build that does not select a library
automatically removes its local packages and restores the published version.

## One-time setup

Choose a folder for locally packed NuGet packages and set its environment
variable:

```powershell
$env:LOCAL_NUGET_REPO = "C:\localnugetpackages"
[System.Environment]::SetEnvironmentVariable(
	"LOCAL_NUGET_REPO", "C:\localnugetpackages", "User")
```

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

Every selected library is repacked from its configured checkout. The detected
package versions are passed to restore and MSBuild without modifying
`Build/SilVersions.props`. The local feed is a restore source only for that
invocation.

An ordinary build selects no local libraries:

```powershell
.\build.ps1
```

Before restore, every build removes managed packages from `LOCAL_NUGET_REPO`
and removes managed cache versions whose NuGet metadata identifies a filesystem
source. Published packages restored from an HTTP source stay cached, so an
ordinary build does not redownload them every time. Repository `nuget.config`
also ignores package sources inherited from user-level configuration.

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
