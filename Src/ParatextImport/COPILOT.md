---
last-reviewed: 2025-10-31
last-reviewed-tree: 2238b4c8a61efc848139b07c520cd636cc82e5d7e1f5ee00523e9703755ba5b3
status: reviewed
---

<!-- copilot:auto-change-log start -->
## Change Log (auto)

This section is populated by running:
1. `python .github/plan_copilot_updates.py --folders <Folder>`
2. `python .github/copilot_apply_updates.py --folders <Folder>`

Do not edit this block manually; rerun the scripts above after code or doc updates.
<!-- copilot:auto-change-log end -->

# ParatextImport

## Purpose
Paratext Scripture import pipeline for FieldWorks (~19K lines). Handles USFM parsing, difference detection, book merging, and undo management for importing Paratext project data into FLEx Scripture. Coordinates UI dialogs, import settings, and LCModel updates while preserving existing content through smart merging.

## Architecture
C# library (net48) with 22 source files (~19K lines). Complex import pipeline coordinating USFM parsing, difference detection, book merging, and UI dialogs. Three-layer architecture:
1. **Management layer**: ParatextImportManager, ParatextImportUi, UndoImportManager
2. **Analysis layer**: BookMerger, Cluster, Difference (difference detection and merge logic)
3. **Adapter layer**: ISCScriptureText wrappers for Paratext SDK abstraction

Import flow: User selects Paratext project → ParatextSfmImporter parses USFM → BookMerger detects differences → User reviews/resolves differences → Import updates LCModel Scripture → UndoImportManager tracks changes for rollback.

## Key Components

### Import Management
- **ParatextImportManager** (ParatextImportManager.cs) - Central coordinator for Paratext imports
  - Entry point: `ImportParatext(Form mainWnd, LcmCache cache, IScrImportSet importSettings, ...)` - Static entry point called via reflection
  - `ImportSf()` - Main import workflow with undo task wrapping
  - `CompleteImport(ScrReference firstImported)` - Post-import finalization
  - Manages UndoImportManager, settings, and UI coordination
- **ParatextImportUi** (ParatextImportUi.cs) - UI presentation and dialogs
- **ParatextSfmImporter** (ParatextSfmImporter.cs) - USFM/SFM file parsing and import logic

### Difference Detection and Merging
- **BookMerger** (BookMerger.cs) - Scripture book comparison and merge engine
  - `DetectDifferences(IScrBook bookCurr, IScrBook bookRev, ...)` - Identify changes between versions
  - `MakeParaCorrelationInfo(...)` - Calculate paragraph correlation factors
  - Uses **ParaCorrelationInfo** for tracking paragraph mappings
- **Cluster** (Cluster.cs) - Groups related differences for user review
  - `ClusterType` enum: AddedVerses, MissingVerses, OrphanedVerses, etc.
  - **ClusterListHelper**, **OverlapInfo**, **SectionHeadCorrelationHelper** - Cluster analysis utilities
- **Difference** (Difference.cs) - Individual Scripture change representation
  - `DifferenceType` enum: SectionHeadAddedToCurrent, TextDifference, VerseMoved, etc.
  - **DifferenceList**, **Comparison** - Difference collections and analysis
- **DiffLocation** (DiffLocation.cs) - Scripture reference and location tracking

### Wrapper Interfaces (Legacy Adaptation)
- **ISCScriptureText** (ISCScriptureText.cs) - Abstracts Paratext text access
- **ISCTextSegment** (ISCTextSegment.cs) - Individual text segment interface
- **ISCTextEnum** (ISCTextEnum.cs) - Enumeration over text segments
- **IBookVersionAgent** (IBookVersionAgent.cs) - Book version comparison contract
- **SCScriptureText**, **SCTextSegment**, **SCTextEnum** (SC*.cs) - Implementations wrapping Paratext SDK

### Support Classes
- **ImportedBooks** (ImportedBooks.cs) - Tracks which books were imported in session
- **ImportStyleProxy** (ImportStyleProxy.cs) - Style mapping and proxy creation
- **ScrAnnotationInfo** (ScrAnnotationInfo.cs) - Scripture annotation metadata
- **ScrObjWrapper** (ScrObjWrapper.cs) - Wraps LCModel Scripture objects for comparison
- **UndoImportManager** (UndoImportManager.cs) - Import rollback tracking
- **ReplaceInFilterFixer** (ReplaceInFilterFixer.cs) - Filter updates during import
- **ParatextLoadException** (ParatextLoadException.cs) - Import-specific exceptions
- **ParatextImportExtensions** (ParatextImportExtensions.cs) - Extension methods for import

## Technology Stack
- **Language**: C#
- **Target framework**: .NET Framework 4.8.x (net48)
- **Key libraries**:
  - LCModel (Scripture data model, LcmCache)
  - LCModel.Core (IScrBook, IScrSection, ITsString)
  - Common/Controls (UI dialogs, progress indicators)
  - Common/FwUtils (IApp, utilities)
  - Common/RootSites (UI integration)
  - SIL.Reporting (logging)
- **External integration**: Paratext SDK (wrapped via ISCScriptureText interfaces)
- **Resource files**: .resx for localized strings

## Dependencies
- **Upstream**: LCModel.Core (Scripture, Text, KernelInterfaces), LCModel (cache, domain services, infrastructure), Common/Controls (UI), Common/FwUtils (utilities, IApp), Common/RootSites (UI integration), SIL.Reporting (logging)
- **Downstream consumers**: xWorks (import commands), LexText applications (Scripture import), Common/ScriptureUtils (ParatextHelper coordination)
- **External**: Paratext SDK (not bundled - USFM/project access via wrappers)

