---
last-reviewed: 2025-10-30
last-verified-commit: 9611cf70e
status: draft
---

# FlexPathwayPlugin

## Purpose
Pathway publishing system integration for FLEx.
Provides plugin infrastructure to export lexicon and interlinear data using SIL's Pathway
publishing system. Enables creation of formatted dictionaries, interlinear texts, and other
publications in various formats (PDF, EPUB, Word) from FieldWorks data.

## Key Components
### Key Classes
- **FlexPathwayPlugin**
- **DeExportDialog**
- **MyFolders**
- **MyFoldersTest**
- **FlexPathwayPluginTest**

## Technology Stack
- C# .NET
- Pathway publishing API integration
- Plugin architecture

## Dependencies
- Depends on: Pathway SDK, LexText core, Cellar (data model)
- Used by: LexText export and publishing features

## Build Information
- C# class library plugin project
- Build via: `dotnet build FlexPathwayPlugin.csproj`
- Includes test suite

## Entry Points
- Plugin interface for Pathway integration
- Export and publishing workflows

## Related Folders
- **LexText/LexTextDll/** - Core LexText functionality
- **Transforms/** - XSLT transforms used for export
- **FXT/** - Transform tools for data conversion

## Code Evidence
*Analysis based on scanning 6 source files*

- **Classes found**: 5 public classes
- **Namespaces**: FlexDePluginTests, FlexPathwayPluginTests, SIL.FieldWorks.XWorks, SIL.PublishingSolution

## Interfaces and Data Models

- **DeExportDialog** (class)
  - Path: `FlexPathwayPlugin.cs`
  - Public class implementation

- **FlexPathwayPlugin** (class)
  - Path: `FlexPathwayPlugin.cs`
  - Public class implementation

- **MyFolders** (class)
  - Path: `myFolders.cs`
  - Public class implementation

## References

- **Project files**: FlexPathwayPlugin.csproj, FlexPathwayPluginTests.csproj
- **Target frameworks**: net462
- **Key C# files**: AssemblyInfo.cs, FlexPathwayPlugin.cs, FlexPathwayPluginTests.cs, MyFoldersTest.cs, MyProcess.cs, myFolders.cs
- **Source file count**: 6 files
- **Data file count**: 0 files

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
