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

Installer builds use **WiX Toolset v6 via NuGet restore** (SDK-style `.wixproj`); no WiX 3.x install (candle/light/heat on PATH) is required.

## Building FieldWorks

FieldWorks uses the **MSBuild Traversal SDK** for declarative, dependency-ordered builds:

**Windows (PowerShell):**
```powershell
.\build.ps1                    # Debug build
.\build.ps1 -Configuration Release
```

For detailed build instructions, see [.github/instructions/build.instructions.md](.github/instructions/build.instructions.md).

## Building Installers (WiX v6)

```powershell
# Build the installer (Debug)
.\build.ps1 -BuildInstaller

# Build the installer (Release)
.\build.ps1 -BuildInstaller -Configuration Release
```

Installer artifacts are produced under `FLExInstaller/bin/x64/<Config>/` (MSI under `en-US/`).

For more details, see [specs/001-wix-v6-migration/quickstart.md](specs/001-wix-v6-migration/quickstart.md).

## Model Context Protocol helpers

This repo ships an `mcp.json` plus PowerShell helpers so MCP-aware editors can spin up
the GitHub and Serena servers automatically. See [Docs/mcp.md](Docs/mcp.md) for
requirements and troubleshooting tips.

## Copilot instruction files

We maintain a human-facing `.github/copilot-instructions.md` plus a small curated set of
`*.instructions.md` files under `.github/instructions/` for prescriptive constraints.

See [.github/AI_GOVERNANCE.md](.github/AI_GOVERNANCE.md) for the documentation taxonomy and “source of truth” rules.

## Recent Changes

**MSBuild Traversal SDK**: FieldWorks now uses Microsoft.Build.Traversal SDK with declarative dependency ordering across 110+ projects organized into 21 build phases. This provides automatic parallel builds, better incremental builds, and clearer dependency management.

**64-bit only + Registration-free COM**: FieldWorks now builds and runs as x64-only with registration-free COM activation. No administrator privileges or COM registration required. See [Docs/64bit-regfree-migration.md](Docs/64bit-regfree-migration.md) for details.

**Unified launcher**: FieldWorks.exe is now the single supported executable. The historical `Flex.exe` stub (LexTextExe) has been removed; shortcuts and scripts should invoke `FieldWorks.exe` directly.
