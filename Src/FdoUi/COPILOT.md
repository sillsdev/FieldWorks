---
last-reviewed: 2025-10-30
last-verified-commit: 9611cf70e
status: draft
---

# FdoUi

## Purpose
User interface components for FieldWorks Data Objects (FDO/LCM).
Provides specialized UI controls for editing and displaying data model objects, including
custom field management dialogs (CustomFieldDlg), chooser dialogs for selecting data objects,
and editors for complex data types. Bridges the data layer with the presentation layer.

## Key Components
### Key Classes
- **InflectionFeatureEditor**
- **WfiWordformUi**
- **InflectionClassEditor**
- **BulkPosEditorBase**
- **BulkPosEditor**
- **LexPronunciationUi**
- **PartOfSpeechUi**
- **CmObjectUi**
- **CmObjectVc**
- **CmAnalObjectVc**

### Key Interfaces
- **IFwGuiControl**

## Technology Stack
- C# .NET WinForms/WPF
- UI controls and dialogs
- Data binding to FieldWorks data model

## Dependencies
- Depends on: Cellar (data model), Common (UI infrastructure), DbExtend (custom fields)
- Used by: xWorks, LexText, and other applications displaying data objects

## Build Information
- C# class library with UI components
- Includes comprehensive test suite
- Build with MSBuild or Visual Studio

## Entry Points
- Provides UI controls and dialogs for data object management
- Custom field editors and data visualization components

