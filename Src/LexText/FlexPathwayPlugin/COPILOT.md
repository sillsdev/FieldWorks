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

## Architecture
C# library with 6 source files. Contains 1 subprojects: FlexPathwayPlugin.

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

## Interop & Contracts
Uses COM for cross-boundary calls.

## Threading & Performance
Single-threaded or thread-agnostic code. No explicit threading detected.

## Config & Feature Flags
No explicit configuration or feature flags detected.

## Build Information
- C# class library plugin project
- Build via: `dotnet build FlexPathwayPlugin.csproj`
- Includes test suite

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

## Entry Points
- Plugin interface for Pathway integration
- Export and publishing workflows

## Test Index
Test projects: FlexPathwayPluginTests. 2 test files. Run via: `dotnet test` or Test Explorer in Visual Studio.

## Usage Hints
Library component. Reference in consuming projects. See Dependencies section for integration points.

## Related Folders
- **LexText/LexTextDll/** - Core LexText functionality
- **Transforms/** - XSLT transforms used for export
- **FXT/** - Transform tools for data conversion

## References

- **Project files**: FlexPathwayPlugin.csproj, FlexPathwayPluginTests.csproj
- **Target frameworks**: net462
- **Key C# files**: AssemblyInfo.cs, FlexPathwayPlugin.cs, FlexPathwayPluginTests.cs, MyFoldersTest.cs, MyProcess.cs, myFolders.cs
- **Source file count**: 6 files
- **Data file count**: 0 files

## References (auto-generated hints)
- Project files:
  - LexText/FlexPathwayPlugin/FlexPathwayPlugin.csproj
  - LexText/FlexPathwayPlugin/FlexPathwayPluginTests/FlexPathwayPluginTests.csproj
- Key C# files:
  - LexText/FlexPathwayPlugin/AssemblyInfo.cs
  - LexText/FlexPathwayPlugin/FlexPathwayPlugin.cs
  - LexText/FlexPathwayPlugin/FlexPathwayPluginTests/FlexPathwayPluginTests.cs
  - LexText/FlexPathwayPlugin/FlexPathwayPluginTests/MyFoldersTest.cs
  - LexText/FlexPathwayPlugin/FlexPathwayPluginTests/MyProcess.cs
  - LexText/FlexPathwayPlugin/myFolders.cs
## Code Evidence
*Analysis based on scanning 6 source files*

- **Classes found**: 5 public classes
- **Namespaces**: FlexDePluginTests, FlexPathwayPluginTests, SIL.FieldWorks.XWorks, SIL.PublishingSolution
