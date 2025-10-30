---
owner: FIXME(set-owner)
last-reviewed: 2025-10-30
status: verified
---

# ProjectUnpacker

## Purpose
Utilities for unpacking FieldWorks projects. Handles decompression and extraction of FieldWorks project archives, enabling project sharing and backup/restore functionality.

## Key Components
- **ProjectUnpacker.csproj** - Project unpacking utility

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
