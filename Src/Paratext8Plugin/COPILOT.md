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

## Architecture
C# library with 7 source files. Contains 1 subprojects: Paratext8Plugin.

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

## Interop & Contracts
Uses COM for cross-boundary calls.

## Threading & Performance
Single-threaded or thread-agnostic code. No explicit threading detected.

## Config & Feature Flags
Config files: App.config.

## Build Information
- C# class library implementing Paratext 8 plugin interface
- Includes test suite
- Build with MSBuild or Visual Studio

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

## Entry Points
- Plugin entry points defined by Paratext 8 architecture
- Provides FieldWorks integration from within Paratext 8

## Test Index
Test projects: Paratext8PluginTests. 1 test files. Run via: `dotnet test` or Test Explorer in Visual Studio.

## Usage Hints
Library component. Reference in consuming projects. See Dependencies section for integration points.

## Related Folders
- **FwParatextLexiconPlugin/** - Lexicon-specific Paratext integration
- **ParatextImport/** - Imports Paratext data into FieldWorks
- **LexText/** - Lexicon data shared with Paratext

## References

- **Project files**: Paratext8Plugin.csproj, Paratext8PluginTests.csproj
- **Target frameworks**: net462
- **Key C# files**: AssemblyInfo.cs, PT8VerseRefWrapper.cs, PTScrTextWrapper.cs, Paratext8Provider.cs, ParatextAlert.cs, ParatextDataIntegrationTests.cs, Pt8VerseWrapper.cs
- **Source file count**: 7 files
- **Data file count**: 1 files

## References (auto-generated hints)
- Project files:
  - Src/Paratext8Plugin/ParaText8PluginTests/Paratext8PluginTests.csproj
  - Src/Paratext8Plugin/Paratext8Plugin.csproj
- Key C# files:
  - Src/Paratext8Plugin/PT8VerseRefWrapper.cs
  - Src/Paratext8Plugin/PTScrTextWrapper.cs
  - Src/Paratext8Plugin/ParaText8PluginTests/ParatextDataIntegrationTests.cs
  - Src/Paratext8Plugin/Paratext8Provider.cs
  - Src/Paratext8Plugin/ParatextAlert.cs
  - Src/Paratext8Plugin/Properties/AssemblyInfo.cs
  - Src/Paratext8Plugin/Pt8VerseWrapper.cs
- Data contracts/transforms:
  - Src/Paratext8Plugin/ParaText8PluginTests/App.config
## Code Evidence
*Analysis based on scanning 6 source files*

- **Classes found**: 5 public classes
- **Namespaces**: Paratext8Plugin
