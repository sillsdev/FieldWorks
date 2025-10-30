---
owner: FIXME(set-owner)
last-reviewed: 2025-10-30
status: verified
---

# SfmStats

## Purpose
Standard Format Marker (SFM) statistics tool. Analyzes SFM files to provide statistics about marker usage, helping users understand their data structure before import.

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
