## Getting Started

New to FieldWorks development? Start here:

- **[Contributing Guide](docs/CONTRIBUTING.md)** - How to set up your development environment and contribute code
- **[VS Code Stability Profile](Docs/vscode-stability-profile.md)** - ReSharper-first VS Code setup and when to switch to Visual Studio
- **[Visual Studio Setup](docs/visual-studio-setup.md)** - Detailed VS 2022 configuration
- **[Core Developer Setup](docs/core-developer-setup.md)** - Additional setup for team members

> **Note**: We are migrating documentation from the [FwDocumentation wiki](https://github.com/sillsdev/FwDocumentation/wiki) into this repository. Some wiki content may be more recent until migration is complete.

## Developer Machine Setup

For first-time setup on a Windows development machine:

- Install required software:
	- Visual Studio 2022 with .NET desktop and C++ desktop workloads
	- Git for Windows
- Run the setup script:
```powershell
# Run as Administrator (or User for user-level PATH)
.\Setup-Developer-Machine.ps1
```

This configures a dev machine for builds and tests (verifies prerequisites and configures PATH).

## Building FieldWorks

FieldWorks uses the **MSBuild Traversal SDK** for declarative, dependency-ordered builds:

**Windows (PowerShell):**
```powershell
.\build.ps1                    # Debug build
.\build.ps1 -Configuration Release
```

For detailed build instructions, see [.github/instructions/build.instructions.md](.github/instructions/build.instructions.md).

### Concurrent worktree builds/tests

`build.ps1` and `test.ps1` use worktree-aware process cleanup, so running scripted builds/tests in different git worktrees does not kill each other.

Within a single worktree, builds and tests run one at a time: scripts acquire a worktree lock and fail fast if another scripted workflow is active.

You can tag lock ownership for diagnostics with `FW_BUILD_STARTED_BY=user|agent` (or `-StartedBy user|agent`).

## Building Installers (WiX 3 default, WiX 6 opt-in)

Installer builds default to **WiX 3** (legacy batch pipeline) using inputs in `FLExInstaller/` and `PatchableInstaller/`. The **Visual Studio WiX Toolset v3 extension** is required so `Wix.CA.targets` is available under the MSBuild extensions path. Use `-InstallerToolset Wix6` to opt into the WiX 6 SDK-style path (restored via NuGet).

### WiX 3.14 setup (required for WiX 3 installer builds)

We expect the WiX 3.14 toolset to be installed under:

- `%LOCALAPPDATA%\FieldWorksTools\Wix314`

Required:

- Ensure `candle.exe`, `light.exe`, `heat.exe`, and `insignia.exe` are available. The WiX 3.14 tools are in the **root** of that folder (not a `bin` subfolder).
- Set the `WIX` environment variable to the toolset root (e.g., `%LOCALAPPDATA%\FieldWorksTools\Wix314`).
- Add the toolset root to `PATH` (or rerun `Setup-Developer-Machine.ps1` to do it for you).
- Install the **Visual Studio WiX Toolset v3 extension** so `Wix.CA.targets` is available to MSBuild.

### Running installer builds

Installer builds include the additional utilities (UnicodeCharEditor, LCMBrowser, MigrateSqlDbs, etc.).
To skip them, pass `-BuildAdditionalApps:$false`.

```powershell
# Build the installer (WiX 3 default)
.\build.ps1 -BuildInstaller

# Build the installer (WiX 6)
.\build.ps1 -BuildInstaller -InstallerToolset Wix6
```

WiX 3 artifacts are produced under `FLExInstaller/bin/x64/<Config>/` (MSI under `en-US/`).

WiX 6 artifacts are produced under `FLExInstaller/wix6/bin/x64/<Config>/` (MSI under `en-US/`).

For more details, see [specs/001-wix-v6-migration/quickstart.md](specs/001-wix-v6-migration/quickstart.md).

### Code signing for local installer builds

Signing is optional for local builds. By default, local installer builds do not sign and instead record files to sign later.

To enable signing for a local installer build, pass `-SignInstaller` to build.ps1.

Required for signing:

- Either `sign` (SIL signing tool) **or** `signtool.exe` on `PATH`.
- If using `signtool.exe`, set `CERTPATH` (PFX path) and `CERTPASS` (password) in the environment.

If you want to control the capture file, set `FILESTOSIGNLATER` to a file path before building. The build will append files needing signatures to that file.

## Model Context Protocol helpers

This repo ships an `mcp.json` workspace configuration so MCP-aware editors can spin up
the GitHub and Serena servers automatically. See [Docs/mcp.md](Docs/mcp.md) for
requirements and troubleshooting tips.

## Agent instruction files

We maintain a minimal AGENTS model (`AGENTS.md`, `.github/AGENTS.md`,
`Src/AGENTS.md`, `FLExInstaller/AGENTS.md`, `openspec/AGENTS.md`) and a
minimal, requirement-focused guidance model.

Prescriptive constraints remain under `.github/instructions/*.instructions.md`.

See [.github/AI_GOVERNANCE.md](.github/AI_GOVERNANCE.md) for the documentation taxonomy and “source of truth” rules.