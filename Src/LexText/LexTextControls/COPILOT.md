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

## Architecture
C# library with 100 source files. Contains 1 subprojects: LexTextControls.

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

## Interop & Contracts
Uses COM for cross-boundary calls.

## Threading & Performance
Threading model: UI thread marshaling.

## Config & Feature Flags
No explicit configuration or feature flags detected.

## Build Information
- C# class library project
- Build via: `dotnet build LexTextControls.csproj`
- Includes extensive test suite

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

## Entry Points
- Lexicon editing dialogs
- Import/export wizards
- Specialized lexicon controls

## Test Index
Test projects: LexTextControlsTests. 7 test files. Run via: `dotnet test` or Test Explorer in Visual Studio.

## Usage Hints
Library component. Reference in consuming projects. See Dependencies section for integration points.

## Related Folders
- **LexText/Lexicon/** - Uses these controls for lexicon editing
- **LexText/LexTextDll/** - Core functionality using controls
- **FwCoreDlgs/** - Common dialog infrastructure
- **Common/Controls/** - Base control infrastructure

## References

- **Project files**: LexTextControls.csproj, LexTextControlsTests.csproj
- **Target frameworks**: net462
- **Key C# files**: AddAllomorphDlg.cs, AssemblyInfo.cs, LexImportWizard.cs, LexImportWizardHelpers.cs, LexReferenceDetailsDlg.cs, MasterCategoryListDlg.cs, OccurrenceDlg.cs, PhonologicalFeatureChooserDlg.cs, RecordGoDlg.cs, Sfm2FlexTextWords.cs
- **XML data/config**: FeatureSystem2.xml, FeatureSystem3.xml
- **Source file count**: 100 files
- **Data file count**: 47 files

## References (auto-generated hints)
- Project files:
  - LexText/LexTextControls/LexTextControls.csproj
  - LexText/LexTextControls/LexTextControlsTests/LexTextControlsTests.csproj
- Key C# files:
  - LexText/LexTextControls/AddAllomorphDlg.cs
  - LexText/LexTextControls/AddNewSenseDlg.cs
  - LexText/LexTextControls/AddWritingSystemButton.Designer.cs
  - LexText/LexTextControls/AddWritingSystemButton.cs
  - LexText/LexTextControls/AssemblyInfo.cs
  - LexText/LexTextControls/BaseGoDlg.cs
  - LexText/LexTextControls/CombineImportDlg.Designer.cs
  - LexText/LexTextControls/CombineImportDlg.cs
  - LexText/LexTextControls/ConfigureHomographDlg.cs
  - LexText/LexTextControls/ConfigureHomographDlg.designer.cs
  - LexText/LexTextControls/ContextMenuRequestedEventArgs.cs
  - LexText/LexTextControls/DataNotebook/AnthroFieldMappingDlg.Designer.cs
  - LexText/LexTextControls/DataNotebook/AnthroFieldMappingDlg.cs
  - LexText/LexTextControls/DataNotebook/DateFieldOptions.Designer.cs
  - LexText/LexTextControls/DataNotebook/DateFieldOptions.cs
  - LexText/LexTextControls/DataNotebook/DiscardOptions.Designer.cs
  - LexText/LexTextControls/DataNotebook/DiscardOptions.cs
  - LexText/LexTextControls/DataNotebook/ImportCharMappingDlg.Designer.cs
  - LexText/LexTextControls/DataNotebook/ImportCharMappingDlg.cs
  - LexText/LexTextControls/DataNotebook/ImportDateFormatDlg.Designer.cs
  - LexText/LexTextControls/DataNotebook/ImportDateFormatDlg.cs
  - LexText/LexTextControls/DataNotebook/ImportEncCvtrDlg.Designer.cs
  - LexText/LexTextControls/DataNotebook/ImportEncCvtrDlg.cs
  - LexText/LexTextControls/DataNotebook/ImportMatchReplaceDlg.Designer.cs
  - LexText/LexTextControls/DataNotebook/ImportMatchReplaceDlg.cs
- Data contracts/transforms:
  - LexText/LexTextControls/AddAllomorphDlg.resx
  - LexText/LexTextControls/AddNewSenseDlg.resx
  - LexText/LexTextControls/BaseGoDlg.resx
  - LexText/LexTextControls/CombineImportDlg.resx
  - LexText/LexTextControls/ConfigureHomographDlg.resx
  - LexText/LexTextControls/DataNotebook/AnthroFieldMappingDlg.resx
  - LexText/LexTextControls/DataNotebook/DateFieldOptions.resx
  - LexText/LexTextControls/DataNotebook/DiscardOptions.resx
  - LexText/LexTextControls/DataNotebook/ImportCharMappingDlg.resx
  - LexText/LexTextControls/DataNotebook/ImportDateFormatDlg.resx
  - LexText/LexTextControls/DataNotebook/ImportEncCvtrDlg.resx
  - LexText/LexTextControls/DataNotebook/ImportMatchReplaceDlg.resx
  - LexText/LexTextControls/DataNotebook/LinkFieldOptions.resx
  - LexText/LexTextControls/DataNotebook/ListRefFieldOptions.resx
  - LexText/LexTextControls/DataNotebook/NotebookImportWiz.resx
  - LexText/LexTextControls/DataNotebook/StringFieldOptions.resx
  - LexText/LexTextControls/DataNotebook/TextFieldOptions.resx
  - LexText/LexTextControls/FeatureStructureTreeView.resx
  - LexText/LexTextControls/InsertEntryDlg.resx
  - LexText/LexTextControls/InsertRecordDlg.resx
  - LexText/LexTextControls/InsertionControl.resx
  - LexText/LexTextControls/LexImportWizard.resx
  - LexText/LexTextControls/LexImportWizardCharMarkerDlg.resx
  - LexText/LexTextControls/LexImportWizardLanguage.resx
  - LexText/LexTextControls/LexImportWizardMarker.resx
## Code Evidence
*Analysis based on scanning 81 source files*

- **Classes found**: 20 public classes
- **Interfaces found**: 3 public interfaces
- **Namespaces**: LexTextControlsTests, SIL.FieldWorks.LexText.Controls, SIL.FieldWorks.LexText.Controls.DataNotebook, SIL.FieldWorks.XWorks.MorphologyEditor, to
