---
last-reviewed: 2025-10-31
last-reviewed-tree: 76886450145052a28b3c1f2b54c499cbc0c7f3879390c5affaf3fac0643f832e
status: draft
---
anchors:
  - change-log-auto
  - purpose
  - architecture
  - key-components
  - referenced-by
  - technology-stack
  - dependencies
  - interop--contracts
  - threading--performance
  - config--feature-flags
  - build-information
  - interfaces-and-data-models
  - entry-points
  - test-index
  - usage-hints
  - related-folders
  - references

<!-- copilot:auto-change-log start -->
## Change Log (auto)

This section is populated by running:
1. `python .github/plan_copilot_updates.py --folders <Folder>`
2. `python .github/copilot_apply_updates.py --folders <Folder>`

Do not edit this block manually; rerun the scripts above after code or doc updates.
<!-- copilot:auto-change-log end -->

# Lexicon (LexEdDll) COPILOT summary

## Purpose
Lexicon editing UI library for FieldWorks Language Explorer (FLEx). Provides specialized controls, handlers, and dialogs for lexical entry editing: entry/sense slices (EntrySequenceReferenceSlice, LexReferencePairSlice, LexReferenceMultiSlice), launchers (EntrySequenceReferenceLauncher, LexReferenceCollectionLauncher), menu handlers (LexEntryMenuHandler), FLExBridge integration (FLExBridgeListener for collaboration), example sentence search (FindExampleSentenceDlg), homograph management (HomographResetter), entry deletion (DeleteEntriesSensesWithoutInterlinearization), and resource files (LexEdStrings localized strings, ImageHolder/LexEntryImages icons). Moderate-sized library (15.7K lines) focusing on lexicon-specific UI and collaboration infrastructure. Project name: LexEdDll.

### Referenced By

