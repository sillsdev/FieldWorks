---
last-reviewed: 2025-10-31
last-reviewed-tree: baf2149067818bca3334ab230423588b48aa0ca02a7c904d87a976a7c2f8b871
status: reviewed
---

<!-- copilot:auto-change-log start -->
## Change Log (auto)

- Snapshot: HEAD~1
- Risk: none
- Files: 0 (code=0, tests=0, resources=0)

### Prompt seeds
- Update COPILOT.md for Src/ParatextImport. Prioritize Purpose/Architecture sections using planner data.
- Highlight API or UI updates, then confirm Usage/Test sections reflect 0 files changed (code=0, tests=0, resources=0); risk=none.
- Finish with verification notes and TODOs for manual testing.
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
- **UI thread**: All import operations run on UI thread (WinForms dialogs, LCModel updates)
- **Long-running operations**: Import wrapped in progress dialog with cancellation support
- **Performance characteristics**:
  - USFM parsing: Depends on file size (typically <1 second per book)
  - Difference detection: O(n*m) paragraph comparison (can be slow for large books)
  - BookMerger correlation: Expensive for heavily edited books (minutes for complex merges)
  - UI review: User-paced (reviewing differences, resolving conflicts)
- **Optimization strategies**:
  - ParaCorrelationInfo caching for paragraph mappings
  - Cluster grouping reduces UI review overhead (related differences grouped)
  - Lazy difference computation (only when needed for display)
- **No background threading**: Synchronous processing with progress feedback
- **Memory**: Large books (>100K verses) can consume significant memory for difference tracking

## Config & Feature Flags
- **IScrImportSet**: Import settings configuration
  - Import source: Paratext project selection
  - Books to import: User-selected book list
  - Merge strategy: Preserve existing vs overwrite
  - Back translation handling: Interleaved vs non-interleaved
- **DifferenceType filtering**: User can filter which difference types to review
- **ClusterType grouping**: Related differences presented together for efficient review
- **Undo granularity**: Import wrapped in single UndoTask for atomic rollback
- **Style mapping**: ImportStyleProxy handles USFM marker → FW style mapping
  - Style name resolution, proxy creation for missing styles
- **Paratext version support**: Legacy Paratext 6 format via ISCScriptureText abstraction
- **No global config files**: Settings persisted in LCModel IScrImportSet objects

## Build Information
- **Project type**: C# class library (net48)
- **Build**: `msbuild ParatextImport.csproj` or `dotnet build` (from FieldWorks.sln)
- **Output**: ParatextImport.dll
- **Dependencies**: LCModel, LCModel.Core, Common/Controls, Common/FwUtils, Common/RootSites, SIL.Reporting
- **Test project**: ParatextImportTests/ParatextImportTests.csproj (15 test files)
- **Resource files**: Difference.resx, Properties/Resources.resx (localized strings)
- **Optional dependency**: Paratext SDK (accessed via ISCScriptureText wrappers; not required for build)

## Interfaces and Data Models

### Interfaces
- **ISCScriptureText** (path: Src/ParatextImport/ISCScriptureText.cs)
  - Purpose: Abstract Paratext scripture project access
  - Methods: GetText(), GetBookList(), GetVerseRef()
  - Implementations: SCScriptureText (wraps Paratext SDK)
  - Notes: Decouples from Paratext SDK versioning

- **ISCTextSegment** (path: Src/ParatextImport/ISCTextSegment.cs)
  - Purpose: Individual text segment (verse, section head, etc.)
  - Properties: Text (string), Reference (ScrReference), Marker (USFM)
  - Notes: Used in USFM parsing and comparison

- **ISCTextEnum** (path: Src/ParatextImport/ISCTextEnum.cs)
  - Purpose: Enumerate text segments from Paratext project
  - Methods: MoveNext(), Current property
  - Notes: Forward-only enumerator pattern

- **IBookVersionAgent** (path: Src/ParatextImport/IBookVersionAgent.cs)
  - Purpose: Book version comparison contract
  - Methods: Compare book versions, detect differences
  - Notes: Used by BookMerger

### Data Models
- **Difference** (path: Src/ParatextImport/Difference.cs)
  - Purpose: Individual Scripture change representation
  - Shape: DifferenceType (enum), DiffLocation (reference), text data
  - Types: 33+ DifferenceType values (SectionHeadAdded, TextDifference, VerseMoved, etc.)
  - Consumers: UI review dialogs, BookMerger

- **Cluster** (path: Src/ParatextImport/Cluster.cs)
  - Purpose: Group related differences for efficient user review
  - Shape: ClusterType (enum), List<Difference>, correlation info
  - Types: AddedVerses, MissingVerses, OrphanedVerses, etc.
  - Consumers: ParatextImportUi for presenting grouped changes

