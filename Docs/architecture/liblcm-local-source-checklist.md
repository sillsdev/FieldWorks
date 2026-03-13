# Local liblcm Source Mode Checklist

This checklist tracks the conversion from the staged overlay-first debugging approach to the nested-checkout, two-solution workflow.

## Goals

- Keep the default FieldWorks experience package-backed.
- Make local `liblcm` development explicit and repo-wide, not a `FieldWorks.exe` post-build side effect.
- Use a nested checkout at `Localizations/LCM`.
- Provide a second solution for local `liblcm` work.

## Checklist

- [x] Add a checked-in implementation checklist for the conversion.
- [x] Remove the old FieldWorks-local debug props toggle.
- [x] Add repo-wide `UseLocalLcmSource` support in shared MSBuild props.
- [x] Redirect `LcmArtifactsDir`, `LcmModelArtifactsDir`, `LcmBuildTasksDir`, and `LcmRootDir` to the nested checkout when local source mode is enabled.
- [x] Switch managed `SIL.LCModel*` references centrally from `PackageReference` to `ProjectReference` in local source mode.
- [x] Drive Visual Studio local mode from `FieldWorks.LocalLcm.sln`.
- [x] Add explicit build entrypoints for package/local mode in `build.ps1` and VS Code tasks.
- [x] Change the default local `liblcm` discovery path to `Localizations/LCM`.
- [x] Update setup/bootstrap behavior so worktrees can keep a nested `Localizations/LCM` checkout instead of relying on a shared liblcm junction.
- [x] Add a second solution, `FieldWorks.LocalLcm.sln`, that includes the core `liblcm` projects.

## Notes

- Local source mode bootstraps `SIL.LCModel.Build.Tasks.dll` into `artifacts/<Configuration>/net462` on the first build when it is missing.
- The package-backed path remains the default for `FieldWorks.sln` and for `./build.ps1 -LcmMode Auto` unless local mode is explicitly requested.