- [LIFT Import](../../../openspec/specs/lexicon/import/lift.md#behavior) — Entry structure mapping
- [Dictionary Export](../../../openspec/specs/lexicon/export/dictionary.md#behavior) — Lexicon configuration
- [LIFT Export](../../../openspec/specs/lexicon/export/lift.md#behavior) — Export structure mapping
- [Pathway Export](../../../openspec/specs/lexicon/export/pathway.md#behavior) — Export data preparation

## Architecture
C# library (net48, OutputType=Library) with lexicon UI components. Slice/Launcher pattern for entry field editing. LexEntryMenuHandler for context menus. FLExBridgeListener XCore colleague for Send/Receive collaboration. Dialogs for specialized tasks (FindExampleSentenceDlg). Utility classes for data operations (CircularRefBreaker, GoldEticGuidFixer, HomographResetter). Resource files for localization. Integrates with LCModel (ILexEntry, ILexSense, ILexReference), XCore framework, FLExBridge (external collaboration tool).

## Key Components
- **FLExBridgeListener** (FLExBridgeListener.cs, 1.9K lines): FLExBridge collaboration integration
  - XCore colleague for Send/Receive operations
  - Launches FLExBridge.exe for project collaboration
  - Merge conflict handling
  - First-time Send/Receive instructions (FLExBridgeFirstSendReceiveInstructionsDlg)
- **LexEntryMenuHandler** (LexEntryMenuHandler.cs, 591 lines): Entry context menu handler
  - Right-click menu operations on entries/senses
  - Add/delete entry, add sense, merge entries
- **EntrySequenceReferenceLauncher** (EntrySequenceReferenceLauncher.cs, 656 lines): Entry sequence reference editor
  - Launch dialog for editing entry sequence references
  - EntrySequenceReferenceSlice: Slice display
- **LexReferenceMultiSlice** (LexReferenceMultiSlice.cs, 1.2K lines): Lexical reference multi-slice
  - Display/edit multiple lexical references
  - Tree/sense relationships
- **LexReferenceCollectionLauncher** (LexReferenceCollectionLauncher.cs, 103 lines): Lexical reference collection launcher
  - Launch lexical reference collection editor
  - LexReferenceCollectionSlice, LexReferenceCollectionView
- **LexReferencePairLauncher** (LexReferencePairLauncher.cs, 150 lines): Lexical reference pair launcher
  - Launch pair reference editor
  - LexReferencePairSlice, LexReferencePairView
- **LexReferenceSequenceLauncher** (LexReferenceSequenceLauncher.cs, 97 lines): Lexical reference sequence launcher
  - Launch sequence reference editor
- **FindExampleSentenceDlg** (FindExampleSentenceDlg.cs, 308 lines): Find example sentence dialog
  - Search corpus for example sentences
  - Insert into sense
- **GhostLexRefSlice** (GhostLexRefSlice.cs, 172 lines): Ghost lexical reference slice
  - Placeholder for lexical references
- **CircularRefBreaker** (CircularRefBreaker.cs, 80 lines): Circular reference detector/breaker
  - Detect and break circular entry relationships
- **HomographResetter** (HomographResetter.cs, 94 lines): Homograph number resetter
  - Recalculate homograph numbers after entry changes
- **GoldEticGuidFixer** (GoldEticGuidFixer.cs, 130 lines): GOLD Etic GUID fixer
  - Fix GOLD (General Ontology for Linguistic Description) etic GUIDs
- **DeleteEntriesSensesWithoutInterlinearization** (DeleteEntriesSensesWithoutInterlinearization.cs, 125 lines): Cleanup utility
  - Delete entries/senses not used in interlinear texts
- **LexEntryChangeHandler** (LexEntryChangeHandler.cs, 137 lines): Entry change handler
  - Handle entry property changes, notifications
- **LexEntryInflTypeConverter** (LexEntryInflTypeConverter.cs, 231 lines): Inflection type converter
  - Convert inflection type data for display
- **FLExBridgeFirstSendReceiveInstructionsDlg** (FLExBridgeFirstSendReceiveInstructionsDlg.cs, 37 lines): First-time Send/Receive instructions
  - Dialog explaining Send/Receive workflow
- **LexEdStrings** (LexEdStrings.Designer.cs, LexEdStrings.resx, 2K lines): Localized strings
  - Designer-generated resource accessor
  - Localized UI strings for lexicon editing
- **ImageHolder, LexEntryImages** (ImageHolder.cs, LexEntryImages.cs, 100 lines): Icon resources
  - Embedded icons/images for lexicon UI

### Referenced By

- [Entry Structure](../../../openspec/specs/lexicon/entries/structure.md#behavior) — Entry slices and launchers
- [Entry Creation](../../../openspec/specs/lexicon/entries/creation.md#behavior) — Entry menu handlers
- [Lexical Relations](../../../openspec/specs/lexicon/entries/relations.md#behavior) — Reference slices and launchers
- [Send/Receive Collaboration](../../../openspec/specs/integration/collaboration/send-receive.md#behavior) — FLExBridge integration

## Technology Stack
C# .NET Framework 4.8.x, Windows Forms, LCModel, XCore, FLExBridge (external process).

## Dependencies
Consumes: LCModel (ILexEntry, ILexSense, ILexReference), XCore (Mediator, IxCoreColleague), LexTextControls, FLExBridge.exe (invoked via Process.Start). Used by: xWorks, FieldWorks.exe.

## Interop & Contracts
ILexEntry, ILexSense, ILexReference, ILexEntryRef, FLExBridge.exe (Process.Start), IxCoreColleague (FLExBridgeListener).

## Threading & Performance
UI thread for all operations. FLExBridge: external process invocation.

## Config & Feature Flags
FLExBridge integration (FLExBridgeListener), homograph numbering (HomographResetter).

## Build Information
LexEdDll.csproj (net48), output: SIL.FieldWorks.XWorks.LexEd.dll. Tests: `dotnet test LexEdDllTests/`.

## Interfaces and Data Models

- **FLExBridgeListener** (FLExBridgeListener.cs)
  - Purpose: FLExBridge collaboration integration (Send/Receive)
  - Interface: IxCoreColleague
  - Key methods: OnSendReceiveProject(), LaunchFLExBridge()
  - Notes: Launches FLExBridge.exe as external process

- **LexEntryMenuHandler** (LexEntryMenuHandler.cs)
  - Purpose: Entry context menu handler
  - Key methods: OnAddEntry(), OnDeleteEntry(), OnAddSense(), OnMergeEntries()
  - Notes: 591 lines of menu logic

- **EntrySequenceReferenceLauncher** (EntrySequenceReferenceLauncher.cs)
  - Purpose: Launch entry sequence reference editor
  - Notes: 656 lines, EntrySequenceReferenceSlice for display

- **LexReferenceMultiSlice** (LexReferenceMultiSlice.cs)
  - Purpose: Display/edit multiple lexical references
  - Notes: 1.2K lines, tree/sense relationships

- **FindExampleSentenceDlg** (FindExampleSentenceDlg.cs)
  - Purpose: Search corpus for example sentences
  - Inputs: Search criteria
  - Outputs: Selected sentence inserted into sense
  - Notes: 308 lines

- **Utility classes**:
  - CircularRefBreaker: Detect/break circular relationships
  - HomographResetter: Recalculate homograph numbers
  - GoldEticGuidFixer: Fix GOLD etic GUIDs
  - DeleteEntriesSensesWithoutInterlinearization: Cleanup unused entries/senses

## Entry Points
Loaded by xWorks. Slices/launchers instantiated by data entry framework.

## Test Index
LexEdDllTests project. Run: `dotnet test LexEdDllTests/`.

## Usage Hints
Lexicon editing via entry slices. FLExBridge: File → Send/Receive. Context menus: right-click entries. Example sentences: Tools → Find Example Sentences. Lexical reference slices for relationships.

## Related Folders
LexTextControls (shared controls), LexTextDll (infrastructure), xWorks (main shell).

## References
LexEdDll.csproj (net48), 15.7K lines. Key files: FLExBridgeListener.cs (1.9K), LexEdStrings.Designer.cs (2K), LexReferenceMultiSlice.cs (1.2K). See `.cache/copilot/diff-plan.json` for file inventory.
