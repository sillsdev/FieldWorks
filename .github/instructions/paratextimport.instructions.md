---
applyTo: "Src/ParatextImport/**"
name: "paratextimport.instructions"
description: "Auto-generated concise instructions from COPILOT.md for ParatextImport"
---

# ParatextImport (Concise)

## Purpose & Scope
Summarized key points from COPILOT.md

## Key Rules
- **ParatextImportManager** (ParatextImportManager.cs) - Central coordinator for Paratext imports
- Entry point: `ImportParatext(Form mainWnd, LcmCache cache, IScrImportSet importSettings, ...)` - Static entry point called via reflection
- `ImportSf()` - Main import workflow with undo task wrapping
- `CompleteImport(ScrReference firstImported)` - Post-import finalization
- Manages UndoImportManager, settings, and UI coordination
- **ParatextImportUi** (ParatextImportUi.cs) - UI presentation and dialogs

## Example (from summary)

---
last-reviewed: 2025-10-31
last-reviewed-tree: baf2149067818bca3334ab230423588b48aa0ca02a7c904d87a976a7c2f8b871
status: reviewed
---

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
  - `DetectDifferences(IScrBook bookCurr, IScrBook bookRev, ...)`
