# FLExInstaller (WiX v6)

This folder contains the WiX Toolset v6 installer sources for FieldWorks (FLEx).

## Build (local)

Preferred entrypoint:

```powershell
# Debug/x64 by default
.\build.ps1 -BuildInstaller

# When iterating only on WiX authoring, skip rebuilding FieldWorks and reuse
# Output/<Config> binaries (requires a prior full build stamp in that config):
.\build.ps1 -BuildInstaller -InstallerOnly

# Release
.\build.ps1 -BuildInstaller -Configuration Release
```

Equivalent MSBuild entrypoint (what `build.ps1` dispatches to):

```powershell
msbuild Build\Orchestrator.proj /t:BuildInstaller /p:Configuration=Debug /p:Platform=x64 /p:config=release
```

Notes:

- WiX v6 is acquired via NuGet restore (SDK-style `.wixproj`); no WiX 3.x install (candle/light/heat on PATH) is required.
- Legacy WiX 3 batch scripts under `Shared/Base/*.bat` are intentionally stubbed and should not be used.

### Faster installer-only iteration

`build.ps1` normally builds FieldWorks first and then builds the installer. If you're only changing WiX authoring and the program outputs are already built, use:

```powershell
.\build.ps1 -BuildInstaller -InstallerOnly
```

Safety behavior:

- `-InstallerOnly` requires that a prior full build has created `Output/<Config>/BuildStamp.json`.
- It refuses to run if the git HEAD differs from the stamp, or if you have uncommitted changes outside `FLExInstaller/**` (to avoid accidentally packaging stale binaries).
- If you really want to override that safety check, add `-ForceInstallerOnly`.

### Debug build speed knobs

Debug builds default to a faster installer/bundle configuration intended for inner-loop development. Release builds are unchanged.

- `FastInstallerBuild` (MSI): disables MSI compression, enables a cabinet cache, and suppresses validation to reduce build time.
- `FastBundleBuild` (bundles): sets bundle chain packages to `Compressed="no"` so payloads are not embedded/compressed into the bundle exe.
  - This makes rebuilds much faster, but the resulting `FieldWorksBundle.exe` / `FieldWorksOfflineBundle.exe` are not “single-file” artifacts; keep the staged payload files next to the bundle when testing locally.

Override either flag via `build.ps1` using `-MsBuildArgs`:

```powershell
# Use normal (slower) MSI build behavior in Debug
.\build.ps1 -BuildInstaller -MsBuildArgs '/p:FastInstallerBuild=0'

# Use normal (slower) bundle embedding/compression in Debug
.\build.ps1 -BuildInstaller -MsBuildArgs '/p:FastBundleBuild=0'
```

## Outputs

Build outputs go under `FLExInstaller/bin/<platform>/<configuration>/`.

- MSI and MSI symbols (culture-specific):
  - `FLExInstaller/bin/x64/<Config>/en-US/FieldWorks.msi`
  - `FLExInstaller/bin/x64/<Config>/en-US/FieldWorks.wixpdb`
- Bundle and bundle symbols:
  - `FLExInstaller/bin/x64/<Config>/FieldWorksBundle.exe`
  - `FLExInstaller/bin/x64/<Config>/FieldWorksBundle.wixpdb`

## Validation (non-installing)

There is a small test that validates installer artifacts exist and checks MSI `Property` table values.

```powershell
# Build the installer first (so artifacts exist)
.\build.ps1 -BuildInstaller

# Run only the installer-artifact test category
.\test.ps1 -TestFilter "TestCategory=InstallerArtifacts" -NoBuild
```

## Key files

- `FieldWorks.Installer.wixproj`: MSI project (SDK-style)
- `FieldWorks.Bundle.wixproj`: Burn bundle project
- `Redistributables.wxi`: prerequisite payload definitions
- `Shared/`: shared WiX fragments and custom action code migrated in-tree
