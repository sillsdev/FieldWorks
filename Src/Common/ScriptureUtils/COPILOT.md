---
last-reviewed: 2025-10-31
last-reviewed-tree: cb92de32746951c55376a7c729623b1a056286d6de6d30c6ea3f9784591ca81f
status: draft
---

# ScriptureUtils COPILOT summary

## Purpose
Scripture-specific utilities and Paratext integration support for bidirectional data exchange between FieldWorks and Paratext projects. Provides ParatextHelper (IParatextHelper) for Paratext project discovery and management, PT7ScrTextWrapper for Paratext 7 project text access, Paratext7Provider for data provider implementation, ScriptureProvider for scripture text access, and reference comparison utilities (ScrReferencePositionComparer, ScriptureReferenceComparer). Enables importing scripture from Paratext, synchronizing changes, and accessing Paratext stylesheets and parser information.

## Architecture
C# class library (.NET Framework 4.6.2) with Paratext integration components. Implements provider pattern for scripture access (ScriptureProvider, Paratext7Provider). Wrapper classes (PT7ScrTextWrapper) adapt Paratext objects to FieldWorks interfaces (IScrText). Comparers for scripture reference ordering and positioning.

## Key Components
- **ParatextHelper** class (ParatextHelper.cs): Paratext project access
  - Implements IParatextHelper interface
  - RefreshProjects(): Reloads available Paratext projects
  - ReloadProject(): Refreshes specific project data
  - GetShortNames(): Returns sorted project short names
  - GetProjects(): Enumerates available IScrText projects
  - LoadProjectMappings(): Loads import mappings for Paratext 6/7
- **IParatextHelper** interface: Contract for Paratext utilities
- **IScrText** interface: Scripture text abstraction
  - Reload(): Refreshes project data
  - DefaultStylesheet: Access to stylesheet
  - Parser: Scripture parser access
  - BooksPresentSet: Available books
  - Name: Project name
  - AssociatedLexicalProject: Linked lexical project
- **PT7ScrTextWrapper** (PT7ScrTextWrapper.cs): Paratext 7 text wrapper
  - Adapts Paratext 7 ScrText objects to IScrText interface
  - Bridges Paratext API to FieldWorks scripture interfaces
- **Paratext7Provider** (Paratext7Provider.cs): Data provider for Paratext integration
  - Implements provider pattern for Paratext data access
  - Handles data exchange between FieldWorks and Paratext
- **ScriptureProvider** (ScriptureProvider.cs): Scripture text provider
  - Abstract/base provider for scripture text access
  - Used by import and interlinear systems
- **ScrReferencePositionComparer** (ScrReferencePositionComparer.cs): Position-based reference comparison
  - Compares scripture references by position within text
  - Used for sorting references by document order
- **ScriptureReferenceComparer** (ScriptureReferenceComparer.cs): Canonical reference comparison
  - Compares scripture references by canonical book order
  - Standard reference sorting (Genesis before Exodus, etc.)

## Technology Stack
- C# .NET Framework 4.6.2 (net462)
- OutputType: Library
- Paratext API integration (external dependency)
- SIL.LCModel.Core.Scripture for scripture data types

## Dependencies

### Upstream (consumes)
- **Paratext libraries**: External Paratext API for project access
- **SIL.LCModel**: Data model (IScrImportSet, scripture domain objects)
- **SIL.LCModel.DomainServices**: Domain service layer
- **SIL.LCModel.Core.Scripture**: Scripture data types and interfaces (IScriptureProvider*, BCVRef)
- **SIL.Reporting**: Error reporting

### Downstream (consumed by)
- **Scripture editing components**: Use Paratext integration
- **Import/export tools**: ParatextImport uses these utilities
- **Interlinear tools**: Access scripture via providers
- Any FieldWorks component requiring Paratext integration

## Interop & Contracts
- **IParatextHelper**: Contract for Paratext project management
- **IScrText**: Contract abstracting scripture text sources
- **IScriptureProvider* interfaces**: Stylesheet, parser, book set providers
- Adapts external Paratext API to FieldWorks interfaces

## Threading & Performance
- Single-threaded access to Paratext projects
- File I/O for Paratext project discovery and access
- Performance depends on Paratext project size and file system

