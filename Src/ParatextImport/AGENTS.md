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
- **ParatextImportManager**: Central coordinator, ImportParatext() entry point, manages UndoImportManager
- **BookMerger/Cluster/Difference**: Detects differences, groups for review, represents individual changes
- **ISCScriptureText interfaces**: Abstracts Paratext SDK access (ISCTextSegment, ISCTextEnum, IBookVersionAgent)
- **Support classes**: ImportedBooks, ImportStyleProxy, ScrObjWrapper, UndoImportManager

## Technology Stack
C# (net48). Key libraries: LCModel, LCModel.Core, Common/Controls, Paratext SDK (wrapped via ISCScriptureText interfaces).

## Dependencies
**Upstream**: LCModel, Common/Controls, Paratext SDK
**Downstream**: xWorks, LexText applications, Common/ScriptureUtils

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
File→Import→Paratext Project, select books, review 33+ difference types (additions/deletions/changes/moves), resolve conflicts, complete import. Import wrapped in single UndoTask for rollback.

## Related Folders
- **Common/ScriptureUtils/**: Paratext integration
- **Paratext8Plugin/**: PT8+ sync
- **xWorks/**: Import UI

## References
20 C# files, 15 test files (~19K lines). Key: ParatextImportManager.cs, BookMerger.cs, Cluster.cs, Difference.cs. See `.cache/copilot/diff-plan.json` for file listings.
