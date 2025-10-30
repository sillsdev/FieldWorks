---
owner: FIXME(set-owner)
last-reviewed: 2025-10-30
status: verified
---

# LexText/LexTextControls

## Purpose
Lexicon UI controls library. Provides specialized controls and dialogs for lexicon editing, including allomorph addition, sense management, and lexicon import/export wizards.

## Key Components
- **LexTextControls.csproj** - Controls library
- **AddAllomorphDlg** - Add allomorph dialog
- **AddNewSenseDlg** - Add sense dialog
- **AddWritingSystemButton** - Writing system addition control
- **BaseGoDlg** - Base dialog for lexicon operations
- **LexImportWizard** - Lexicon import wizard
- **LiftMerger** - LIFT format merging
- **LiftExporter** - LIFT format export
- **WordsSfmImport** - SFM format import
- **LexTextControlsTests/** - Comprehensive test suite

## Technology Stack
- C# .NET WinForms
- Dialog and wizard UI patterns
- LIFT (Lexicon Interchange FormaT) support

## Dependencies
- Depends on: Cellar (data model), Common (UI infrastructure), LexText core
- Used by: LexText/LexTextDll, LexText/Lexicon

## Build Information
- C# class library project
- Build via: `dotnet build LexTextControls.csproj`
- Includes extensive test suite

## Testing
- Run tests: `dotnet test LexTextControls/LexTextControlsTests/`
- Tests cover dialogs, wizards, and import/export

## Entry Points
- Lexicon editing dialogs
- Import/export wizards
- Specialized lexicon controls

## Related Folders
- **LexText/Lexicon/** - Uses these controls for lexicon editing
- **LexText/LexTextDll/** - Core functionality using controls
- **FwCoreDlgs/** - Common dialog infrastructure
- **Common/Controls/** - Base control infrastructure


## References
- **Project Files**: LexTextControls.csproj
- **Key C# Files**: AddAllomorphDlg.cs, AddNewSenseDlg.cs, AddWritingSystemButton.cs, BaseGoDlg.cs, CombineImportDlg.cs, ConfigureHomographDlg.cs, ContextMenuRequestedEventArgs.cs, EntryDlgListener.cs
