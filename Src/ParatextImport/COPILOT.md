---
last-reviewed: 2025-10-31
last-verified-commit: 17b0b53
status: reviewed
---

# ParatextImport

## Purpose
Paratext Scripture import pipeline for FieldWorks (~19K lines). Handles USFM parsing, difference detection, book merging, and undo management for importing Paratext project data into FLEx Scripture. Coordinates UI dialogs, import settings, and LCModel updates while preserving existing content through smart merging.

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

## Dependencies
- **Upstream**: LCModel.Core (Scripture, Text, KernelInterfaces), LCModel (cache, domain services, infrastructure), Common/Controls (UI), Common/FwUtils (utilities, IApp), Common/RootSites (UI integration), SIL.Reporting (logging)
- **Downstream consumers**: xWorks (import commands), LexText applications (Scripture import), Common/ScriptureUtils (ParatextHelper coordination)
- **External**: Paratext SDK (not bundled - USFM/project access via wrappers)

## Test Infrastructure
- **ParatextImportTests/** subfolder with 15 test files
- **AutoMergeTests**, **BookMergerTests**, **ClusterTests**, **DifferenceTests** - Core algorithm tests
- **Import test suites**: ParatextImportTests, ParatextImportManagerTests, ImportStyleProxyTests
- **Interleaved/NonInterleaved BT tests**, **Paratext6Tests** - Legacy format support
- **DiffTestHelper**, **BookMergerTestsBase** - Test infrastructure
- Run via: `dotnet test` or Visual Studio Test Explorer

## Related Folders
- **Common/ScriptureUtils/** - ParatextHelper, PT7ScrTextWrapper for Paratext integration
- **Paratext8Plugin/** - Paratext 8+ bidirectional sync via MEF plugin
- **FwParatextLexiconPlugin/** - Lexicon data export to Paratext
- **xWorks/** - Import UI commands and workflow integration

## References
- **Project**: ParatextImport.csproj (.NET Framework 4.6.2 class library)
- **Test project**: ParatextImportTests/ParatextImportTests.csproj
- **20 CS files** (main), **15 test files**, **~19K lines total**
- **Key files**: ParatextImportManager.cs, BookMerger.cs, Cluster.cs, Difference.cs, ParatextSfmImporter.cs
