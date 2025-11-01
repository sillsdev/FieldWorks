---
last-reviewed: 2025-11-01
last-verified-commit: HEAD
status: production
---

# FixFwDataDll

## Purpose
Data repair library integrating SIL.LCModel.FixData with FieldWorks UI. Provides IUtility plugin (ErrorFixer) for FwCoreDlgs UtilityDlg framework, FixErrorsDlg for project selection, and helper utilities (FwData, WriteAllObjectsUtility). Used by FwCoreDlgs utility menu and FixFwData command-line tool.

## Key Components

### ErrorFixer.cs (~180 lines)
- **ErrorFixer**: IUtility implementation for UtilityDlg plugin system
  - **Process()**: Shows FixErrorsDlg, invokes FwDataFixer on selected project, logs results to RichText control
  - **Label**: "Find and Fix Errors"
  - **OnSelection()**: Updates UtilityDlg descriptions (WhenDescription, WhatDescription, RedoDescription)
  - Uses FwDataFixer from SIL.LCModel.FixData
- Reports errors to m_dlg.LogRichText with HTML styling

### FixErrorsDlg.cs (~100 lines)
- **FixErrorsDlg**: WinForms dialog for project selection
  - Scans FwDirectoryFinder.ProjectsDirectory for unlocked .fwdata files
  - Single-select CheckedListBox (m_lvProjects)
  - **SelectedProject**: Returns checked project name
  - **m_btnFixLinks_Click**: Sets DialogResult.OK
- Filters out locked projects (.fwdata.lock)

### FwData.cs
- **FwData**: Legacy wrapper/utility (not analyzed in detail)

### WriteAllObjectsUtility.cs
- **WriteAllObjectsUtility**: Export utility for all objects (not analyzed in detail)

## Dependencies
- **SIL.LCModel.FixData**: FwDataFixer (core repair logic)
- **SIL.FieldWorks.FwCoreDlgs**: UtilityDlg, IUtility
- **SIL.FieldWorks.Common.FwUtils**: FwDirectoryFinder
- **SIL.LCModel**: LcmFileHelper
- **Consumer**: FwCoreDlgs utility menu, Utilities/FixFwData command-line tool

## Build Information
- **Project**: FixFwDataDll.csproj
- **Type**: Library (.NET Framework 4.6.2)
- **Output**: FixFwDataDll.dll
- **Namespace**: SIL.FieldWorks.FixData
- **Source files**: ErrorFixer.cs, FixErrorsDlg.cs, FwData.cs, WriteAllObjectsUtility.cs (7 files total including Designer/Strings, ~1065 lines)

## Test Index
No test project found.

## Related Folders
- **Utilities/FixFwData/**: Command-line wrapper for non-interactive repair
- **FwCoreDlgs/**: UtilityDlg framework (IUtility plugin host)
- **SIL.LCModel.FixData**: External library with FwDataFixer

## References
- **SIL.FieldWorks.FwCoreDlgs.IUtility**: Plugin interface
- **SIL.LCModel.FixData.FwDataFixer**: Core repair engine
- **SIL.FieldWorks.Common.FwUtils.FwDirectoryFinder**: Projects directory location
