---
applyTo: "FLExInstaller/**"
name: "installer.instructions"
description: "FieldWorks installer (WiX) development guidelines"
---
# Installer development guidelines (WiX)

## Purpose & Scope
Guidance for the installer project, packaging configuration, and localization of installer strings.

## Context loading
- Only build the installer when changing installer logic or packaging; prefer app/library builds in inner loop.
- Review `FLExInstaller/` and related `.wxs/.wixproj` files; confirm WiX 3.14.x tooling.
- See `Docs/installer-build-guide.md` for local build instructions and CI workflow details.

## Quick Setup
```powershell
# Validate installer build prerequisites
.\Build\Agent\Setup-InstallerBuild.ps1 -ValidateOnly

# Set up for patch builds (downloads base artifacts)
.\Build\Agent\Setup-InstallerBuild.ps1 -SetupPatch

# Build base installer
msbuild Build/Orchestrator.proj /t:BuildInstaller /p:Configuration=Debug /p:Platform=x64 /p:config=release /m

# Build patch installer (after -SetupPatch)
msbuild Build/Orchestrator.proj /t:BuildPatchInstaller /p:Configuration=Debug /p:Platform=x64 /p:config=release /m
```

## Deterministic requirements
- Versioning: Maintain consistent ProductCode/UpgradeCode policies; ensure patches use higher build numbers than bases.
- Components/Features: Keep component GUID stability; avoid reshuffling that breaks upgrades.
- Files: Use build outputs; avoid hand-copying artifacts.
- Localization: Ensure installer strings align with repository localization patterns.

## Structured output
- Always validate a local installer build when touching installer config.
- Keep changes minimal and documented in commit messages.

## References
- Build: See `Build/Installer.targets` and top-level build scripts.
- CI: Patch/base installer workflows live under `.github/workflows/`.
- Setup: `Build/Agent/Setup-InstallerBuild.ps1` for environment validation.
