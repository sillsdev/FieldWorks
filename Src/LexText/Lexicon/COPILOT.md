---
owner: FIXME(set-owner)
last-reviewed: 2025-10-30
status: verified
---

# LexText/Lexicon

## Purpose
Lexicon editing components and features. Provides the main lexicon entry editing interface, reference management, and FLEx Bridge integration for collaboration.

## Key Components
- **LexEdDll.csproj** - Lexicon editing library
- **EntrySequenceReferenceLauncher** - Entry reference management
- **EntrySequenceReferenceSlice** - Reference UI slice
- **CircularRefBreaker.cs** - Prevents circular references
- **DeleteEntriesSensesWithoutInterlinearization.cs** - Cleanup utilities
- **FLExBridgeFirstSendReceiveInstructionsDlg** - FLEx Bridge onboarding
- Lexicon entry editing components


## Key Classes/Interfaces
- **HomographResetter**
- **LexReferencePairView**
- **LexReferencePairVc**
- **LexReferenceCollectionView**
- **LexReferenceCollectionVc**
- **LexReferencePairSlice**
- **GhostLexRefSlice**
- **GhostLexRefLauncher**

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


## References
- **Project Files**: LexEdDll.csproj
- **Key C# Files**: CircularRefBreaker.cs, DeleteEntriesSensesWithoutInterlinearization.cs, EntrySequenceReferenceLauncher.cs, EntrySequenceReferenceSlice.cs, FLExBridgeFirstSendReceiveInstructionsDlg.cs, FLExBridgeListener.cs, FindExampleSentenceDlg.cs, GhostLexRefSlice.cs
