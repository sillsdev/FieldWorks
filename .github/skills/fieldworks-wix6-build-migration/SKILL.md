---
name: fieldworks-wix6-build-migration
description: Use this skill for FieldWorks WiX Toolset build migration work: moving PatchableInstaller/genericinstaller behavior into WiX 6, fixing Build/Installer*.targets, SDK-style .wixproj files, InstallerToolset selection, prerequisite download/staging, CI installer builds, signing knobs, artifact paths, and local developer build failures. Trigger whenever the user mentions FieldWorks WiX build, Wix3/Wix6 toolset selection, PatchableInstaller migration, genericinstaller removal, or installer artifacts.
---

# FieldWorks WiX 6 Build Migration

This skill handles build infrastructure and migration mechanics. Keep it separate from UI behavior, runtime diagnostics, and patch/upgrade validation unless those areas are the cause of a build failure.

## Load References When Needed

- Read `references/repo-build-map.md` for FieldWorks-specific targets, projects, commands, and artifact paths.
- Read `references/official-wix-build-notes.md` for WiX Toolset v4/v6 migration and MSBuild facts.

## First Moves

1. Confirm current branch/worktree and whether the task is about WiX 3 default, WiX 6 opt-in, or both.
2. Read `FLExInstaller/AGENTS.md`, `.github/instructions/installer.instructions.md`, and `.github/instructions/build.instructions.md`.
3. Inspect `Build/InstallerBuild.proj` to confirm which target file is imported for `InstallerToolset=Wix3|Wix6`.
4. Inspect `Build/Installer.targets`, `Build/Installer.Wix3.targets`, and relevant `.wixproj` files before editing.
5. Search for active references to `PatchableInstaller`, `genericinstaller`, `candle.exe`, `light.exe`, `insignia.exe`, and `heat.exe`; classify each as WiX 3 legacy, WiX 6 active, or documentation/reference.

## FieldWorks Build Rules

- Default installer build is WiX 3: `./build.ps1 -BuildInstaller`.
- WiX 6 is opt-in: `./build.ps1 -BuildInstaller -InstallerToolset Wix6`.
- Do not make WiX 6 schema changes in `FLExInstaller/` root files. Keep WiX 6 authoring under `FLExInstaller/wix6/`.
- Do not reintroduce a `genericinstaller` submodule. If old behavior is needed, migrate the specific source/config into the repo and document why.
- Do not remove or break the WiX 3 path during the transition.
- Use pinned repo package versions, not whatever WiX happens to be globally installed.
- WiX 6 projects should be SDK-style and use WiX extension `PackageReference`s such as Bal, Util, NetFx, UI, and Heat as appropriate.
- WiX v6 source uses `http://wixtoolset.org/schemas/v4/wxs`; there is no v6 schema namespace.

## PatchableInstaller Migration Workflow

When moving behavior out of the old generic installer/PatchableInstaller model:

1. Identify the old source of behavior in `PatchableInstaller/`, `Build/Installer.legacy.targets`, or WiX 3 includes.
2. Find the WiX 6 destination: `FLExInstaller/wix6/Shared/Base`, `Shared/Common`, `Shared/CustomActions`, `Shared/ProcRunner`, or a `.wixproj` target.
3. Port only the needed behavior. Avoid copying whole legacy scripts or batch-driven assumptions into the WiX 6 path.
4. Convert old command-line parameters into MSBuild properties and `DefineConstants` rather than batch variables.
5. Add validation that fails loudly when required payloads or prerequisites are missing.
6. Update specs/docs only when behavior or build commands change.

## Build Failure Workflow

1. Run or ask for the exact command and configuration, including `InstallerToolset`.
2. Validate prerequisites with `./Build/Agent/Setup-InstallerBuild.ps1 -ValidateOnly` when the failure smells environmental.
3. If WiX 6 fails, inspect `FieldWorks.Installer.wixproj`, `FieldWorks.Bundle.wixproj`, and `FieldWorks.OfflineBundle.wixproj` before touching MSBuild targets.
4. For WiX errors, map the error to authoring/project/tooling. Do not hide warnings by suppression unless the repo already documents that suppression.
5. Preserve `.wixpdb` outputs; they are useful for diagnostics and future patch baselines.

## Validation

Prefer these checks, scaled to the change:

- `./Build/Agent/Setup-InstallerBuild.ps1 -ValidateOnly`
- `./build.ps1 -BuildInstaller -InstallerToolset Wix6 -Configuration Debug`
- `./build.ps1 -BuildInstaller -InstallerToolset Wix6 -Configuration Release` for release-path changes
- Confirm artifacts under `FLExInstaller/wix6/bin/x64/<Configuration>/` and `en-US/`.
- For CI or pre-commit work, use the repo VS Code task `CI: Full local check` when appropriate.

## Output Expectations

When reporting a build migration result, include:

- The active toolset and entry point.
- The target/project files touched.
- The legacy behavior replaced or isolated.
- The artifacts expected and where they land.
- The validation run and remaining risk.
