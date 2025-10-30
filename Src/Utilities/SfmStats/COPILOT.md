---
last-reviewed: 2025-10-30
last-verified-commit: 9611cf70e
status: draft
---

# SfmStats

## Purpose
Standard Format Marker (SFM) statistics and analysis tool.
Analyzes SFM-formatted data files to provide statistics about marker usage, frequency,
and patterns. Useful for understanding legacy data structure and preparing for data migration
or import into FieldWorks.

## Key Components
No major public classes identified.

## Technology Stack
- C# .NET console application
- SFM file parsing
- Statistical analysis

## Dependencies
- Depends on: SFM parsing utilities, Common utilities
- Used by: Users analyzing SFM data before import

## Build Information
- C# console application
- Build via: `dotnet build SfmStats.csproj`
- Command-line utility

## Entry Points
- Main() method with command-line interface
- SFM file analysis and statistics generation

## Related Folders
- **Utilities/SfmToXml/** - SFM to XML conversion (related tool)
- **LexText/LexTextControls/** - SFM import functionality
- **ParatextImport/** - Uses SFM parsing

## Code Evidence
*Analysis based on scanning 1 source files*

- **Namespaces**: SfmStats

## References

- **Project files**: SfmStats.csproj
- **Target frameworks**: net462
- **Key C# files**: AssemblyInfo.cs, Program.cs
- **Source file count**: 2 files
- **Data file count**: 0 files

## Interfaces and Data Models
See code analysis sections above for key interfaces and data models. Additional interfaces may be documented in source files.

## Architecture
C# library with 2 source files.

## Interop & Contracts
Uses COM for cross-boundary calls.

## Threading & Performance
Single-threaded or thread-agnostic code. No explicit threading detected.

## Config & Feature Flags
No explicit configuration or feature flags detected.

## Test Index
No tests found in this folder. Tests may be in a separate Test folder or solution.

## Usage Hints
Console application. Build and run via command line or Visual Studio. See Entry Points section.
