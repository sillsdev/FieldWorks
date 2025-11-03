---
applyTo: "**/*"
description: "FieldWorks build guidelines and inner-loop tips"
---
# Build guidelines and inner-loop tips

## Context loading
- Always initialize the environment when using scripts: `source ./environ`.
- Prefer top-level build scripts or FW.sln to avoid dependency misconfiguration.

## Deterministic requirements
- Inner loop: Use incremental builds; avoid full clean unless necessary.
- Choose the right path:
  - Scripts: `bash ./agent-build-fw.sh` mirrors CI locally.
  - MSBuild: `msbuild FW.sln /m /p:Configuration=Debug`.
- Installer: Skip unless you change installer logic.

## Structured output
- Keep build logs for failures; scan for first error.
- Don’t modify `Build/` targets lightly; coordinate changes.

## References
- `.github/workflows/` for CI steps
- `Build/` for targets/props and build infrastructure
