---
last-reviewed: 2025-10-30
last-verified-commit: 9611cf70e
status: draft
---

# GenerateHCConfig

## Purpose
Build-time utilities for generating help system configuration files.
Creates configuration artifacts and metadata files needed by the FieldWorks help system.
Typically runs during the build process to prepare help content for deployment.

## Architecture
C# library with 6 source files.

## Key Components
No major public classes identified.

## Technology Stack
- C# .NET
- Build-time code generation
- Help system configuration

## Dependencies
- Depends on: Minimal build utilities
- Used by: Build process to generate help configurations

## Interop & Contracts
No explicit interop boundaries detected. Pure managed or native code.

## Threading & Performance
Threading model: UI thread marshaling.

## Config & Feature Flags
Config files: App.config.

## Build Information
- C# executable project
- Runs during build process
- Build with MSBuild or Visual Studio

## Interfaces and Data Models
See code analysis sections above for key interfaces and data models. Additional interfaces may be documented in source files.

## Entry Points
- Command-line tool invoked during build
- Generates configuration files for help system

## Test Index
No tests found in this folder. Tests may be in a separate Test folder or solution.

## Usage Hints
Console application. Build and run via command line or Visual Studio. See Entry Points section.

## Related Folders
- **Build/** - Build scripts that invoke GenerateHCConfig
- **DistFiles/** - May contain generated help configuration output
- **FwResources/** - May work with resource files

## References

- **Project files**: GenerateHCConfig.csproj
- **Target frameworks**: net462
- **Key C# files**: AssemblyInfo.cs, ConsoleLogger.cs, NullFdoDirectories.cs, NullThreadedProgress.cs, Program.cs, ProjectIdentifier.cs
- **Source file count**: 6 files
- **Data file count**: 1 files

## References (auto-generated hints)
- Project files:
  - Src/GenerateHCConfig/BuildInclude.targets
  - Src/GenerateHCConfig/GenerateHCConfig.csproj
- Key C# files:
  - Src/GenerateHCConfig/ConsoleLogger.cs
  - Src/GenerateHCConfig/NullFdoDirectories.cs
  - Src/GenerateHCConfig/NullThreadedProgress.cs
  - Src/GenerateHCConfig/Program.cs
  - Src/GenerateHCConfig/ProjectIdentifier.cs
  - Src/GenerateHCConfig/Properties/AssemblyInfo.cs
- Data contracts/transforms:
  - Src/GenerateHCConfig/App.config
## Code Evidence
*Analysis based on scanning 5 source files*

- **Namespaces**: GenerateHCConfig
