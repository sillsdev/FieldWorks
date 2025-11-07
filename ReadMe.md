Developer documentation for FieldWorks can be found here: (https://github.com/sillsdev/FwDocumentation/wiki)

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

## Recent Changes

**MSBuild Traversal SDK**: FieldWorks now uses Microsoft.Build.Traversal SDK with declarative dependency ordering across 110+ projects organized into 21 build phases. This provides automatic parallel builds, better incremental builds, and clearer dependency management.

**64-bit only + Registration-free COM**: FieldWorks now builds and runs as x64-only with registration-free COM activation. No administrator privileges or COM registration required. See [Docs/64bit-regfree-migration.md](Docs/64bit-regfree-migration.md) for details.