## Related Folders
- **Cellar/** - Core data model that FdoUi visualizes
- **DbExtend/** - Custom field extensions that FdoUi provides UI for
- **FwCoreDlgs/** - Additional dialogs that work with FdoUi components
- **xWorks/** - Uses FdoUi for data object display and editing

## Code Evidence
*Analysis based on scanning 25 source files*

- **Classes found**: 20 public classes
- **Interfaces found**: 1 public interfaces
- **Namespaces**: SIL.FieldWorks.FdoUi, SIL.FieldWorks.FdoUi.Dialogs

## Interfaces and Data Models

- **IFwGuiControl** (interface)
  - Path: `FdoUiCore.cs`
  - Public interface definition

- **BulkPosEditor** (class)
  - Path: `BulkPosEditor.cs`
  - Public class implementation

- **BulkPosEditorBase** (class)
  - Path: `BulkPosEditor.cs`
  - Public class implementation

- **CmAnalObjectVc** (class)
  - Path: `FdoUiCore.cs`
  - Public class implementation

- **CmNameAbbrObjVc** (class)
  - Path: `FdoUiCore.cs`
  - Public class implementation

- **CmNamedObjVc** (class)
  - Path: `FdoUiCore.cs`
  - Public class implementation

- **CmObjectUi** (class)
  - Path: `FdoUiCore.cs`
  - Public class implementation

- **CmObjectVc** (class)
  - Path: `FdoUiCore.cs`
  - Public class implementation

- **CmPossRefVc** (class)
  - Path: `FdoUiCore.cs`
  - Public class implementation

- **CmPossibilityUi** (class)
  - Path: `FdoUiCore.cs`
  - Public class implementation

- **CmVernObjectVc** (class)
  - Path: `FdoUiCore.cs`
  - Public class implementation

- **InflectionClassEditor** (class)
  - Path: `InflectionClassEditor.cs`
  - Public class implementation

- **InflectionFeatureEditor** (class)
  - Path: `InflectionFeatureEditor.cs`
  - Public class implementation

- **LexPronunciationUi** (class)
  - Path: `LexPronunciationUi.cs`
  - Public class implementation

- **MoDerivAffMsaUi** (class)
  - Path: `FdoUiCore.cs`
  - Public class implementation

- **MoInflAffMsaUi** (class)
  - Path: `FdoUiCore.cs`
  - Public class implementation

- **MoMorphSynAnalysisUi** (class)
  - Path: `FdoUiCore.cs`
  - Public class implementation

- **MoStemMsaUi** (class)
  - Path: `FdoUiCore.cs`
  - Public class implementation

- **MsaVc** (class)
  - Path: `FdoUiCore.cs`
  - Public class implementation

- **PartOfSpeechUi** (class)
  - Path: `PartOfSpeechUi.cs`
  - Public class implementation

- **WfiWordformUi** (class)
  - Path: `WfiWordformUi.cs`
  - Public class implementation

- **VcFrags** (enum)
  - Path: `FdoUiCore.cs`

## References

- **Project files**: FdoUi.csproj, FdoUiTests.csproj
- **Target frameworks**: net462
- **Key C# files**: AssemblyInfo.cs, BulkPosEditor.cs, FdoUiCore.cs, InflectionClassEditor.cs, InflectionFeatureEditor.cs, LexPronunciationUi.cs, PartOfSpeechUi.cs, PhonologicalFeatureEditor.cs, TypeAheadSupportVc.cs, WfiWordformUi.cs
- **Source file count**: 31 files
- **Data file count**: 10 files

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

## References (auto-generated hints)
- Project files:
  - Src\FdoUi\FdoUi.csproj
  - Src\FdoUi\FdoUiTests\FdoUiTests.csproj
- Key C# files:
  - Src\FdoUi\AssemblyInfo.cs
  - Src\FdoUi\BulkPosEditor.cs
  - Src\FdoUi\Dialogs\CantRestoreLinkedFilesToOriginalLocation.Designer.cs
  - Src\FdoUi\Dialogs\CantRestoreLinkedFilesToOriginalLocation.cs
  - Src\FdoUi\Dialogs\ConfirmDeleteObjectDlg.cs
  - Src\FdoUi\Dialogs\ConflictingSaveDlg.Designer.cs
  - Src\FdoUi\Dialogs\ConflictingSaveDlg.cs
  - Src\FdoUi\Dialogs\FilesToRestoreAreOlder.Designer.cs
  - Src\FdoUi\Dialogs\FilesToRestoreAreOlder.cs
  - Src\FdoUi\Dialogs\MergeObjectDlg.cs
  - Src\FdoUi\Dialogs\RelatedWords.cs
  - Src\FdoUi\Dialogs\RestoreLinkedFilesToProjectsFolder.Designer.cs
  - Src\FdoUi\Dialogs\RestoreLinkedFilesToProjectsFolder.cs
  - Src\FdoUi\Dialogs\SummaryDialogForm.cs
  - Src\FdoUi\DummyCmObject.cs
  - Src\FdoUi\FdoUiCore.cs
  - Src\FdoUi\FdoUiStrings.Designer.cs
  - Src\FdoUi\FdoUiTests\FdoUiTests.cs
  - Src\FdoUi\FsFeatDefnUi.cs
  - Src\FdoUi\FwLcmUI.cs
  - Src\FdoUi\InflectionClassEditor.cs
  - Src\FdoUi\InflectionFeatureEditor.cs
  - Src\FdoUi\LexEntryUi.cs
  - Src\FdoUi\LexPronunciationUi.cs
  - Src\FdoUi\PartOfSpeechUi.cs
- Data contracts/transforms:
  - Src\FdoUi\Dialogs\CantRestoreLinkedFilesToOriginalLocation.resx
  - Src\FdoUi\Dialogs\ConfirmDeleteObjectDlg.resx
  - Src\FdoUi\Dialogs\ConflictingSaveDlg.resx
  - Src\FdoUi\Dialogs\FilesToRestoreAreOlder.resx
  - Src\FdoUi\Dialogs\MergeObjectDlg.resx
  - Src\FdoUi\Dialogs\RelatedWords.resx
  - Src\FdoUi\Dialogs\RestoreLinkedFilesToProjectsFolder.resx
  - Src\FdoUi\Dialogs\SummaryDialogForm.resx
  - Src\FdoUi\FdoUiStrings.resx
  - Src\FdoUi\Properties\Resources.resx
