---
owner: FIXME(set-owner)
last-reviewed: 2025-10-30
status: verified
---

# FXT

## Purpose
FieldWorks transform assets and related tooling. Provides transformation capabilities for converting and processing linguistic data between different formats and representations.

## Key Components
- **FxtExe/FxtExe.csproj** - Executable transformation tool
- **FxtDll/FxtDll.csproj** - Transform library for programmatic use

## Technology Stack
- C# .NET
- XSLT transformation engine
- XML processing

## Dependencies
- Depends on: Common utilities, data model
- Used by: Export/import pipelines, document generation

## Build Information
- Two C# projects: executable (FxtExe) and library (FxtDll)
- Build with MSBuild or Visual Studio
- Command-line transformation tool

## Entry Points
- **FxtExe** - Command-line tool for applying transforms
- **FxtDll** - Library for embedding transformation capabilities

## Related Folders
- **Transforms/** - Contains XSLT files and transformation assets used by FXT
- **DocConvert/** - Document conversion that may use FXT transformations
- **ParatextImport/** - May use FXT for data transformation during import
