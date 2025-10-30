---
last-reviewed: 2025-10-30
last-verified-commit: 9611cf70e
status: draft
---

# Lexicon

## Purpose
Lexicon editing and entry management components.
Implements the core lexical database editing interface including entry forms, reference management,
sense hierarchies, and FLEx Bridge integration for version control. Central component for
dictionary and lexicon development workflows in FLEx.

## Key Components
### Key Classes
- **HomographResetter**
- **LexReferencePairView**
- **LexReferencePairVc**
- **LexReferenceCollectionView**
- **LexReferenceCollectionVc**
- **LexReferencePairSlice**
- **GhostLexRefSlice**
- **GhostLexRefLauncher**
- **RevEntrySensesCollectionReferenceSlice**
- **LexReferenceTreeRootView**

### Key Interfaces
- **ILexReferenceSlice**

## Technology Stack
- C# .NET WinForms
- Complex data editing UI
- FLEx Bridge integration

## Dependencies
- Depends on: Cellar (data model), LexText/LexTextControls, Common (UI)
- Used by: LexText/LexTextDll (main application)

## Build Information
- C# class library project
- Build via: `dotnet build LexEdDll.csproj`
- Core lexicon editing functionality

## Entry Points
- Lexicon entry editing interface
- Entry reference management
- FLEx Bridge collaboration features

## Related Folders
- **LexText/LexTextControls/** - Controls used in lexicon editing
- **LexText/LexTextDll/** - Application hosting lexicon features
- **Cellar/** - Lexicon data model
- **xWorks/** - Dictionary configuration and display

## Code Evidence
*Analysis based on scanning 73 source files*

- **Classes found**: 20 public classes
- **Interfaces found**: 1 public interfaces
- **Namespaces**: LexEdDllTests, SIL.FieldWorks.XWorks.LexEd

## Interfaces and Data Models

- **FeatureSystemInflectionFeatureListDlgLauncherSlice** (class)
  - Path: `MsaInflectionFeatureListDlgLauncherSlice.cs`
  - Public class implementation

- **GhostLexRefLauncher** (class)
  - Path: `GhostLexRefSlice.cs`
  - Public class implementation

- **GhostLexRefSlice** (class)
  - Path: `GhostLexRefSlice.cs`
  - Public class implementation

- **HomographResetter** (class)
  - Path: `HomographResetter.cs`
  - Public class implementation

- **LexReferenceCollectionVc** (class)
  - Path: `LexReferenceCollectionView.cs`
  - Public class implementation

- **LexReferenceCollectionView** (class)
  - Path: `LexReferenceCollectionView.cs`
  - Public class implementation

- **LexReferencePairSlice** (class)
  - Path: `LexReferencePairSlice.cs`
  - Public class implementation

- **LexReferencePairVc** (class)
  - Path: `LexReferencePairView.cs`
  - Public class implementation

- **LexReferencePairView** (class)
  - Path: `LexReferencePairView.cs`
  - Public class implementation

- **LexReferenceTreeBranchesVc** (class)
  - Path: `LexReferenceTreeBranchesView.cs`
  - Public class implementation

- **LexReferenceTreeBranchesView** (class)
  - Path: `LexReferenceTreeBranchesView.cs`
  - Public class implementation

- **LexReferenceTreeRootSlice** (class)
  - Path: `LexReferenceTreeRootSlice.cs`
  - Public class implementation

- **LexReferenceTreeRootVc** (class)
  - Path: `LexReferenceTreeRootView.cs`
  - Public class implementation

- **LexReferenceTreeRootView** (class)
  - Path: `LexReferenceTreeRootView.cs`
  - Public class implementation

- **MsaInflectionFeatureListDlgLauncherSlice** (class)
  - Path: `MsaInflectionFeatureListDlgLauncherSlice.cs`
  - Public class implementation

- **PhonologicalFeatureListDlgLauncherSlice** (class)
  - Path: `PhonologicalFeatureListDlgLauncherSlice.cs`
  - Public class implementation

- **RevEntrySensesCollectionReferenceSlice** (class)
  - Path: `RevEntrySensesCollectionReferenceSlice.cs`
  - Public class implementation

- **ReversalIndexEntrySlice** (class)
  - Path: `ReversalIndexEntrySlice.cs`
  - Public class implementation

- **ReversalIndexEntrySliceView** (class)
  - Path: `ReversalIndexEntrySlice.cs`
  - Public class implementation

- **ReversalIndexEntryVc** (class)
  - Path: `ReversalIndexEntrySlice.cs`
  - Public class implementation

## References

- **Project files**: LexEdDll.csproj, LexEdDllTests.csproj
- **Target frameworks**: net462
- **Key C# files**: AssemblyInfo.cs, FLExBridgeFirstSendReceiveInstructionsDlg.Designer.cs, FindExampleSentenceDlg.Designer.cs, GhostLexRefSlice.cs, HomographResetter.cs, LexReferenceCollectionView.cs, LexReferencePairSlice.cs, LexReferencePairView.cs, RevEntrySensesCollectionReferenceSlice.cs, SortReversalSubEntries.cs
- **Source file count**: 77 files
- **Data file count**: 24 files

## Architecture
TBD — populate from code. See auto-generated hints below.

## Interop & Contracts
TBD — populate from code. See auto-generated hints below.

## Threading & Performance
TBD — populate from code. See auto-generated hints below.

## Config & Feature Flags
TBD — populate from code. See auto-generated hints below.

## Test Index
TBD — populate from code. See auto-generated hints below.

## Usage Hints
TBD — populate from code. See auto-generated hints below.
