---
owner: FIXME(set-owner)
last-reviewed: 2025-10-30
status: verified
---

# Paratext8Plugin

## Purpose
Integration plugin for Paratext 8. Enables bidirectional integration between FieldWorks and Paratext 8 Bible translation software, allowing data sharing and collaboration between the two systems.

## Key Components
- **Paratext8Plugin.csproj** - Main plugin library for Paratext 8
- **ParaText8PluginTests/Paratext8PluginTests.csproj** - Plugin tests

## Technology Stack
- C# .NET
- Paratext 8 plugin API
- Data synchronization and integration

## Dependencies
- Depends on: Cellar (data model), Paratext 8 SDK, FdoUi
- Used by: Paratext 8 application when integrating with FieldWorks

## Build Information
- C# class library implementing Paratext 8 plugin interface
- Includes test suite
- Build with MSBuild or Visual Studio

## Testing
- Run tests: `dotnet test Paratext8Plugin/ParaText8PluginTests/Paratext8PluginTests.csproj`
- Tests cover plugin integration and data exchange

## Entry Points
- Plugin entry points defined by Paratext 8 architecture
- Provides FieldWorks integration from within Paratext 8

## Related Folders
- **FwParatextLexiconPlugin/** - Lexicon-specific Paratext integration
- **ParatextImport/** - Imports Paratext data into FieldWorks
- **LexText/** - Lexicon data shared with Paratext


## References
- **Project Files**: Paratext8Plugin.csproj
- **Key C# Files**: PT8VerseRefWrapper.cs, PTScrTextWrapper.cs, Paratext8Provider.cs, ParatextAlert.cs, Pt8VerseWrapper.cs
