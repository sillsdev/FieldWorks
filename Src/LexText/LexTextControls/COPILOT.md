---
owner: FIXME(set-owner)
last-reviewed: 2025-10-30
status: verified
---

# LexTextControls

## Purpose
Lexicon UI controls library. Provides specialized controls and dialogs for lexicon editing, including allomorph addition, sense management, and lexicon import/export wizards.

## Key Components
### Key Classes
- **InterlinearMapping**
- **Sfm2FlexTextWordsFrag**
- **Sfm2FlexTextMappingBase**
- **Sfm2FlexTextBase**
- **PhonologicalFeatureChooserDlg**
- **LexImportWizard**
- **AddAllomorphDlg**
- **MasterCategoryListDlg**
- **LexImportField**
- **LexImportFields**

### Key Interfaces
- **IFwExtension**
- **ICmLiftObject**
- **IPatternControl**

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

## Entry Points
- Lexicon editing dialogs
- Import/export wizards
- Specialized lexicon controls

## Related Folders
- **LexText/Lexicon/** - Uses these controls for lexicon editing
- **LexText/LexTextDll/** - Core functionality using controls
- **FwCoreDlgs/** - Common dialog infrastructure
- **Common/Controls/** - Base control infrastructure

## Code Evidence
*Analysis based on scanning 81 source files*

- **Classes found**: 20 public classes
- **Interfaces found**: 3 public interfaces
- **Namespaces**: LexTextControlsTests, SIL.FieldWorks.LexText.Controls, SIL.FieldWorks.LexText.Controls.DataNotebook, SIL.FieldWorks.XWorks.MorphologyEditor, to
