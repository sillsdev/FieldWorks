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

## Architecture
C# library with 77 source files. Contains 1 subprojects: LexEdDll.

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

## Interop & Contracts
Uses COM for cross-boundary calls.

## Threading & Performance
Single-threaded or thread-agnostic code. No explicit threading detected.

## Config & Feature Flags
No explicit configuration or feature flags detected.

## Build Information
- C# class library project
- Build via: `dotnet build LexEdDll.csproj`
- Core lexicon editing functionality

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

## Entry Points
- Lexicon entry editing interface
- Entry reference management
- FLEx Bridge collaboration features

## Test Index
Test projects: LexEdDllTests. 9 test files. Run via: `dotnet test` or Test Explorer in Visual Studio.

## Usage Hints
Library component. Reference in consuming projects. See Dependencies section for integration points.

## Related Folders
- **LexText/LexTextControls/** - Controls used in lexicon editing
- **LexText/LexTextDll/** - Application hosting lexicon features
- **Cellar/** - Lexicon data model
- **xWorks/** - Dictionary configuration and display

## References

- **Project files**: LexEdDll.csproj, LexEdDllTests.csproj
- **Target frameworks**: net462
- **Key C# files**: AssemblyInfo.cs, FLExBridgeFirstSendReceiveInstructionsDlg.Designer.cs, FindExampleSentenceDlg.Designer.cs, GhostLexRefSlice.cs, HomographResetter.cs, LexReferenceCollectionView.cs, LexReferencePairSlice.cs, LexReferencePairView.cs, RevEntrySensesCollectionReferenceSlice.cs, SortReversalSubEntries.cs
- **Source file count**: 77 files
- **Data file count**: 24 files

## References (auto-generated hints)
- Project files:
  - LexText/Lexicon/LexEdDll.csproj
  - LexText/Lexicon/LexEdDllTests/LexEdDllTests.csproj
- Key C# files:
  - LexText/Lexicon/AssemblyInfo.cs
  - LexText/Lexicon/CircularRefBreaker.cs
  - LexText/Lexicon/DeleteEntriesSensesWithoutInterlinearization.cs
  - LexText/Lexicon/EntrySequenceReferenceLauncher.cs
  - LexText/Lexicon/EntrySequenceReferenceSlice.cs
  - LexText/Lexicon/FLExBridgeFirstSendReceiveInstructionsDlg.Designer.cs
  - LexText/Lexicon/FLExBridgeFirstSendReceiveInstructionsDlg.cs
  - LexText/Lexicon/FLExBridgeListener.cs
  - LexText/Lexicon/FindExampleSentenceDlg.Designer.cs
  - LexText/Lexicon/FindExampleSentenceDlg.cs
  - LexText/Lexicon/GhostLexRefSlice.cs
  - LexText/Lexicon/GoldEticGuidFixer.cs
  - LexText/Lexicon/HomographResetter.cs
  - LexText/Lexicon/ImageHolder.cs
  - LexText/Lexicon/LexEdDllTests/CircularRefBreakerTests.cs
  - LexText/Lexicon/LexEdDllTests/DummyReversalIndexEntrySlice.cs
  - LexText/Lexicon/LexEdDllTests/FlexBridgeListenerTests.cs
  - LexText/Lexicon/LexEdDllTests/GoldEticGuidFixerTests.cs
  - LexText/Lexicon/LexEdDllTests/LexEntryChangeHandlerTests.cs
  - LexText/Lexicon/LexEdDllTests/LexReferenceTreeRootLauncherTests.cs
  - LexText/Lexicon/LexEdDllTests/Properties/AssemblyInfo.cs
  - LexText/Lexicon/LexEdDllTests/ReversalEntryBulkEditTests.cs
  - LexText/Lexicon/LexEdDllTests/ReversalEntryViewTests.cs
  - LexText/Lexicon/LexEdDllTests/SortReversalSubEntriesTests.cs
  - LexText/Lexicon/LexEdDllTests/TestUtils.cs
- Data contracts/transforms:
  - LexText/Lexicon/EntrySequenceReferenceLauncher.resx
  - LexText/Lexicon/FLExBridgeFirstSendReceiveInstructionsDlg.resx
  - LexText/Lexicon/FindExampleSentenceDlg.resx
  - LexText/Lexicon/ImageHolder.resx
  - LexText/Lexicon/LexEdStrings.resx
  - LexText/Lexicon/LexEntryImages.resx
  - LexText/Lexicon/LexReferenceTreeRootLauncher.resx
  - LexText/Lexicon/LexReferenceTreeRootView.resx
  - LexText/Lexicon/MSADlgLauncher.resx
  - LexText/Lexicon/MSADlgLauncherSlice.resx
  - LexText/Lexicon/MSADlglauncherView.resx
  - LexText/Lexicon/MsaInflectionFeatureListDlgLauncher.resx
  - LexText/Lexicon/MsaInflectionFeatureListDlgLauncherSlice.resx
  - LexText/Lexicon/MsaInflectionFeatureListDlgLauncherView.resx
  - LexText/Lexicon/PhonologicalFeatureListDlgLauncher.resx
  - LexText/Lexicon/PhonologicalFeatureListDlgLauncherSlice.resx
  - LexText/Lexicon/Resources.resx
  - LexText/Lexicon/RevEntrySensesCollectionReferenceLauncher.resx
  - LexText/Lexicon/RevEntrySensesCollectionReferenceSlice.resx
  - LexText/Lexicon/RevEntrySensesCollectionReferenceView.resx
  - LexText/Lexicon/ReversalEntryGoDlg.resx
  - LexText/Lexicon/ReversalIndexEntryFormSlice.resx
  - LexText/Lexicon/ReversalIndexEntrySlice.resx
  - LexText/Lexicon/SwapLexemeWithAllomorphDlg.resx
## Code Evidence
*Analysis based on scanning 73 source files*

- **Classes found**: 20 public classes
- **Interfaces found**: 1 public interfaces
- **Namespaces**: LexEdDllTests, SIL.FieldWorks.XWorks.LexEd
