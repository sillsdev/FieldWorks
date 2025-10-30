---
last-reviewed: 2025-10-30
last-verified-commit: 9611cf70e
status: draft
---

# Paratext8Plugin

## Purpose
Bidirectional integration plugin for Paratext 8 Bible translation software.
Enables data exchange and synchronization between FieldWorks and Paratext 8 projects.
Supports sharing linguistic data, managing project relationships, and coordinating
translation workflows between the two applications.

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

## Interfaces and Data Models

- **MockScriptureProvider** (class)
  - Path: `ParaText8PluginTests/ParatextDataIntegrationTests.cs`
  - Public class implementation

- **PT8ParserStateWrapper** (class)
  - Path: `Paratext8Provider.cs`
  - Public class implementation

- **Paratext8Provider** (class)
  - Path: `Paratext8Provider.cs`
  - Public class implementation

- **ParatextAlert** (class)
  - Path: `ParatextAlert.cs`
  - Public class implementation

## References

- **Project files**: Paratext8Plugin.csproj, Paratext8PluginTests.csproj
- **Target frameworks**: net462
- **Key C# files**: AssemblyInfo.cs, PT8VerseRefWrapper.cs, PTScrTextWrapper.cs, Paratext8Provider.cs, ParatextAlert.cs, ParatextDataIntegrationTests.cs, Pt8VerseWrapper.cs
- **Source file count**: 7 files
- **Data file count**: 1 files

## Architecture
TBD — populate from code. See auto-generated hints below.

## Interop & Contracts
TBD — populate from code. See auto-generated hints below.

## Threading & Performance
TBD — populate from code. See auto-generated hints below.

## Config & Feature Flags
TBD — populate from code. See auto-generated hints below.

## Test Index
TBD — populate from code. See auto-generated hints below.

## Usage Hints
TBD — populate from code. See auto-generated hints below.

## References (auto-generated hints)
- Project files:
  - Src\Paratext8Plugin\ParaText8PluginTests\Paratext8PluginTests.csproj
  - Src\Paratext8Plugin\Paratext8Plugin.csproj
- Key C# files:
  - Src\Paratext8Plugin\PT8VerseRefWrapper.cs
  - Src\Paratext8Plugin\PTScrTextWrapper.cs
  - Src\Paratext8Plugin\ParaText8PluginTests\ParatextDataIntegrationTests.cs
  - Src\Paratext8Plugin\Paratext8Provider.cs
  - Src\Paratext8Plugin\ParatextAlert.cs
  - Src\Paratext8Plugin\Properties\AssemblyInfo.cs
  - Src\Paratext8Plugin\Pt8VerseWrapper.cs
- Data contracts/transforms:
  - Src\Paratext8Plugin\ParaText8PluginTests\App.config
