---
last-reviewed: 2025-11-01
last-reviewed-tree: 68376b62bdef7bb1e14508c7e65b15e51b3f17f978d4b2194d8ab87f56dd549b
status: production
---

# FixFwDataDll

## Purpose
Data repair library integrating SIL.LCModel.FixData with FieldWorks UI. Provides IUtility plugin (ErrorFixer) for FwCoreDlgs UtilityDlg framework, FixErrorsDlg for project selection, and helper utilities (FwData, WriteAllObjectsUtility). Used by FwCoreDlgs utility menu and FixFwData command-line tool.

## Architecture
TBD - populate from code. See auto-generated hints below.

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

## Technology Stack
TBD - populate from code. See auto-generated hints below.

## Dependencies
- **SIL.LCModel.FixData**: FwDataFixer (core repair logic)
- **SIL.FieldWorks.FwCoreDlgs**: UtilityDlg, IUtility
- **SIL.FieldWorks.Common.FwUtils**: FwDirectoryFinder
- **SIL.LCModel**: LcmFileHelper
- **Consumer**: FwCoreDlgs utility menu, Utilities/FixFwData command-line tool

## Interop & Contracts
TBD - populate from code. See auto-generated hints below.

## Threading & Performance
TBD - populate from code. See auto-generated hints below.

## Config & Feature Flags
TBD - populate from code. See auto-generated hints below.

## Build Information
- **Project**: FixFwDataDll.csproj
- **Type**: Library (.NET Framework 4.6.2)
- **Output**: FixFwDataDll.dll
- **Namespace**: SIL.FieldWorks.FixData
- **Source files**: ErrorFixer.cs, FixErrorsDlg.cs, FwData.cs, WriteAllObjectsUtility.cs (7 files total including Designer/Strings, ~1065 lines)

## Interfaces and Data Models
TBD - populate from code. See auto-generated hints below.

## Entry Points
TBD - populate from code. See auto-generated hints below.

## Test Index
No test project found.

## Usage Hints
TBD - populate from code. See auto-generated hints below.

## Related Folders
- **Utilities/FixFwData/**: Command-line wrapper for non-interactive repair
- **FwCoreDlgs/**: UtilityDlg framework (IUtility plugin host)
- **SIL.LCModel.FixData**: External library with FwDataFixer

## References
- **SIL.FieldWorks.FwCoreDlgs.IUtility**: Plugin interface
- **SIL.LCModel.FixData.FwDataFixer**: Core repair engine
- **SIL.FieldWorks.Common.FwUtils.FwDirectoryFinder**: Projects directory location

## References (auto-generated hints)
- Project files:
  - Utilities/FixFwDataDll/FixFwDataDll.csproj
- Key C# files:
  - Utilities/FixFwDataDll/ErrorFixer.cs
  - Utilities/FixFwDataDll/FixErrorsDlg.Designer.cs
  - Utilities/FixFwDataDll/FixErrorsDlg.cs
  - Utilities/FixFwDataDll/FwData.cs
  - Utilities/FixFwDataDll/Properties/AssemblyInfo.cs
  - Utilities/FixFwDataDll/Strings.Designer.cs
  - Utilities/FixFwDataDll/WriteAllObjectsUtility.cs
- Data contracts/transforms:
  - Utilities/FixFwDataDll/FixErrorsDlg.resx
  - Utilities/FixFwDataDll/Strings.resx
