---
owner: FIXME(set-owner)
last-reviewed: 2025-10-30
status: verified
---

# ParatextImport

## Purpose
Import pipeline for Paratext data into FieldWorks. Handles importing Scripture texts, notes, and related data from Paratext projects into the FieldWorks data model.

## Key Components
- **ParatextImport.csproj** - Main import library
- **ParatextImportTests/ParatextImportTests.csproj** - Import functionality tests

## Technology Stack
- C# .NET
- Paratext data format parsing
- Data transformation and mapping
- Import pipeline architecture

## Dependencies
- Depends on: Cellar (data model), Paratext SDK, Common utilities
- Used by: FieldWorks applications importing Paratext data

## Build Information
- C# class library
- Includes comprehensive test suite
- Build with MSBuild or Visual Studio

## Testing
- Run tests: `dotnet test ParatextImport/ParatextImportTests/ParatextImportTests.csproj`
- Tests cover import scenarios and data transformation

## Entry Points
- Provides import APIs and wizards
- Used through UI import commands in applications

## Related Folders
- **Paratext8Plugin/** - Bidirectional Paratext 8 integration
- **FwParatextLexiconPlugin/** - Lexicon data sharing with Paratext
- **LexText/** - Target for imported lexical data
- **DocConvert/** - Document conversion utilities used in import
- **FXT/** - May use transformations during import


## References
- **Project Files**: ParatextImport.csproj
- **Key C# Files**: BookMerger.cs, Cluster.cs, DiffLocation.cs, Difference.cs, IBookVersionAgent.cs, ISCScriptureText.cs, ISCTextEnum.cs, ISCTextSegment.cs