## Interop & Contracts
- **Paratext SDK abstraction**: ISCScriptureText, ISCTextSegment, ISCTextEnum interfaces
  - Purpose: Decouple from Paratext SDK versioning, enable testing with mocks
  - Implementations: SCScriptureText, SCTextSegment, SCTextEnum wrap actual Paratext SDK
- **Reflection entry point**: ImportParatext() called via reflection from xWorks
  - Signature: `static void ImportParatext(Form mainWnd, LcmCache cache, IScrImportSet importSettings, ...)`
  - Purpose: Late binding allows ParatextImport to be optional dependency
- **Data contracts**:
  - IScrImportSet: Import settings and configuration
  - IScrBook: Scripture book data in LCModel
  - DifferenceType enum: Change classification (33+ types)
  - ClusterType enum: Grouped difference categories
- **UI contracts**: Dialogs for user review of differences and merge conflicts
- **Undo contract**: UndoImportManager tracks changes for rollback via LCModel UnitOfWork

## Threading & Performance
UI thread for all operations. Long-running imports wrapped in progress dialog. Difference detection can be slow for heavily edited books.

## Config & Feature Flags
IScrImportSet for import settings (project selection, books, merge strategy). DifferenceType filtering, ClusterType grouping. Import wrapped in UndoTask.

## Build Information
C# library (net48). Build via `msbuild ParatextImport.csproj`. Output: ParatextImport.dll.

## Interfaces and Data Models
ISCScriptureText for Paratext project access. Difference for change representation (33+ types). Cluster for grouping related differences. BookMerger for detection.

## Entry Points
ParatextImportManager.ImportParatext() called via reflection from File→Import→Paratext. ParatextSfmImporter parses USFM, BookMerger detects differences, user reviews.
  - File→Import→Paratext Project: Full import with UI
  - Automated import: ImportSf() with fDisplayUi=false (testing/scripting)

## Test Index
- **Test project**: ParatextImportTests/ParatextImportTests.csproj (15 test files)
- **Key test suites**:
  - **BookMergerTests**: Core merge algorithm tests (BookMergerTests.cs, BookMergerTestsBase.cs)
  - **ClusterTests**: Difference grouping and cluster analysis (ClusterTests.cs)
  - **DifferenceTests**: Individual difference detection and representation (DifferenceTests.cs)
  - **AutoMergeTests**: Automatic merge logic without user intervention (AutoMergeTests.cs)
  - **ImportTests**: End-to-end import scenarios (ParatextImportTests.cs, ParatextImportManagerTests.cs)
  - **Back translation tests**: Interleaved and non-interleaved BT import (ParatextImportBtInterleaved.cs, ParatextImportBtNonInterleaved.cs)
  - **Style tests**: Style mapping and proxy creation (ImportStyleProxyTests.cs)
  - **Legacy format**: Paratext 6 compatibility (ParatextImportParatext6Tests.cs)
  - **No UI tests**: ParatextImportNoUi.cs for headless import testing
- **Test infrastructure**: DiffTestHelper, BookMergerTestsBase (shared test utilities)
- **Test data**: Mock ISCScriptureText implementations, sample USFM snippets
- **Test runners**: Visual Studio Test Explorer, `dotnet test`
- **Coverage**: USFM parsing, difference detection, merge logic, style handling, undo tracking

## Usage Hints
- **Typical import workflow**:
  1. Ensure Paratext project exists and is accessible
  2. In FLEx: File→Import→Paratext Project
  3. Select project and books to import
  4. Review detected differences (additions, changes, deletions)
  5. Resolve conflicts (choose Paratext version, FLEx version, or manual merge)
  6. Complete import (updates Scripture data in LCModel)
- **Difference types**: 33+ types categorized for review
  - Additions: SectionHeadAddedToCurrent, VersesAddedToCurrent
  - Deletions: SectionHeadMissingInCurrent, VersesMissingInCurrent
  - Changes: TextDifference, VerseNumberDifference
  - Moves: VerseMoved, SectionHeadMoved
- **Cluster grouping**: Related differences grouped for efficient review
  - AddedVerses cluster: All added verses in a range
  - MissingVerses cluster: All missing verses in a range
  - OrphanedVerses cluster: Verses without clear correlation
- **Undo/rollback**: Import wrapped in single UndoTask
  - Edit→Undo after import rolls back all changes atomically
- **Performance tips**:
  - Import large books (Psalms, Isaiah) in smaller batches if slow
  - Review differences carefully; auto-merge can have unexpected results
  - Use "Accept all" cautiously; review conflicts manually
- **Common pitfalls**:
  - Paratext project not accessible: Verify Paratext installation and permissions
  - Style mapping errors: Ensure FW styles exist for USFM markers
  - Merge conflicts: Manual resolution required for ambiguous changes
  - Large imports: Can take minutes for books with heavy edits
- **Debugging tips**:
  - Enable logging (SIL.Reporting) for detailed difference detection traces
  - Use ParatextImportNoUi tests for reproducing issues without UI
  - Mock ISCScriptureText for testing without Paratext SDK
- **Extension points**: Implement ISCScriptureText for custom scripture sources

## Related Folders
- **Common/ScriptureUtils/**: Paratext integration
- **Paratext8Plugin/**: PT8+ sync
- **xWorks/**: Import UI

## References
20 C# files, 15 test files (~19K lines). Key: ParatextImportManager.cs, BookMerger.cs, Cluster.cs, Difference.cs. See `.cache/copilot/diff-plan.json` for file listings.
