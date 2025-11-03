---
last-reviewed: 2025-11-01
last-reviewed-tree: 6cf055af735fcf5f893126f0d5bf31ba037b8c3ff5eef360f62a7319ca5d5f0e
status: production
---

# FixFwData

## Purpose
Command-line utility (WinExe) for repairing FieldWorks project data files. Takes a single file path argument, invokes FwDataFixer from SIL.LCModel.FixData, logs errors to console, and returns exit code 0 (success) or 1 (errors occurred). Provides standalone data repair capability outside the main FieldWorks application for troubleshooting and data recovery.

## Architecture
TBD - populate from code. See auto-generated hints below.

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
TBD - populate from code. See auto-generated hints below.

## Dependencies
- **SIL.LCModel.FixData**: FwDataFixer class (core repair logic)
- **SIL.Reporting**: ErrorReport
- **SIL.LCModel.Utils**: IProgress
- **SIL.Windows.Forms**: HotSpotProvider, WinFormsExceptionHandler
- **Consumer**: Administrators, support staff for data repair

## Interop & Contracts
TBD - populate from code. See auto-generated hints below.

## Threading & Performance
TBD - populate from code. See auto-generated hints below.

## Config & Feature Flags
TBD - populate from code. See auto-generated hints below.

## Build Information
- **Project**: FixFwData.csproj
- **Type**: WinExe (.NET Framework 4.6.2)
- **Output**: FixFwData.exe
- **Platform**: AnyCPU
- **Source files**: Program.cs, Properties/AssemblyInfo.cs (2 files)

## Interfaces and Data Models
TBD - populate from code. See auto-generated hints below.

## Entry Points
TBD - populate from code. See auto-generated hints below.

## Test Index
No test project found.

## Usage Hints
TBD - populate from code. See auto-generated hints below.

## Related Folders
- **Utilities/FixFwDataDll/**: Core data repair library (would contain FwDataFixer if not in LCModel)
- **SIL.LCModel.FixData**: External library with FwDataFixer
- **MigrateSqlDbs/**: Legacy FW6→FW7 migration (related data repair scenario)

## References
- **SIL.LCModel.FixData.FwDataFixer**: Main repair engine
- **SIL.LCModel.Utils.IProgress**: Progress reporting interface
- **SIL.Windows.Forms.Reporting.WinFormsExceptionHandler**: Error handling

## References (auto-generated hints)
- Project files:
  - Utilities/FixFwData/FixFwData.csproj
- Key C# files:
  - Utilities/FixFwData/Program.cs
  - Utilities/FixFwData/Properties/AssemblyInfo.cs
