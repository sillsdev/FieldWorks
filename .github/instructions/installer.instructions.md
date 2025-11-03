---
applyTo: "FLExInstaller/**"
description: "FieldWorks installer (WiX) development guidelines"
---
# Installer development guidelines (WiX)

## Context loading
- Only build the installer when changing installer logic or packaging; prefer app/library builds in inner loop.
- Review `FLExInstaller/` and related `.wxs/.wixproj` files; confirm WiX 3.11.x tooling.

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
