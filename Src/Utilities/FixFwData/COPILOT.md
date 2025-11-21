---
last-reviewed: 2025-11-21
last-reviewed-tree: 9bd081c8b99fcd3e3e2e644cac0f5e8222648391667a18c1ee1095769390928b
status: draft
---

<!-- copilot:auto-change-log start -->
## Change Log (auto)

This section is populated by running:
1. `python .github/plan_copilot_updates.py --folders <Folder>`
2. `python .github/copilot_apply_updates.py --folders <Folder>`

Do not edit this block manually; rerun the scripts above after code or doc updates.
<!-- copilot:auto-change-log end -->

# FixFwData

## Purpose
Command-line utility (WinExe) for repairing FieldWorks project data files. Takes a single file path argument, invokes FwDataFixer from SIL.LCModel.FixData, logs errors to console, and returns exit code 0 (success) or 1 (errors occurred). Provides standalone data repair capability outside the main FieldWorks application for troubleshooting and data recovery.

## Architecture
Simple command-line WinExe wrapper (~120 lines in Program.cs) around SIL.LCModel.FixData.FwDataFixer. Single-file architecture: Main() parses command-line argument (file path), instantiates FwDataFixer, calls FixErrorsAndSave() with console logger callbacks, returns exit code. NullProgress nested class provides IProgress implementation writing to Console.Out. No UI dialogs - pure console output with WinForms exception handling for stability.

## Key Components

### Program.cs (~120 lines)
- **Main(string[] args)**: Entry point. Takes file path argument, creates FwDataFixer, calls FixErrorsAndSave(), returns exit code
  - Input: args[0] = pathname to FW project file
  - Output: Exit code 0 (no errors) or 1 (errors occurred)
  - Uses NullProgress (console-based IProgress)
  - Calls SetUpErrorHandling() for WinForms exception handling
- **logger(string description, bool errorFixed)**: Callback for FwDataFixer. Prints errors to console, sets errorsOccurred flag, counts fixes
- **counter()**: Callback returning total error count
- **SetUpErrorHandling()**: Configures ErrorReport email (flex_errors@sil.org), WinFormsExceptionHandler
- **NullProgress**: IProgress implementation that writes messages to Console.Out, doesn't support cancellation

## Technology Stack
- **Language**: C#
- **Target framework**: .NET Framework 4.8.x (net48)
- **Application type**: WinExe (console application with WinForms error handling)
- **Key libraries**:
  - SIL.LCModel.FixData (FwDataFixer - core repair engine)
  - SIL.LCModel.Utils (IProgress interface)
  - SIL.Reporting (ErrorReport for crash reporting)
  - SIL.Windows.Forms (WinFormsExceptionHandler, HotSpotProvider)
- **Platform**: Windows-only (WinForms dependencies)

## Dependencies
- **SIL.LCModel.FixData**: FwDataFixer class (core repair logic)
- **SIL.Reporting**: ErrorReport
- **SIL.LCModel.Utils**: IProgress
- **SIL.Windows.Forms**: HotSpotProvider, WinFormsExceptionHandler
- **Consumer**: Administrators, support staff for data repair

## Interop & Contracts
- **Command-line interface**: `FixFwData.exe <filepath>`
  - Input: Single argument - path to FW project file (.fwdata or .fwdb)
  - Output: Console messages (errors found/fixed), exit code
  - Exit codes: 0 = success (no errors or all fixed), 1 = errors occurred
- **FwDataFixer contract**: Calls FixErrorsAndSave(logger, counter)
  - logger callback: `void(string description, bool errorFixed)` - Reports each error
  - counter callback: `int()` - Returns total error count
- **Console output**: All error messages and progress written to stdout
- **Error reporting**: flex_errors@sil.org configured for crash reports
- **No file format dependencies**: FwDataFixer handles all project file formats

## Threading & Performance
- **Single-threaded**: All operations on main thread
- **Synchronous**: FwDataFixer.FixErrorsAndSave() runs synchronously
- **Performance characteristics**:
  - File loading: Depends on file size (seconds to minutes for large projects)
  - Error scanning: O(n) where n = number of objects in project
  - Error fixing: Depends on error count and complexity
  - Typical runtime: 1-5 minutes for small projects, 10-30 minutes for large
- **Console output**: Incremental (errors logged as found)
- **No progress UI**: NullProgress writes to console but provides no visual progress
- **Memory**: Loads entire project into memory (can be GBs for large projects)

