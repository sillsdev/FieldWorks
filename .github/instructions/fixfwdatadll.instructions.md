---
applyTo: "Src/Utilities/FixFwDataDll/**"
name: "fixfwdatadll.instructions"
description: "Auto-generated concise instructions from COPILOT.md for FixFwDataDll"
---

# FixFwDataDll (Concise)

## Purpose & Scope
Summarized key points from COPILOT.md

## Key Rules
- **ErrorFixer**: IUtility implementation for UtilityDlg plugin system
- **Process()**: Shows FixErrorsDlg, invokes FwDataFixer on selected project, logs results to RichText control
- **Label**: "Find and Fix Errors"
- **OnSelection()**: Updates UtilityDlg descriptions (WhenDescription, WhatDescription, RedoDescription)
- Uses FwDataFixer from SIL.LCModel.FixData
- Reports errors to m_dlg.LogRichText with HTML styling

## Example (from summary)

---
last-reviewed: 2025-11-01
last-reviewed-tree: 68376b62bdef7bb1e14508c7e65b15e51b3f17f978d4b2194d8ab87f56dd549b
status: production
---

# FixFwDataDll

## Purpose
Data repair library integrating SIL.LCModel.FixData with FieldWorks UI. Provides IUtility plugin (ErrorFixer) for FwCoreDlgs UtilityDlg framework, FixErrorsDlg for project selection, and helper utilities (FwData, WriteAllObjectsUtility). Used by FwCoreDlgs utility menu and FixFwData command-line tool.

## Architecture
Library (~1065 lines, 7 C# files) integrating SIL.LCModel.FixData repair engine with FieldWorks UI infrastructure. Three-layer design:
1. **Plugin layer**: ErrorFixer implements IUtility for UtilityDlg framework
2. **UI layer**: FixErrorsDlg (WinForms dialog) for project selection
3. **Utility layer**: FwData, WriteAllObjectsUtility (legacy helpers)

Integration flow: UtilityDlg → ErrorFixer.Process() → FixErrorsDlg (select project) → FwDataFixer (repair) → Results logged to UtilityDlg's RichText control with HTML formatting.

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
