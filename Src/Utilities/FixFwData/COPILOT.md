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
C# .NET Framework 4.8.x WinExe. Key libraries: SIL.LCModel.FixData (FwDataFixer repair engine), SIL.Reporting (ErrorReport), SIL.Windows.Forms (exception handling). Windows-only.

## Dependencies
Consumes: SIL.LCModel.FixData (FwDataFixer), SIL.Reporting, SIL.LCModel.Utils (IProgress). Used by: Administrators/support staff for data repair.

## Interop & Contracts
Command-line: `FixFwData.exe <filepath>`. Input: FW project file path. Output: Console messages, exit code (0=success, 1=errors). FwDataFixer.FixErrorsAndSave() with logger/counter callbacks. Error reporting to flex_errors@sil.org.

## Threading & Performance
Single-threaded synchronous operation. Typical runtime: 1-5 minutes (small projects), 10-30 minutes (large). Loads entire project into memory (can be GBs).

## Config & Feature Flags
File path argument required (no flags). Error email hardcoded to flex_errors@sil.org. No configuration file. Exit code: 0=success, 1=errors.

## Build Information
FixFwData.csproj (net48, WinExe). Output: FixFwData.exe. Source: Program.cs, Properties/AssemblyInfo.cs (2 files).

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
Command-line: `FixFwData.exe "C:\path\to\project.fwdata"`. Main() validates argument, creates FwDataFixer, calls FixErrorsAndSave() with logger/counter callbacks, returns exit code. Used for data recovery when FLEx won't open projects.

## Test Index
No test project.

## Usage Hints
Usage: `FixFwData.exe "path"` (enclose in quotes if spaces). Exit code: 0=success, 1=errors. Typical workflow: User reports corrupt project → support staff runs tool → review console output → retry opening in FLEx. Console output shows "Error fixed" messages and error count. Output redirectable: `FixFwData.exe project.fwdata > repair.log 2>&1`. Runtime: 1-5 minutes (small), 10-30 minutes (large). Ensure FLEx closed (file lock), sufficient disk space. Scripting: check %ERRORLEVEL%.

## Related Folders
Utilities/FixFwDataDll/ (core repair library), MigrateSqlDbs/ (legacy migration).

## References
FixFwData.csproj (net48, WinExe). Uses SIL.LCModel.FixData.FwDataFixer (repair engine). Program.cs (~120 lines), NullProgress nested class (console IProgress). See `.cache/copilot/diff-plan.json` for complete file listing.