- **DiffLocation** (path: Src/ParatextImport/DiffLocation.cs)
  - Purpose: Scripture reference and location tracking
  - Shape: Book (int), Chapter (int), Verse (int), section/paragraph indices
  - Consumers: Difference tracking, merge conflict resolution

## Entry Points
- **Reflection entry point**: ParatextImportManager.ImportParatext()
  - Invocation: Called via reflection from xWorks import commands
  - Signature: `static void ImportParatext(Form mainWnd, LcmCache cache, IScrImportSet importSettings, StyleSheet styleSheet, bool fDisplayUi)`
  - Purpose: Late binding allows ParatextImport to be optional dependency
- **Import workflow**:
  1. User invokes File→Import→Paratext in FLEx
  2. xWorks reflects into ParatextImportManager.ImportParatext()
  3. ParatextImportUi shows project/book selection dialogs
  4. ParatextSfmImporter parses USFM from selected Paratext project
  5. BookMerger detects differences between Paratext and FLEx Scripture
  6. User reviews/resolves differences in UI dialogs
  7. Import updates LCModel Scripture data
  8. UndoImportManager tracks changes for rollback
  9. CompleteImport() finalizes import
- **Programmatic access**: ParatextImportManager.ImportSf() for direct import (testing)
- **Common invocation paths**:
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
- **Common/ScriptureUtils/** - ParatextHelper, PT7ScrTextWrapper for Paratext integration
- **Paratext8Plugin/** - Paratext 8+ bidirectional sync via MEF plugin
- **FwParatextLexiconPlugin/** - Lexicon data export to Paratext
- **xWorks/** - Import UI commands and workflow integration

## References
- **Project**: ParatextImport.csproj (.NET Framework 4.8.x class library)
- **Test project**: ParatextImportTests/ParatextImportTests.csproj
- **20 CS files** (main), **15 test files**, **~19K lines total**
- **Key files**: ParatextImportManager.cs, BookMerger.cs, Cluster.cs, Difference.cs, ParatextSfmImporter.cs

## Auto-Generated Project and File References
- Project files:
  - Src/ParatextImport/ParatextImport.csproj
  - Src/ParatextImport/ParatextImportTests/ParatextImportTests.csproj
- Key C# files:
  - Src/ParatextImport/BookMerger.cs
  - Src/ParatextImport/Cluster.cs
  - Src/ParatextImport/DiffLocation.cs
  - Src/ParatextImport/Difference.cs
  - Src/ParatextImport/IBookVersionAgent.cs
  - Src/ParatextImport/ISCScriptureText.cs
  - Src/ParatextImport/ISCTextEnum.cs
  - Src/ParatextImport/ISCTextSegment.cs
  - Src/ParatextImport/ImportStyleProxy.cs
  - Src/ParatextImport/ImportedBooks.cs
  - Src/ParatextImport/ParatextImportExtensions.cs
  - Src/ParatextImport/ParatextImportManager.cs
  - Src/ParatextImport/ParatextImportTests/AutoMergeTests.cs
  - Src/ParatextImport/ParatextImportTests/BookMergerTests.cs
  - Src/ParatextImport/ParatextImportTests/BookMergerTestsBase.cs
  - Src/ParatextImport/ParatextImportTests/ClusterTests.cs
  - Src/ParatextImport/ParatextImportTests/DiffTestHelper.cs
  - Src/ParatextImport/ParatextImportTests/DifferenceTests.cs
  - Src/ParatextImport/ParatextImportTests/ImportTests/ImportStyleProxyTests.cs
  - Src/ParatextImport/ParatextImportTests/ImportTests/ParatextImportBtInterleaved.cs
  - Src/ParatextImport/ParatextImportTests/ImportTests/ParatextImportBtNonInterleaved.cs
  - Src/ParatextImport/ParatextImportTests/ImportTests/ParatextImportManagerTests.cs
  - Src/ParatextImport/ParatextImportTests/ImportTests/ParatextImportNoUi.cs
  - Src/ParatextImport/ParatextImportTests/ImportTests/ParatextImportParatext6Tests.cs
  - Src/ParatextImport/ParatextImportTests/ImportTests/ParatextImportTests.cs
- Data contracts/transforms:
  - Src/ParatextImport/Difference.resx
  - Src/ParatextImport/Properties/Resources.resx
## Test Infrastructure
- **ParatextImportTests/** subfolder with 15 test files
- **AutoMergeTests**, **BookMergerTests**, **ClusterTests**, **DifferenceTests** - Core algorithm tests
- **Import test suites**: ParatextImportTests, ParatextImportManagerTests, ImportStyleProxyTests
- **Interleaved/NonInterleaved BT tests**, **Paratext6Tests** - Legacy format support
- **DiffTestHelper**, **BookMergerTestsBase** - Test infrastructure
- Run via: `dotnet test` or Visual Studio Test Explorer
