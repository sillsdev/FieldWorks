## Getting Started

New to FieldWorks development? Start here:

- **[Contributing Guide](docs/CONTRIBUTING.md)** - How to set up your development environment and contribute code
- **[Visual Studio Setup](docs/visual-studio-setup.md)** - Detailed VS 2022 configuration
- **[Core Developer Setup](docs/core-developer-setup.md)** - Additional setup for team members

> **Note**: We are migrating documentation from the [FwDocumentation wiki](https://github.com/sillsdev/FwDocumentation/wiki) into this repository. Some wiki content may be more recent until migration is complete.

## Developer Machine Setup

For first-time setup on a Windows development machine:

```powershell
# Run as Administrator (or User for user-level PATH)
.\Setup-Developer-Machine.ps1
```

This configures a dev machine for builds and tests (verifies prerequisites and configures PATH). Prerequisites:
- Visual Studio 2022 with .NET desktop and C++ desktop workloads
- Git for Windows

Installer builds default to **WiX 3** (legacy batch pipeline) using inputs in `FLExInstaller/` and `PatchableInstaller/`. The **Visual Studio WiX Toolset v3 extension** is required so `Wix.CA.targets` is available under the MSBuild extensions path. Use `-InstallerToolset Wix6` to opt into the WiX 6 SDK-style path (restored via NuGet).

### WiX 3.14 setup (required for WiX 3 installer builds)

We expect the WiX 3.14 toolset to be installed under:

- `%LOCALAPPDATA%\FieldWorksTools\Wix314`

Required:

- Ensure `candle.exe`, `light.exe`, `heat.exe`, and `insignia.exe` are available. The WiX 3.14 tools are in the **root** of that folder (not a `bin` subfolder).
- Set the `WIX` environment variable to the toolset root (e.g., `%LOCALAPPDATA%\FieldWorksTools\Wix314`).
- Add the toolset root to `PATH` (or rerun `Setup-Developer-Machine.ps1` to do it for you).
- Install the **Visual Studio WiX Toolset v3 extension** so `Wix.CA.targets` is available to MSBuild.

## Building FieldWorks

FieldWorks uses the **MSBuild Traversal SDK** for declarative, dependency-ordered builds:

**Windows (PowerShell):**
```powershell
.\build.ps1                    # Debug build
.\build.ps1 -Configuration Release
```

For detailed build instructions, see [.github/instructions/build.instructions.md](.github/instructions/build.instructions.md).

## Building Installers (WiX 3 default, WiX 6 opt-in)

Installer builds include the additional utilities (UnicodeCharEditor, LCMBrowser, MigrateSqlDbs, etc.).
To skip them, pass `-BuildAdditionalApps:$false`.

```powershell
# Build the installer (Debug, WiX 3 default)
.\build.ps1 -BuildInstaller

# Build the installer (Debug, WiX 6)
.\build.ps1 -BuildInstaller -InstallerToolset Wix6

# Build the installer (Release, WiX 3 default)
.\build.ps1 -BuildInstaller -Configuration Release

# Build the installer (Release, WiX 6)
.\build.ps1 -BuildInstaller -Configuration Release -InstallerToolset Wix6
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

This repo ships an `mcp.json` plus PowerShell helpers so MCP-aware editors can spin up
the GitHub and Serena servers automatically. See [Docs/mcp.md](Docs/mcp.md) for
requirements and troubleshooting tips.

## Agent instruction files

We maintain a human-facing `.github/AGENTS.md` plus a small curated set of
`*.instructions.md` files under `.github/instructions/` for prescriptive constraints.

See [.github/AI_GOVERNANCE.md](.github/AI_GOVERNANCE.md) for the documentation taxonomy and “source of truth” rules.

## Recent Changes

**MSBuild Traversal SDK**: FieldWorks now uses Microsoft.Build.Traversal SDK with declarative dependency ordering across 110+ projects organized into 21 build phases. This provides automatic parallel builds, better incremental builds, and clearer dependency management.

**64-bit only + Registration-free COM**: FieldWorks now builds and runs as x64-only with registration-free COM activation. No administrator privileges or COM registration required. See [Docs/64bit-regfree-migration.md](Docs/64bit-regfree-migration.md) for details.

**Unified launcher**: FieldWorks.exe is now the single supported executable. The historical `Flex.exe` stub (LexTextExe) has been removed; shortcuts and scripts should invoke `FieldWorks.exe` directly.
