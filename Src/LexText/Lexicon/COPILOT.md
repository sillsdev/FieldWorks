---
owner: FIXME(set-owner)
last-reviewed: 2025-10-30
status: verified
---

# Lexicon

## Purpose
Lexicon editing components and features. Provides the main lexicon entry editing interface, reference management, and FLEx Bridge integration for collaboration.

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
