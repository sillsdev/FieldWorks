# FLExInstaller

Minimal installer guidance for agents.

## Defaults

- Use `.\build.ps1 -BuildInstaller` for installer builds.
- Validate prerequisites with `.\Build\Agent\Setup-InstallerBuild.ps1 -ValidateOnly`.
- Follow `.github/instructions/installer.instructions.md` for packaging and evidence rules.

## Constraints

- Keep existing WiX 3 and WiX 6 flows intact.
- Do not introduce installer signing or registry behavior changes without explicit requirements.
- Keep installer edits scoped to this folder and related build targets only.

