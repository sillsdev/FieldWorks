# GitHub Copilot Coding Agent Instructions

This file provides guidance for the **GitHub Copilot coding agent** when working on FieldWorks.

> **For VS Code Copilot Chat**: See `.github/copilot-instructions.md` for local IDE guidance.

## Quick Start

FieldWorks is a Windows-first linguistics suite. The coding agent runs on `windows-latest` GitHub runners.

### Environment Setup

The coding agent uses `.github/workflows/copilot-setup-steps.yml` which:
1. Relies on pre-installed software (VS 2022, .NET Framework 4.8.1, WiX 3.14.x, etc.)
2. Configures build environment via `Build/Agent/Setup-FwBuildEnv.ps1`
3. Verifies dependencies via `Build/Agent/Verify-FwDependencies.ps1`
4. Optionally sets up Serena MCP for code intelligence

### Build Commands

```powershell
# Full traversal build (recommended)
.\build.ps1

# Direct MSBuild
msbuild FieldWorks.proj /p:Configuration=Debug /p:Platform=x64 /m

# Native C++ only (must build before managed code)
msbuild Build\Src\NativeBuild\NativeBuild.csproj /p:Configuration=Debug /p:Platform=x64
```

### Test Commands

```powershell
# Run all tests via MSBuild
msbuild FieldWorks.proj /p:Configuration=Debug /p:Platform=x64 /p:action=test

# Run specific test project
dotnet test Src/<Component>.Tests/<Component>.Tests.csproj
```

## Key Constraints

### Build Order Matters
Native C++ (Phase 2) must build before managed code (Phases 3+). The traversal build in `FieldWorks.proj` handles this automatically.

### COM/Registry Isolation
Changes affecting COM registration or the Windows registry must:
- Include appropriate tests
- Work inside Docker containers (see `scripts/spin-up-agents.ps1`)
- Never assume host machine registry state

### Localization
- Use `.resx` files for localizable strings
- Never hardcode user-facing text
- Respect `crowdin.json` integration

## File Guidance

| Path Pattern | Guidance |
|--------------|----------|
| `Src/**/*.cs` | Follow `.github/instructions/managed.instructions.md` |
| `Src/**/*.cpp`, `*.h` | Follow `.github/instructions/native.instructions.md` |
| `FLExInstaller/**` | Follow `.github/instructions/installer.instructions.md` |
| `Build/**` | Change sparingly; affects all builds |
| `.github/workflows/**` | Test locally before pushing |

## Per-Folder Documentation

Most `Src/` folders contain a `COPILOT.md` file describing:
- Component purpose and public API
- Dependencies and dependents
- Testing requirements
- Recent changes

Always read the relevant `COPILOT.md` before modifying a folder.

## Code Style

- Follow `.editorconfig` settings
- Match existing patterns in the file/folder
- Use `FIXME(<topic>)` comments for uncertainties
- Run `.\Build\Agent\check-and-fix-whitespace.ps1` before committing

## Validation Before PR

1. Build succeeds: `.\build.ps1`
2. Relevant tests pass
3. Whitespace check: `.\Build\Agent\check-and-fix-whitespace.ps1`
4. Commit messages: `.\Build\Agent\commit-messages.ps1`

## Additional Resources

- **Instructions**: `.github/instructions/*.instructions.md`
- **Prompts**: `.github/prompts/*.prompt.md`
- **Source Catalog**: `.github/src-catalog.md`
- **Build Details**: `.github/instructions/build.instructions.md`
- **Testing**: `.github/instructions/testing.instructions.md`
