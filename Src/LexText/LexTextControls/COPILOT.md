---
last-reviewed: 2025-10-30
last-verified-commit: 9611cf70e
status: draft
---

# LexTextControls

## Purpose
Lexicon UI controls library for FLEx editing interfaces.
Provides specialized controls and dialogs for lexicon management including import/export wizards
(LexImportWizard, FlexLiftMerger), entry editing components, chooser dialogs, and dictionary
configuration interfaces. Essential UI building blocks for the FLEx lexicon editor.

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

## Interfaces and Data Models

- **IFwExtension** (interface)
  - Path: `IFwExtension.cs`
  - Public interface definition

- **AddAllomorphDlg** (class)
  - Path: `AddAllomorphDlg.cs`
  - Public class implementation

- **ContentMapping** (class)
  - Path: `LexImportWizardHelpers.cs`
  - Public class implementation

- **InsertEntryDlgListener** (class)
  - Path: `EntryDlgListener.cs`
  - Public class implementation

- **InterlinearMapping** (class)
  - Path: `Sfm2FlexTextWords.cs`
  - Public class implementation

- **LexImport** (class)
  - Path: `LexImport.cs`
  - Public class implementation

- **LexImportField** (class)
  - Path: `LexImportWizardHelpers.cs`
  - Public class implementation

- **LexImportFields** (class)
  - Path: `LexImportWizardHelpers.cs`
  - Public class implementation

- **LexImportWizard** (class)
  - Path: `LexImportWizard.cs`
  - Public class implementation

- **LexReferenceDetailsDlg** (class)
  - Path: `LexReferenceDetailsDlg.cs`
  - Public class implementation

- **ListViewItemComparer** (class)
  - Path: `LexImportWizardHelpers.cs`
  - Public class implementation

- **MSAGroupBox** (class)
  - Path: `MSAGroupBox.cs`
  - Public class implementation

- **MarkerPresenter** (class)
  - Path: `LexImportWizardHelpers.cs`
  - Public class implementation

- **MasterCategoryListDlg** (class)
  - Path: `MasterCategoryListDlg.cs`
  - Public class implementation

- **MergeEntryDlgListener** (class)
  - Path: `EntryDlgListener.cs`
  - Public class implementation

- **OccurrenceDlg** (class)
  - Path: `OccurrenceDlg.cs`
  - Public class implementation

- **PhonologicalFeatureChooserDlg** (class)
  - Path: `PhonologicalFeatureChooserDlg.cs`
  - Public class implementation

- **RecordGoDlg** (class)
  - Path: `RecordGoDlg.cs`
  - Public class implementation

- **Sfm2FlexTextBase** (class)
  - Path: `Sfm2FlexTextWords.cs`
  - Public class implementation

- **Sfm2FlexTextMappingBase** (class)
  - Path: `Sfm2FlexTextWords.cs`
  - Public class implementation

- **Sfm2FlexTextWordsFrag** (class)
  - Path: `Sfm2FlexTextWords.cs`
  - Public class implementation

- **CFChanges** (enum)
  - Path: `LexImportWizardHelpers.cs`

- **ImageKind** (enum)
  - Path: `FeatureStructureTreeView.cs`

- **InterlinDestination** (enum)
  - Path: `Sfm2FlexTextWords.cs`

- **MergeStyle** (enum)
  - Path: `LiftMerger.cs`

- **MorphTypeFilterType** (enum)
  - Path: `InsertEntryDlg.cs`

- **NodeKind** (enum)
  - Path: `FeatureStructureTreeView.cs`

## References

- **Project files**: LexTextControls.csproj, LexTextControlsTests.csproj
- **Target frameworks**: net462
- **Key C# files**: AddAllomorphDlg.cs, AssemblyInfo.cs, LexImportWizard.cs, LexImportWizardHelpers.cs, LexReferenceDetailsDlg.cs, MasterCategoryListDlg.cs, OccurrenceDlg.cs, PhonologicalFeatureChooserDlg.cs, RecordGoDlg.cs, Sfm2FlexTextWords.cs
- **XML data/config**: FeatureSystem2.xml, FeatureSystem3.xml
- **Source file count**: 100 files
- **Data file count**: 47 files

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
