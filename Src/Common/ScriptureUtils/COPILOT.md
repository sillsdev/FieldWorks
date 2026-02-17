---
last-reviewed: 2025-10-31
last-reviewed-tree: 6eff5a0c5e237f35fa511195a7cf7a5bb7ec4c9cf3b6dec768ded6e91032b3f8
status: draft
---

<!-- copilot:auto-change-log start -->
## Change Log (auto)

This section is populated by running:
1. `python .github/plan_copilot_updates.py --folders <Folder>`
2. `python .github/copilot_apply_updates.py --folders <Folder>`

Do not edit this block manually; rerun the scripts above after code or doc updates.
<!-- copilot:auto-change-log end -->

# ScriptureUtils COPILOT summary

## Purpose
Scripture-specific utilities and Paratext integration support for bidirectional data exchange between FieldWorks and Paratext projects. Provides ParatextHelper (IParatextHelper) for Paratext project discovery and management, PT7ScrTextWrapper for Paratext 7 project text access, Paratext7Provider for data provider implementation, ScriptureProvider for scripture text access, and reference comparison utilities (ScrReferencePositionComparer, ScriptureReferenceComparer). Enables importing scripture from Paratext, synchronizing changes, and accessing Paratext stylesheets and parser information.

## Architecture
C# class library (.NET Framework 4.8.x) with Paratext integration components. Implements provider pattern for scripture access (ScriptureProvider, Paratext7Provider). Wrapper classes (PT7ScrTextWrapper) adapt Paratext objects to FieldWorks interfaces (IScrText). Comparers for scripture reference ordering and positioning.

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
C# .NET Framework 4.8.x, Paratext API integration, SIL.LCModel.Core.Scripture.

## Dependencies
- Upstream: Paratext libraries, SIL.LCModel, SIL.LCModel.Core.Scripture
- Downstream: ParatextImport, Scripture editing, Interlinear tools

## Interop & Contracts
IParatextHelper, IScrText, IScriptureProvider* interfaces adapt Paratext API to FieldWorks.

## Threading & Performance
Single-threaded Paratext access, performance depends on project size and file I/O.

## Config & Feature Flags
No explicit config, Paratext paths determined by installation, import settings via IScrImportSet.

## Build Information
ScriptureUtils.csproj (net48) → ScriptureUtils.dll.

## Interfaces and Data Models
IParatextHelper, IScrText, PT7ScrTextWrapper, Paratext7Provider, ScriptureProvider, ScrReferencePositionComparer, ScriptureReferenceComparer.

## Entry Points
Library for Paratext integration, used by import and scripture editing.

## Test Index
ScriptureUtilsTests validates Paratext integration and reference comparison.

## Usage Hints
Use IParatextHelper for project discovery, PT7ScrTextWrapper for adaptation, comparers for reference sorting.

## Related Folders
ParatextImport, Paratext8Plugin, SIL.LCModel.

## References
See `.cache/copilot/diff-plan.json` for file details.