## Config & Feature Flags
- **Command-line argument**: File path (required, no flags/options)
- **Error email**: Hardcoded to flex_errors@sil.org (for crash reports)
- **No configuration file**: All behavior hardcoded
- **WinForms exception handling**: SetUpErrorHandling() configures UnhandledException handlers
- **NullProgress settings**: No cancellation support (IsCanceling always returns false)
- **Exit code behavior**: 0 = success, 1 = errors (standard convention)

## Build Information
- **Project**: FixFwData.csproj
- **Type**: WinExe (.NET Framework 4.8.x)
- **Output**: FixFwData.exe
- **Platform**: AnyCPU
- **Source files**: Program.cs, Properties/AssemblyInfo.cs (2 files)

## Interfaces and Data Models

### Interfaces
- **IProgress** (from SIL.LCModel.Utils)
  - Purpose: Progress reporting during operations
  - Implementation: NullProgress (writes to Console.Out)
  - Methods: Step(int amount), Message(string msg), IsCanceling property
  - Notes: No visual progress, no cancellation support

### Classes
- **NullProgress** (nested in Program)
  - Purpose: Console-based IProgress implementation
  - Methods:
    - Step(int amount): No-op (no visual progress)
    - Message(string msg): Writes to Console.Out
    - IsCanceling: Always returns false
  - Usage: Passed to FwDataFixer for progress callbacks

### Callbacks
- **logger**: `Action<string description, bool errorFixed>`
  - Purpose: Log each error found/fixed
  - Implementation: Prints to console, sets errorsOccurred flag
- **counter**: `Func<int>`
  - Purpose: Return total error count
  - Implementation: Returns count of logged errors

## Entry Points
- **Command-line execution**: `FixFwData.exe "C:\path\to\project.fwdata"`
  - Typical use: Data recovery when FLEx won't open a project
  - Returns exit code for scripting/automation
- **Main(string[] args)**: Program entry point
  - Validates argument count (exactly 1 required)
  - Creates FwDataFixer instance
  - Calls FixErrorsAndSave() with logger/counter callbacks
  - Returns exit code based on errorsOccurred flag
- **Common scenarios**:
  - User reports "FLEx won't open my project"
  - Support staff: Run FixFwData.exe to diagnose/repair
  - Automated recovery scripts
  - Pre-migration data cleanup
- **Output redirection**: Can redirect console output to log file
  - Example: `FixFwData.exe project.fwdata > repair.log 2>&1`

## Test Index
No test project found.

## Usage Hints
- **Basic usage**: `FixFwData.exe "C:\Users\...\MyProject.fwdata"`
  - Must provide full path to project file
  - Enclose path in quotes if it contains spaces
  - Check exit code: 0 = success, 1 = errors
- **Typical workflow**:
  1. User reports corrupt project (FLEx won't open)
  2. Support staff asks for project file
  3. Run FixFwData.exe on project file
  4. Review console output for errors found/fixed
  5. If exit code 0, project should be openable
  6. If exit code 1, review error messages, may need manual intervention
- **Console output interpretation**:
  - "Error fixed" = FwDataFixer repaired the issue
  - Error count at end = total issues found
  - No errors = "No errors found" message
- **Common pitfalls**:
  - Forgetting to provide file path argument (error message)
  - Running on wrong file type (.fwbackup instead of .fwdata)
  - Insufficient disk space for repair operations
  - File locked by another process (FLEx must be closed)
- **Troubleshooting**:
  - "File not found": Check path, quotes
  - "Access denied": Check file permissions
  - Long runtime: Normal for large projects (patience required)
  - No output: Check if process hung (task manager)
- **Scripting example**:
  ```batch
  FixFwData.exe "project.fwdata" > repair.log
  if %ERRORLEVEL% EQU 0 (
      echo Repair successful
  ) else (
      echo Errors occurred, see repair.log
  )
  ```

## Related Folders
- **Utilities/FixFwDataDll/**: Core data repair library (would contain FwDataFixer if not in LCModel)
- **SIL.LCModel.FixData**: External library with FwDataFixer
- **MigrateSqlDbs/**: Legacy FW6→FW7 migration (related data repair scenario)

## References
- **SIL.LCModel.FixData.FwDataFixer**: Main repair engine
- **SIL.LCModel.Utils.IProgress**: Progress reporting interface
- **SIL.Windows.Forms.Reporting.WinFormsExceptionHandler**: Error handling

## Auto-Generated Project and File References
- Project files:
  - Utilities/FixFwData/FixFwData.csproj
- Key C# files:
  - Utilities/FixFwData/Program.cs
  - Utilities/FixFwData/Properties/AssemblyInfo.cs
