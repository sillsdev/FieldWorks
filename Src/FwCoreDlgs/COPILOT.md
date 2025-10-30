---
last-reviewed: 2025-10-30
last-verified-commit: 9611cf70e
status: draft
---

# FwCoreDlgs

## Purpose
Common dialogs and UI components shared across FieldWorks applications.
Includes standardized dialog boxes for file operations (BackupProjectDlg, RestoreProjectDlg),
writing system configuration (FwWritingSystemSetupDlg), project management, and user preferences.
Ensures consistent user experience across different parts of FieldWorks.

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

## Architecture
C# library with 166 source files. Contains 2 subprojects: FwCoreDlgs, FwCoreDlgControls.

## Interop & Contracts
Uses Marshaling, COM for cross-boundary calls.

## Threading & Performance
Threading model: explicit threading, synchronization.

## Config & Feature Flags
Config files: App.config.

## Test Index
Test projects: FwCoreDlgsTests, FwCoreDlgControlsTests. 26 test files. Run via: `dotnet test` or Test Explorer in Visual Studio.

## Usage Hints
Library component. Reference in consuming projects. See Dependencies section for integration points.

## References (auto-generated hints)
- Project files:
  - Src\FwCoreDlgs\FwCoreDlgControls\FwCoreDlgControls.csproj
  - Src\FwCoreDlgs\FwCoreDlgControls\FwCoreDlgControlsTests\FwCoreDlgControlsTests.csproj
  - Src\FwCoreDlgs\FwCoreDlgs.csproj
  - Src\FwCoreDlgs\FwCoreDlgsTests\FwCoreDlgsTests.csproj
- Key C# files:
  - Src\FwCoreDlgs\AddCnvtrDlg.cs
  - Src\FwCoreDlgs\AddConverterDlgStrings.Designer.cs
  - Src\FwCoreDlgs\AddConverterResources.Designer.cs
  - Src\FwCoreDlgs\AddNewUserDlg.cs
  - Src\FwCoreDlgs\AddNewVernLangWarningDlg.Designer.cs
  - Src\FwCoreDlgs\AddNewVernLangWarningDlg.cs
  - Src\FwCoreDlgs\AdvancedEncProps.cs
  - Src\FwCoreDlgs\AdvancedScriptRegionVariantModel.cs
  - Src\FwCoreDlgs\AdvancedScriptRegionVariantView.Designer.cs
  - Src\FwCoreDlgs\AdvancedScriptRegionVariantView.cs
  - Src\FwCoreDlgs\ArchiveWithRamp.Designer.cs
  - Src\FwCoreDlgs\ArchiveWithRamp.cs
  - Src\FwCoreDlgs\AssemblyInfo.cs
  - Src\FwCoreDlgs\BackupProjectSettings.cs
  - Src\FwCoreDlgs\BackupRestore\BackupProjectDlg.Designer.cs
  - Src\FwCoreDlgs\BackupRestore\BackupProjectDlg.cs
  - Src\FwCoreDlgs\BackupRestore\BackupProjectPresenter.cs
  - Src\FwCoreDlgs\BackupRestore\ChangeDefaultBackupDir.Designer.cs
  - Src\FwCoreDlgs\BackupRestore\ChangeDefaultBackupDir.cs
  - Src\FwCoreDlgs\BackupRestore\IBackupProjectView.cs
  - Src\FwCoreDlgs\BackupRestore\OverwriteExistingProject.Designer.cs
  - Src\FwCoreDlgs\BackupRestore\OverwriteExistingProject.cs
  - Src\FwCoreDlgs\BackupRestore\RestoreProjectDlg.Designer.cs
  - Src\FwCoreDlgs\BackupRestore\RestoreProjectDlg.cs
  - Src\FwCoreDlgs\BackupRestore\RestoreProjectPresenter.cs
- Data contracts/transforms:
  - Src\FwCoreDlgs\AddCnvtrDlg.resx
  - Src\FwCoreDlgs\AddConverterDlgStrings.resx
  - Src\FwCoreDlgs\AddConverterResources.resx
  - Src\FwCoreDlgs\AddNewUserDlg.resx
  - Src\FwCoreDlgs\AddNewVernLangWarningDlg.resx
  - Src\FwCoreDlgs\AdvancedEncProps.resx
  - Src\FwCoreDlgs\AdvancedScriptRegionVariantView.resx
  - Src\FwCoreDlgs\ArchiveWithRamp.resx
  - Src\FwCoreDlgs\BackupRestore\BackupProjectDlg.resx
  - Src\FwCoreDlgs\BackupRestore\ChangeDefaultBackupDir.resx
  - Src\FwCoreDlgs\BackupRestore\OverwriteExistingProject.resx
  - Src\FwCoreDlgs\BackupRestore\RestoreProjectDlg.resx
  - Src\FwCoreDlgs\BasicFindDialog.resx
  - Src\FwCoreDlgs\CharContextCtrl.resx
  - Src\FwCoreDlgs\ChooseLangProjectDialog.resx
  - Src\FwCoreDlgs\CnvtrPropertiesCtrl.resx
  - Src\FwCoreDlgs\ConverterTest.resx
  - Src\FwCoreDlgs\DeleteWritingSystemWarningDialog.resx
  - Src\FwCoreDlgs\FWCoreDlgsErrors.resx
  - Src\FwCoreDlgs\FwApplyStyleDlg.resx
  - Src\FwCoreDlgs\FwChooseAnthroListCtrl.resx
  - Src\FwCoreDlgs\FwChooserDlg.resx
  - Src\FwCoreDlgs\FwCoreDlgControls\ConfigParentNode.resx
  - Src\FwCoreDlgs\FwCoreDlgControls\ConfigSenseLayout.resx
  - Src\FwCoreDlgs\FwCoreDlgControls\DefaultFontsControl.resx
