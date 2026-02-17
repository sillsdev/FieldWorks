---
last-reviewed: 2025-11-01
last-reviewed-tree: 36e1d90caeb27f6f521886e113479f718faf2009beea23f0dcff066ed6ed3677
status: production
---
anchors:
  - change-log-auto
  - purpose
  - architecture
  - key-components
  - errorfixercs-180-lines
  - fixerrorsdlgcs-100-lines
  - fwdatacs
  - writeallobjectsutilitycs
  - technology-stack
  - dependencies
  - interop--contracts
  - threading--performance
  - config--feature-flags
  - build-information
  - interfaces-and-data-models
  - entry-points
  - test-index
  - usage-hints
  - related-folders
  - references
  - auto-generated-project-and-file-references

<!-- copilot:auto-change-log start -->
## Change Log (auto)

This section is populated by running:
1. `python .github/plan_copilot_updates.py --folders <Folder>`
2. `python .github/copilot_apply_updates.py --folders <Folder>`

Do not edit this block manually; rerun the scripts above after code or doc updates.
<!-- copilot:auto-change-log end -->

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

### WriteAllObjectsUtility.cs
- **WriteAllObjectsUtility**: Export utility for all objects (not analyzed in detail)

## Technology Stack
- **Language**: C#
- **Target framework**: .NET Framework 4.8.x (net48)
- **Library type**: Class library (DLL)
- **UI framework**: System.Windows.Forms (FixErrorsDlg)
- **Key libraries**:
  - SIL.LCModel.FixData (FwDataFixer - core repair engine)
  - SIL.FieldWorks.FwCoreDlgs (UtilityDlg, IUtility plugin interface)
  - SIL.FieldWorks.Common.FwUtils (FwDirectoryFinder, utilities)
  - SIL.LCModel (LcmFileHelper)
  - System.Windows.Forms (WinForms controls)
- **Resource files**: Strings.resx (localized strings), FixErrorsDlg.resx (dialog layout)

## Dependencies
- **SIL.LCModel.FixData**: FwDataFixer (core repair logic)
- **SIL.FieldWorks.FwCoreDlgs**: UtilityDlg, IUtility
- **SIL.FieldWorks.Common.FwUtils**: FwDirectoryFinder
- **SIL.LCModel**: LcmFileHelper
- **Consumer**: FwCoreDlgs utility menu, Utilities/FixFwData command-line tool

## Interop & Contracts
- **IUtility plugin interface** (from FwCoreDlgs):
  - Purpose: Integrate into UtilityDlg menu system
  - Methods:
    - Process(): Execute repair operation (shows FixErrorsDlg, runs FwDataFixer)
    - OnSelection(): Update UtilityDlg descriptions (When/What/Redo text)
  - Properties: Label ("Find and Fix Errors")
- **FixErrorsDlg contract**:
  - Input: Projects directory scan (FwDirectoryFinder.ProjectsDirectory)
  - Output: SelectedProject property (user-selected project name)
  - DialogResult: OK = project selected, Cancel = cancelled
- **FwDataFixer integration**:
  - Callback: logger(string description, bool errorFixed) - Logs to RichText with HTML
  - HTML formatting: `<span style="color:red">` for errors, green for fixes
- **Project file locking**: Filters out locked projects (.fwdata.lock files)
- **Directory scanning**: Enumerates .fwdata files in projects directory

## Threading & Performance
All operations on UI thread. Synchronous repair can take minutes for large projects. Progress logged incrementally to RichText.

## Config & Feature Flags
Projects from FwDirectoryFinder.ProjectsDirectory. Excludes locked projects (.fwdata.lock). HTML logging (red=errors, green=fixes). IUtility plugin.

## Build Information
C# library (net48). Build via `msbuild FixFwDataDll.csproj`. Output: FixFwDataDll.dll.

## Interfaces and Data Models
IUtility implementation (ErrorFixer). FixErrorsDlg for project selection. Wraps FwDataFixer from SIL.LCModel.FixData.

## Entry Points
Tools→Utilities→Find and Fix Errors. UtilityDlg discovers ErrorFixer via IUtility. Process() shows FixErrorsDlg, calls FwDataFixer.FixErrorsAndSave(), logs to RichText.

## Test Index
No dedicated test project. Tested via manual usage in UtilityDlg.

## Usage Hints
- **Access from FLEx**: Tools→Utilities→Find and Fix Errors
  - Launches UtilityDlg with ErrorFixer selected
- **Project selection**:
  - Dialog lists all .fwdata files in projects directory
  - Locked projects (in use) automatically filtered out
  - Check desired project, click OK
- **Repair process**:
  - Synchronous operation (may take minutes)
  - Progress logged to dialog window
  - Red text = errors found, green text = fixes applied
- **Common scenarios**:
  - "FLEx is behaving strangely" → Run error fixer
  - After crash recovery → Check for corruption
  - Before major operations (migration, export)
- **Best practices**:
  - Close project before running (if checking different project)
  - Review error log after completion
  - Re-run if new errors suspected (Redo description)
- **Common pitfalls**:
  - Running on locked project (filtered out automatically)
  - Not waiting for completion (long runtime for large projects)
  - Ignoring error log (should review for serious issues)
- **Troubleshooting**:
  - "No projects found": Check projects directory location
  - "Project locked": Close FLEx, other apps accessing project
  - Long runtime: Normal for large projects (patience required)
- **Comparison with FixFwData.exe**:
  - FixFwDataDll: GUI integration (UtilityDlg menu)
  - FixFwData.exe: Command-line standalone
  - Both use same FwDataFixer engine

## Related Folders
- **Utilities/FixFwData/**: Command-line wrapper for non-interactive repair
- **FwCoreDlgs/**: UtilityDlg framework (IUtility plugin host)
- **SIL.LCModel.FixData**: External library with FwDataFixer

## References
- **SIL.FieldWorks.FwCoreDlgs.IUtility**: Plugin interface
- **SIL.LCModel.FixData.FwDataFixer**: Core repair engine
- **SIL.FieldWorks.Common.FwUtils.FwDirectoryFinder**: Projects directory location

## Auto-Generated Project and File References
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
