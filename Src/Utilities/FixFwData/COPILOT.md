---
last-reviewed: 2025-11-01
last-verified-commit: HEAD
status: production
---

# FixFwData

## Purpose
Command-line utility (WinExe) for repairing FieldWorks project data files. Takes a single file path argument, invokes FwDataFixer from SIL.LCModel.FixData, logs errors to console, and returns exit code 0 (success) or 1 (errors occurred). Provides standalone data repair capability outside the main FieldWorks application for troubleshooting and data recovery.

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

## Dependencies
- **SIL.LCModel.FixData**: FwDataFixer class (core repair logic)
- **SIL.Reporting**: ErrorReport
- **SIL.LCModel.Utils**: IProgress
- **SIL.Windows.Forms**: HotSpotProvider, WinFormsExceptionHandler
- **Consumer**: Administrators, support staff for data repair

## Build Information
- **Project**: FixFwData.csproj
- **Type**: WinExe (.NET Framework 4.6.2)
- **Output**: FixFwData.exe
- **Platform**: AnyCPU
- **Source files**: Program.cs, Properties/AssemblyInfo.cs (2 files)

## Test Index
No test project found.

## Related Folders
- **Utilities/FixFwDataDll/**: Core data repair library (would contain FwDataFixer if not in LCModel)
- **SIL.LCModel.FixData**: External library with FwDataFixer
- **MigrateSqlDbs/**: Legacy FW6→FW7 migration (related data repair scenario)

## References
- **SIL.LCModel.FixData.FwDataFixer**: Main repair engine
- **SIL.LCModel.Utils.IProgress**: Progress reporting interface
- **SIL.Windows.Forms.Reporting.WinFormsExceptionHandler**: Error handling
