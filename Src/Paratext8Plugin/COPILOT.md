---
owner: FIXME(set-owner)
last-reviewed: 2025-10-30
status: verified
---

# Paratext8Plugin

## Purpose
Integration plugin for Paratext 8. Enables bidirectional integration between FieldWorks and Paratext 8 Bible translation software, allowing data sharing and collaboration between the two systems.

## Key Components
### Key Classes
- **ParatextAlert**
- **Paratext8Provider**
- **PT8ParserStateWrapper**
- **MockScriptureProvider**
- **ParatextDataIntegrationTests**

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

## Entry Points
- Plugin entry points defined by Paratext 8 architecture
- Provides FieldWorks integration from within Paratext 8

## Related Folders
- **FwParatextLexiconPlugin/** - Lexicon-specific Paratext integration
- **ParatextImport/** - Imports Paratext data into FieldWorks
- **LexText/** - Lexicon data shared with Paratext

## Code Evidence
*Analysis based on scanning 6 source files*

- **Classes found**: 5 public classes
- **Namespaces**: Paratext8Plugin
