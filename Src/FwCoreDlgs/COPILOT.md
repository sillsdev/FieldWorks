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
