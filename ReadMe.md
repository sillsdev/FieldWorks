Developer documentation for FieldWorks can be found here: (https://github.com/sillsdev/FwDocumentation/wiki)

## Developer Machine Setup

For first-time setup on a Windows development machine:

```powershell
# Run as Administrator (or User for user-level PATH)
.\Setup-Developer-Machine.ps1
```

This installs WiX Toolset, LLVM/clangd, OmniSharp, and configures PATH. Prerequisites:
- Visual Studio 2022 with .NET desktop and C++ desktop workloads
- Git for Windows

## Building FieldWorks

FieldWorks uses the **MSBuild Traversal SDK** for declarative, dependency-ordered builds:

**Windows (PowerShell):**
```powershell
.\build.ps1                    # Debug build
.\build.ps1 -Configuration Release
```

**Linux/macOS (Bash):**
```bash
./build.sh                     # Debug build
./build.sh -c Release
```

For detailed build instructions, see [.github/instructions/build.instructions.md](.github/instructions/build.instructions.md).

## Model Context Protocol helpers

This repo ships an `mcp.json` plus PowerShell helpers so MCP-aware editors can spin up
the GitHub and Serena servers automatically. See [Docs/mcp.md](Docs/mcp.md) for
requirements and troubleshooting tips.

## Copilot instruction files

We maintain both a human-facing `.github/copilot-instructions.md` and a set of
short `*.instructions.md` files under `.github/instructions/` for Copilot code review.
Use `scripts/tools/validate_instructions.py` locally or the `Validate instructions` CI job
to ensure instruction files follow conventions.

## Recent Changes

**MSBuild Traversal SDK**: FieldWorks now uses Microsoft.Build.Traversal SDK with declarative dependency ordering across 110+ projects organized into 21 build phases. This provides automatic parallel builds, better incremental builds, and clearer dependency management.

**64-bit only + Registration-free COM**: FieldWorks now builds and runs as x64-only with registration-free COM activation. No administrator privileges or COM registration required. See [Docs/64bit-regfree-migration.md](Docs/64bit-regfree-migration.md) for details.

**Unified launcher**: FieldWorks.exe is now the single supported executable. The historical `Flex.exe` stub (LexTextExe) has been removed; shortcuts and scripts should invoke `FieldWorks.exe` directly.
