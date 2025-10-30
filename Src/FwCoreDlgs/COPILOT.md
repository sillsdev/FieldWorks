---
owner: FIXME(set-owner)
last-reviewed: 2025-10-30
status: verified
---

# FwCoreDlgs

## Purpose
Common dialogs used across FieldWorks applications. Provides a standardized set of dialog boxes and UI components for common operations like file selection, configuration, and user input.

## Key Components
### Key Classes
- **FwChooseAnthroListCtrl**
- **ArchiveWithRamp**
- **FwApplyStyleDlg**
- **FwFindReplaceDlg**
- **VwPatternSerializableSettings**
- **SearchKiller**
- **SampleVc**
- **SampleView**
- **RegexHelperMenu**
- **MissingOldFieldWorksDlg**

### Key Interfaces
- **IFindAndReplaceContext**
- **IUtility**
- **IWizardStep**
- **IBasicFindView**
- **IFontDialog**
- **IStylesTab**

## Technology Stack
- C# .NET WinForms
- Standard dialog patterns
- Reusable UI components

## Dependencies
- Depends on: Common (UI infrastructure), FdoUi (data object UI)
- Used by: All major FieldWorks applications (xWorks, LexText)

## Build Information
- Three C# projects: dialogs, controls, and tests
- Build with MSBuild or Visual Studio
- Provides shared dialog infrastructure

## Entry Points
- Provides standard dialogs (file choosers, configuration dialogs, etc.)
- Reusable controls for building consistent UI

## Related Folders
- **Common/** - UI infrastructure that FwCoreDlgs builds upon
- **FdoUi/** - Data object UI that uses FwCoreDlgs
- **xWorks/** - Primary consumer of standard dialogs
- **LexText/** - Uses FwCoreDlgs for common operations

## Code Evidence
*Analysis based on scanning 120 source files*

- **Classes found**: 20 public classes
- **Interfaces found**: 6 public interfaces
- **Namespaces**: AddConverterDlgTests, SIL.FieldWorks.Common.Controls, SIL.FieldWorks.FwCoreDlgControls, SIL.FieldWorks.FwCoreDlgControlsTests, SIL.FieldWorks.FwCoreDlgs

## Interfaces and Data Models

- **IBasicFindView** (interface)
  - Path: `BasicFindDialog.cs`
  - Public interface definition

- **IFindAndReplaceContext** (interface)
  - Path: `FwFindReplaceDlg.cs`
  - Public interface definition

- **IUtility** (interface)
  - Path: `IUtility.cs`
  - Public interface definition

- **IWizardStep** (interface)
  - Path: `FwNewLangProjectModel.cs`
  - Public interface definition

- **AddCnvtrDlg** (class)
  - Path: `AddCnvtrDlg.cs`
  - Public class implementation

- **AddNewUserDlg** (class)
  - Path: `AddNewUserDlg.cs`
  - Public class implementation

- **ChooserTreeView** (class)
  - Path: `ChooserTreeView.cs`
  - Public class implementation

- **CnvtrTypeComboItem** (class)
  - Path: `CnvtrTypeComboItem.cs`
  - Public class implementation

- **ErrorMessageHandler** (class)
  - Path: `ErrorMessage.cs`
  - Public class implementation

- **FwChooseAnthroListModel** (class)
  - Path: `FwChooseAnthroListModel.cs`
  - Public class implementation

- **FwChooserDlg** (class)
  - Path: `FwChooserDlg.cs`
  - Public class implementation

- **FwCoreDlgs** (class)
  - Path: `FwCoreDlgs.Designer.cs`
  - Public class implementation

- **FwDeleteProjectDlg** (class)
  - Path: `FwDeleteProjectDlg.cs`
  - Public class implementation

- **FwFindReplaceDlg** (class)
  - Path: `FwFindReplaceDlg.cs`
  - Public class implementation

- **FwProjPropertiesDlg** (class)
  - Path: `FwProjPropertiesDlg.cs`
  - Public class implementation

- **FwSplashScreen** (class)
  - Path: `FwSplashScreen.cs`
  - Public class implementation

- **FwUserProperties** (class)
  - Path: `FwUserProperties.cs`
  - Public class implementation

- **HelperMenu** (class)
  - Path: `HelperMenu.cs`
  - Public class implementation

- **RegexHelperMenu** (class)
  - Path: `RegexHelperMenu.cs`
  - Public class implementation

- **SampleVc** (class)
  - Path: `ConverterTest.cs`
  - Public class implementation

- **SampleView** (class)
  - Path: `ConverterTest.cs`
  - Public class implementation

- **SearchKiller** (class)
  - Path: `FwFindReplaceDlg.cs`
  - Public class implementation

- **ViewHiddenWritingSystemsModel** (class)
  - Path: `ViewHiddenWritingSystemsModel.cs`
  - Public class implementation

- **VwPatternSerializableSettings** (class)
  - Path: `FwFindReplaceDlg.cs`
  - Public class implementation

- **ConverterType** (enum)
  - Path: `CnvtrTypeComboItem.cs`

- **ErrorMessage** (enum)
  - Path: `ErrorMessage.cs`

- **FileLocationChoice** (enum)
  - Path: `MoveOrCopyFilesDlg.cs`

- **ListChoice** (enum)
  - Path: `FwChooseAnthroListModel.cs`

- **MatchType** (enum)
  - Path: `FwFindReplaceDlg.cs`

- **SampleFrags** (enum)
  - Path: `ConverterTest.cs`

- **SampleTags** (enum)
  - Path: `ConverterTest.cs`

- **StyleChangeType** (enum)
  - Path: `FwStylesDlg.cs`

## References

- **Project files**: FwCoreDlgControls.csproj, FwCoreDlgControlsTests.csproj, FwCoreDlgs.csproj, FwCoreDlgsTests.csproj
- **Target frameworks**: net462
- **Key C# files**: ArchiveWithRamp.cs, AssemblyInfo.cs, ConverterTest.cs, FwApplyStyleDlg.cs, FwChooseAnthroListCtrl.cs, FwCoreDlgs.Designer.cs, FwFindReplaceDlg.cs, FwNewLangProject.Designer.cs, RegexHelperMenu.cs, ValidCharactersDlg.Designer.cs
- **XML data/config**: custompua.xml, xtst.xml
- **Source file count**: 166 files
- **Data file count**: 67 files
