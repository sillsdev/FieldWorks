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
- **UI thread**: All operations on main UI thread (WinForms single-threaded model)
- **Synchronous repair**: FwDataFixer.FixErrorsAndSave() runs synchronously on UI thread
  - Can cause UI freeze during repair (minutes for large projects)
  - Progress logged incrementally to RichText control
- **Performance characteristics**:
  - Project scan: Fast (<1 second, enumerates .fwdata files)
  - Repair operation: Depends on project size and error count (seconds to minutes)
  - HTML logging: Minimal overhead (RichText append operations)
- **File I/O**: Synchronous reads (project file loading, lock file checks)
- **No background threading**: All work on UI thread (potential for "Not Responding" during long repairs)
- **Memory**: Loads project into memory during repair (can be GBs for large projects)

## Config & Feature Flags
- **Projects directory**: FwDirectoryFinder.ProjectsDirectory (typically %LOCALAPPDATA%\SIL\FieldWorks\Projects\)
- **Lock file filtering**: Excludes projects with .fwdata.lock files (in use)
- **UtilityDlg descriptions** (from Strings.resx):
  - WhenDescription: "Anytime you suspect there is a problem with the data"
  - WhatDescription: "Checks for and fixes various kinds of data corruption"
  - RedoDescription: "Run again when you suspect more problems"
- **HTML logging format**:
  - Errors: `<span style="color:red">{description}</span>`
  - Fixes: `<span style="color:green">Fixed: {description}</span>`
- **No configuration file**: All behavior hardcoded or from resources
- **Plugin registration**: IUtility implementation discovered by UtilityDlg via reflection

## Build Information
- **Project**: FixFwDataDll.csproj
- **Type**: Library (.NET Framework 4.8.x)
- **Output**: FixFwDataDll.dll
- **Namespace**: SIL.FieldWorks.FixData
- **Source files**: ErrorFixer.cs, FixErrorsDlg.cs, FwData.cs, WriteAllObjectsUtility.cs (7 files total including Designer/Strings, ~1065 lines)

## Interfaces and Data Models

### Interfaces
- **IUtility** (from SIL.FieldWorks.FwCoreDlgs)
  - Purpose: Plugin interface for UtilityDlg framework
  - Implementation: ErrorFixer class
  - Methods:
    - Process(IUtilityDlg dlg): Execute repair operation
    - OnSelection(IUtilityDlg dlg): Update dialog descriptions
  - Properties: Label (string) - Display name in utility menu

### Classes
- **ErrorFixer** (path: Src/Utilities/FixFwDataDll/ErrorFixer.cs)
  - Purpose: IUtility plugin for data repair
  - Methods: Process(), OnSelection()
  - Dependencies: FwDataFixer from SIL.LCModel.FixData
  - Notes: Logs to dlg.LogRichText with HTML formatting

- **FixErrorsDlg** (path: Src/Utilities/FixFwDataDll/FixErrorsDlg.cs)
  - Purpose: WinForms project selection dialog
  - Controls: CheckedListBox (m_lvProjects), OK/Cancel buttons
  - Properties: SelectedProject (string) - Returns checked project name
  - Methods: m_btnFixLinks_Click (OK handler)
  - Notes: Filters out locked projects

- **FwData** (path: Src/Utilities/FixFwDataDll/FwData.cs)
  - Purpose: Legacy data wrapper/utility (~14K lines, 358 lines)
  - Notes: Historical code, purpose unclear without deeper analysis

- **WriteAllObjectsUtility** (path: Src/Utilities/FixFwDataDll/WriteAllObjectsUtility.cs)
  - Purpose: Export utility for all objects
  - Notes: Minimal file (~30 lines)

## Entry Points
- **UtilityDlg menu**: Tools→Utilities→Find and Fix Errors
  - UtilityDlg discovers ErrorFixer via IUtility interface
  - User selects utility from list → calls ErrorFixer.Process()
- **ErrorFixer.Process()** workflow:
  1. Show FixErrorsDlg for project selection
  2. User checks project, clicks OK
  3. Create FwDataFixer instance
  4. Call FixErrorsAndSave() with logger callback
  5. Log errors/fixes to dlg.LogRichText with HTML
  6. Return to UtilityDlg (results displayed)
- **Programmatic access** (from FixFwData command-line tool):
  - Not directly used (FixFwData uses SIL.LCModel.FixData directly)
  - FixFwDataDll provides GUI integration layer
- **Typical user workflow**:
  1. User suspects data corruption
  2. Tools→Utilities→Find and Fix Errors
  3. Select project from list
  4. Review errors/fixes in log
  5. Close utility dialog

## Test Index
No test project found.

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
