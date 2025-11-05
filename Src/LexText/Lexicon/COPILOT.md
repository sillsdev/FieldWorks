---
last-reviewed: 2025-10-31
last-reviewed-tree: bd2d6f35a29f37c7bd3b31d265923e8a002993862f71dfa2c5a9b01a8f9d29c3
status: draft
---

# Lexicon (LexEdDll) COPILOT summary

## Purpose
Lexicon editing UI library for FieldWorks Language Explorer (FLEx). Provides specialized controls, handlers, and dialogs for lexical entry editing: entry/sense slices (EntrySequenceReferenceSlice, LexReferencePairSlice, LexReferenceMultiSlice), launchers (EntrySequenceReferenceLauncher, LexReferenceCollectionLauncher), menu handlers (LexEntryMenuHandler), FLExBridge integration (FLExBridgeListener for collaboration), example sentence search (FindExampleSentenceDlg), homograph management (HomographResetter), entry deletion (DeleteEntriesSensesWithoutInterlinearization), and resource files (LexEdStrings localized strings, ImageHolder/LexEntryImages icons). Moderate-sized library (15.7K lines) focusing on lexicon-specific UI and collaboration infrastructure. Project name: LexEdDll.

## Architecture
C# library (net462, OutputType=Library) with lexicon UI components. Slice/Launcher pattern for entry field editing. LexEntryMenuHandler for context menus. FLExBridgeListener XCore colleague for Send/Receive collaboration. Dialogs for specialized tasks (FindExampleSentenceDlg). Utility classes for data operations (CircularRefBreaker, GoldEticGuidFixer, HomographResetter). Resource files for localization. Integrates with LCModel (ILexEntry, ILexSense, ILexReference), XCore framework, FLExBridge (external collaboration tool).

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

## Technology Stack
- C# .NET Framework 4.6.2 (net462)
- OutputType: Library
- Windows Forms (dialogs, slices)
- LCModel (data model)
- XCore (framework)
- FLExBridge (external collaboration tool, invoked via Process.Start)

## Dependencies

### Upstream (consumes)
- **LCModel**: Data model (ILexEntry, ILexSense, ILexReference, ILexEntryRef, ILexRefType)
- **XCore**: Application framework (Mediator, IxCoreColleague)
- **LexTextControls/**: Shared lexicon controls
- **Common/FwUtils**: Utilities
- **FLExBridge** (external): Collaboration tool (invoked as separate process)

### Downstream (consumed by)
- **xWorks**: Main application shell (loads lexicon editing UI)
- **LexTextExe/**: FLEx application

## Interop & Contracts
- **ILexEntry**: Lexical entry object
- **ILexSense**: Lexical sense
- **ILexReference**: Lexical reference (relationships between entries)
- **ILexEntryRef**: Entry reference (complex forms, variants)
- **FLExBridge.exe**: External collaboration tool (invoked via Process.Start)
- **IxCoreColleague**: XCore colleague pattern (FLExBridgeListener)

## Threading & Performance
- **UI thread**: All operations on UI thread
- **FLExBridge**: External process invocation (Send/Receive)

## Config & Feature Flags
- **FLExBridge integration**: Enabled via FLExBridgeListener
- **Homograph numbering**: Configured separately (HomographResetter recalculates)

## Build Information
- **Project file**: LexEdDll.csproj (net462, OutputType=Library)
- **Test project**: LexEdDllTests/
- **Output**: SIL.FieldWorks.XWorks.LexEd.dll
- **Build**: Via top-level FieldWorks.sln or: `msbuild LexEdDll.csproj`
- **Run tests**: `dotnet test LexEdDllTests/`

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
Loaded by xWorks main application shell. Slices/launchers instantiated by data entry framework.

## Test Index
- **Test project**: LexEdDllTests/
- **Run tests**: `dotnet test LexEdDllTests/`
- **Coverage**: FLExBridge integration, entry handlers, reference management

## Usage Hints
- **Lexicon editing**: Entry slices, sense editing, reference management
- **FLExBridge**: File → Send/Receive Project (collaboration workflow)
- **Context menus**: Right-click entries for menu operations (LexEntryMenuHandler)
- **Example sentences**: Tools → Find Example Sentences (FindExampleSentenceDlg)
- **References**: Lexical reference slices for synonyms, antonyms, etc.
- **Collaboration**: FLExBridge integration for team collaboration (Send/Receive)
- **Utilities**: CircularRefBreaker, HomographResetter for data maintenance

## Related Folders
- **LexTextControls/**: Shared lexicon controls (InsertEntryDlg, etc.)
- **LexTextDll/**: Application infrastructure
- **xWorks/**: Main application shell

## References
- **Project file**: LexEdDll.csproj (net462, OutputType=Library)
- **Key C# files**: FLExBridgeListener.cs (1.9K), LexEdStrings.Designer.cs (2K), LexReferenceMultiSlice.cs (1.2K), EntrySequenceReferenceLauncher.cs (656), LexEntryMenuHandler.cs (591), FindExampleSentenceDlg.cs (308), LexEntryInflTypeConverter.cs (231), and 70+ more files
- **Resources**: LexEdStrings.resx (35.8KB), ImageHolder.resx (10KB), LexEntryImages.resx (10.8KB)
- **Test project**: LexEdDllTests/
- **Total lines of code**: 15727
- **Output**: SIL.FieldWorks.XWorks.LexEd.dll
- **Namespace**: Various (SIL.FieldWorks.XWorks.LexEd, etc.)