## Config & Feature Flags
- No explicit configuration
- Paratext project paths determined by Paratext installation
- Import settings controlled via IScrImportSet

## Build Information
- **Project file**: ScriptureUtils.csproj (net462, OutputType=Library)
- **Test project**: ScriptureUtilsTests/ScriptureUtilsTests.csproj
- **Output**: ScriptureUtils.dll
- **Build**: Via top-level FieldWorks.sln or: `msbuild ScriptureUtils.csproj /p:Configuration=Debug`
- **Run tests**: `dotnet test ScriptureUtilsTests/ScriptureUtilsTests.csproj`

## Interfaces and Data Models

- **IParatextHelper** (ParatextHelper.cs)
  - Purpose: Contract for accessing Paratext projects and utilities
  - Inputs: Project identifiers, import settings
  - Outputs: Project lists, IScrText instances, mappings
  - Notes: Implemented by ParatextHelper

- **IScrText** (ParatextHelper.cs)
  - Purpose: Abstraction of scripture text source (Paratext project or other)
  - Inputs: N/A (properties)
  - Outputs: Stylesheet, parser, book set, project metadata
  - Notes: Implemented by PT7ScrTextWrapper for Paratext 7 projects

- **PT7ScrTextWrapper** (PT7ScrTextWrapper.cs)
  - Purpose: Adapts Paratext 7 ScrText objects to IScrText interface
  - Inputs: Paratext 7 ScrText object
  - Outputs: IScrText interface implementation
  - Notes: Bridge between Paratext API and FieldWorks

- **Paratext7Provider** (Paratext7Provider.cs)
  - Purpose: Provider for Paratext data access
  - Inputs: Project references
  - Outputs: Scripture data from Paratext projects
  - Notes: Implements provider pattern for data exchange

- **ScriptureProvider** (ScriptureProvider.cs)
  - Purpose: Base provider for scripture text access
  - Inputs: Scripture references
  - Outputs: Scripture text and metadata
  - Notes: Base class for specific providers

- **ScrReferencePositionComparer** (ScrReferencePositionComparer.cs)
  - Purpose: Compares scripture references by position within text
  - Inputs: Two scripture references
  - Outputs: Comparison result (-1, 0, 1)
  - Notes: Used for document order sorting

- **ScriptureReferenceComparer** (ScriptureReferenceComparer.cs)
  - Purpose: Compares scripture references by canonical book order
  - Inputs: Two scripture references
  - Outputs: Comparison result (-1, 0, 1)
  - Notes: Standard reference sorting (Genesis < Exodus < Matthew < etc.)

## Entry Points
Referenced as library for Paratext integration and scripture utilities. Used by import tools and scripture editing components.

## Test Index
- **Test project**: ScriptureUtilsTests
- **Run tests**: `dotnet test ScriptureUtilsTests/ScriptureUtilsTests.csproj`
- **Coverage**: Paratext integration, reference comparison

## Usage Hints
- Use IParatextHelper to discover and access Paratext projects
- PT7ScrTextWrapper adapts Paratext objects to FieldWorks interfaces
- Use ScriptureReferenceComparer for canonical sorting of references
- Use ScrReferencePositionComparer for document order sorting
- Requires Paratext to be installed for full functionality

## Related Folders
- **ParatextImport/**: Uses ScriptureUtils for importing Paratext data
- **Paratext8Plugin/**: Newer Paratext 8 integration (parallel infrastructure)
- **SIL.LCModel**: Scripture data model
- Scripture editing components throughout FieldWorks

## References
- **Project files**: ScriptureUtils.csproj (net462), ScriptureUtilsTests/ScriptureUtilsTests.csproj
- **Target frameworks**: .NET Framework 4.6.2
- **Key C# files**: ParatextHelper.cs, PT7ScrTextWrapper.cs, Paratext7Provider.cs, ScriptureProvider.cs, ScrReferencePositionComparer.cs, ScriptureReferenceComparer.cs, AssemblyInfo.cs
- **Total lines of code**: 1670
- **Output**: Output/Debug/ScriptureUtils.dll
- **Namespace**: SIL.FieldWorks.Common.ScriptureUtils