---
owner: FIXME(set-owner)
last-reviewed: 2025-10-30
status: verified
---

# ProjectUnpacker

## Purpose
Utilities for extracting and decompressing FieldWorks project archives. 
Handles unpacking of .fwbackup files and other compressed project formats. 
Supports project restoration, sharing, and migration scenarios by providing 
reliable extraction of project data from archive formats.

## Key Components
### Key Classes
- **RegistryData**
- **Unpacker**
- **ResourceUnpacker**

## Technology Stack
- C# .NET
- Archive/compression handling
- File system operations
- Project file management

## Dependencies
- Depends on: Cellar (data model), Common utilities
- Used by: Application startup, project opening, import/restore features

## Build Information
- C# executable or library
- Build with MSBuild or Visual Studio

## Entry Points
- Command-line tool or library for project unpacking
- Used when opening archived projects

## Related Folders
- **Cellar/** - Data model for projects being unpacked
- **MigrateSqlDbs/** - May need to migrate unpacked projects
- **InstallValidator/** - May validate unpacked project structure

## Code Evidence
*Analysis based on scanning 3 source files*

- **Classes found**: 3 public classes
- **Namespaces**: SIL.FieldWorks.Test.ProjectUnpacker

## Interfaces and Data Models

- **RegistryData** (class)
  - Path: `RegistryData.cs`
  - Public class implementation

- **ResourceUnpacker** (class)
  - Path: `Unpacker.cs`
  - Public class implementation

- **Unpacker** (class)
  - Path: `Unpacker.cs`
  - Public class implementation

## References

- **Project files**: ProjectUnpacker.csproj
- **Target frameworks**: net462
- **Key C# files**: AssemblyInfo.cs, RegistryData.cs, Unpacker.cs
- **Source file count**: 3 files
- **Data file count**: 3 files
