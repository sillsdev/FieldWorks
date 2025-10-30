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

## Key Components
No major public classes identified.

## Technology Stack
- C# .NET
- Build-time code generation
- Help system configuration

## Dependencies
- Depends on: Minimal build utilities
- Used by: Build process to generate help configurations

## Build Information
- C# executable project
- Runs during build process
- Build with MSBuild or Visual Studio

## Entry Points
- Command-line tool invoked during build
- Generates configuration files for help system

## Related Folders
- **Build/** - Build scripts that invoke GenerateHCConfig
- **DistFiles/** - May contain generated help configuration output
- **FwResources/** - May work with resource files

## Code Evidence
*Analysis based on scanning 5 source files*

- **Namespaces**: GenerateHCConfig

## References

- **Project files**: GenerateHCConfig.csproj
- **Target frameworks**: net462
- **Key C# files**: AssemblyInfo.cs, ConsoleLogger.cs, NullFdoDirectories.cs, NullThreadedProgress.cs, Program.cs, ProjectIdentifier.cs
- **Source file count**: 6 files
- **Data file count**: 1 files

## Architecture
TBD — populate from code. See auto-generated hints below.

## Interop & Contracts
TBD — populate from code. See auto-generated hints below.

## Threading & Performance
TBD — populate from code. See auto-generated hints below.

## Config & Feature Flags
TBD — populate from code. See auto-generated hints below.

## Interfaces and Data Models
TBD — populate from code. See auto-generated hints below.

## Test Index
TBD — populate from code. See auto-generated hints below.

## Usage Hints
TBD — populate from code. See auto-generated hints below.

## References (auto-generated hints)
- Project files:
  - Src\GenerateHCConfig\BuildInclude.targets
  - Src\GenerateHCConfig\GenerateHCConfig.csproj
- Key C# files:
  - Src\GenerateHCConfig\ConsoleLogger.cs
  - Src\GenerateHCConfig\NullFdoDirectories.cs
  - Src\GenerateHCConfig\NullThreadedProgress.cs
  - Src\GenerateHCConfig\Program.cs
  - Src\GenerateHCConfig\ProjectIdentifier.cs
  - Src\GenerateHCConfig\Properties\AssemblyInfo.cs
- Data contracts/transforms:
  - Src\GenerateHCConfig\App.config
