---
applyTo: ".github/workflows/**,Build/**,scripts/**,*.ps1,*.proj,*.targets,*.props,Directory.Packages.props,FLExInstaller/**"
name: "fieldworks-build-installer-review"
description: "Copilot code review checks for FieldWorks build, CI, dependency, installer, and PowerShell changes"
---

# Build, CI, Dependency, and Installer Review Checks

## Purpose

Use these checks for build scripts, CI workflows, dependency versions, MSBuild
targets, installer inputs, and PowerShell helper changes.

## Build and CI integrity

- Preserve native-before-managed ordering and do not bypass `build.ps1` or
  `test.ps1` for normal validation.
- Verify build/test process cleanup remains scoped to the current worktree and
  does not terminate processes from other worktrees.
- For workflow changes, verify tests run before installer packaging when that
  ordering affects release confidence.
- Flag hardcoded absolute paths, machine-specific assumptions, and hidden
  dependency downloads that are not represented in repo scripts.

## Dependency alignment

- Version changes in `Directory.Packages.props`, `Build/SilVersions.props`,
  app configs, binding redirects, and local-library scripts should agree.
- Dependency updates should explain compatibility with .NET Framework 4.8,
  native x64 requirements, and existing installer/runtime packaging.

## PowerShell and script safety

- Prefer repo wrapper scripts over ad-hoc command pipelines.
- Check error handling, exit-code propagation, quoting of paths with spaces, and
  deterministic evidence output.
- Avoid name-based process termination; use specific process IDs when stopping a
  process is required.

## Installer evidence

- WiX and installer changes should include validation evidence for bundle/MSI
  build, install/upgrade/uninstall behavior, logs, and expected footprint.
- Preserve both WiX 3 and WiX 6 flows unless the change explicitly scopes one
  toolset.
