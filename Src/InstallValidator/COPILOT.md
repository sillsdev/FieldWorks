---
last-reviewed: 2025-10-31
last-reviewed-tree: 9bb37d4844749c3d47b182f3d986f342994d9876216b65e0d3e2791f9ec96ae5
status: draft
---

<!-- copilot:auto-change-log start -->
## Change Log (auto)

- Snapshot: HEAD~1
- Risk: none
- Files: 0 (code=0, tests=0, resources=0)

### Prompt seeds
- Update COPILOT.md for Src/InstallValidator. Prioritize Purpose/Architecture sections using planner data.
- Highlight API or UI updates, then confirm Usage/Test sections reflect 0 files changed (code=0, tests=0, resources=0); risk=none.
- Finish with verification notes and TODOs for manual testing.
<!-- copilot:auto-change-log end -->


# InstallValidator COPILOT summary

## Purpose
Installation prerequisite validation tool verifying FieldWorks installation correctness. Reads installerTestMetadata.csv containing expected file list with MD5 checksums, versions, and dates. Compares expected metadata against actual installed files, generating FlexInstallationReport CSV with results (correct, missing, or incorrect). Identifies installation problems: missing files, wrong versions, corrupted files. Helps verify successful installation and diagnose installation issues. Command-line tool (InstallValidator.exe) with drag-and-drop support (drop CSV on EXE).

## Architecture
C# console application (.NET Framework 4.8.x) with single source file (InstallValidator.cs, 120 lines). Main() entry point processes CSV input, computes MD5 checksums for comparison, generates report CSV. Test project InstallValidatorTests validates functionality. Minimal dependencies for reliability.

## Key Components
- **InstallValidator** class (InstallValidator.cs, 120 lines): Installation verification
  - Main() entry point: Processes installerTestMetadata.csv
  - Input CSV format: FilePath, MD5, Version (optional), Date (optional)
  - Output CSV format: File, Result, Expected Version, Actual Version, Expected Date, Actual Date Modified (UTC)
  - ComputeMd5Sum(): Calculate MD5 checksum of file
  - GenerateOutputNameFromInput(): Create output filename from app version
  - SafeGetAt(): Safely access array elements
  - Results: "was installed correctly", "is missing", "incorrect file is present"
  - Drag-and-drop support: Opens report automatically when invoked via drop

## Technology Stack
- C# .NET Framework 4.8.x (net8)
- OutputType: Exe (console application)
- System.Security.Cryptography for MD5 hashing
- System.Diagnostics for file version info
- CSV file I/O

## Dependencies

### Upstream (consumes)
- .NET Framework 4.8.x (System.*, minimal dependencies)
- System.Security.Cryptography: MD5 hashing
- System.Diagnostics: FileVersionInfo

### Downstream (consumed by)
- **Installer validation**: Verify FieldWorks installation
- **QA/Testing**: Installation verification in test scenarios
- **Users**: Diagnose installation problems

## Interop & Contracts
- **Input CSV**: installerTestMetadata.csv
  - Format: FilePath, MD5, Version (optional), Date (optional)
  - First line: App version info (e.g., "FieldWorks 9.0.4")
- **Output CSV**: FlexInstallationReport.{version}.csv
  - Format: File, Result, Expected Version, Actual Version, Expected Date, Actual Date Modified (UTC)
- **Command-line**: InstallValidator.exe installerTestMetadata.csv [report_path]
- **Drag-and-drop**: Drop CSV on EXE to run and open report

## Threading & Performance
- **Single-threaded**: Sequential file processing
- **I/O bound**: File reading and MD5 computation
- **Performance**: Fast for typical installation (hundreds of files)

## Config & Feature Flags
No configuration. Behavior controlled by input CSV.

## Build Information
- **Project file**: InstallValidator.csproj (net48, OutputType=Exe)
- **Test project**: InstallValidatorTests/
- **Output**: InstallValidator.exe
- **Build**: Via top-level FieldWorks.sln or: `msbuild InstallValidator.csproj`
- **Run tests**: `dotnet test InstallValidatorTests/`

## Interfaces and Data Models

- **Main()** (InstallValidator.cs)
  - Purpose: Entry point for installation validation
  - Inputs: args[0] = installerTestMetadata.csv path, args[1] = optional report output path
  - Outputs: FlexInstallationReport CSV, exit code 0 (always succeeds; errors in report)
  - Notes: Drag-and-drop supported (opens report after generation)

- **ComputeMd5Sum()** (InstallValidator.cs)
  - Purpose: Calculate MD5 checksum of file
  - Inputs: string filename (full path)
  - Outputs: string (MD5 checksum as hex string)
  - Notes: Uses static MD5 Hasher for performance

- **Input CSV format** (installerTestMetadata.csv):
  - Line 1: App version info (e.g., "FieldWorks 9.0.4")
  - Subsequent lines: FilePath, MD5, Version (optional), Date (optional)
  - Example: "FieldWorks.exe, a1b2c3d4..., 9.0.4.0, 2023-01-15"

- **Output CSV format** (FlexInstallationReport):
  - Line 1: "Installation report for: {app version}"
  - Line 2: Headers (File, Result, Expected Version, Actual Version, Expected Date, Actual Date Modified)
  - Data lines: File validation results
  - Results: "was installed correctly", "is missing", "incorrect file is present"

## Entry Points
- **InstallValidator.exe**: Console executable
- **Drag-and-drop**: Drop installerTestMetadata.csv on exe
- **Command-line**: `InstallValidator.exe installerTestMetadata.csv [report_path]`

## Test Index
- **Test project**: InstallValidatorTests/
- **Run tests**: `dotnet test InstallValidatorTests/`
- **Coverage**: CSV processing, MD5 computation, report generation

## Usage Hints
- **Generate metadata**: Installer creates installerTestMetadata.csv with expected file list, MD5s, versions, dates
- **Validate installation**: Run InstallValidator.exe after install or drag CSV onto exe
- **Review report**: FlexInstallationReport CSV shows which files are missing, incorrect, or correct
- **QA workflow**: Include in automated installation testing
- **User troubleshooting**: Users can run to diagnose installation issues
- **Drag-and-drop**: Easiest for non-technical users (report opens automatically)
- **Unit tests**: Use optional second argument to specify report location

## Related Folders
- **FLExInstaller/**: Creates installerTestMetadata.csv during install
- Installation infrastructure

## References
- **Project files**: InstallValidator.csproj (net48, OutputType=Exe), InstallValidatorTests/
- **Target frameworks**: .NET Framework 4.8.x
- **Key C# files**: InstallValidator.cs (120 lines)
- **Total lines of code**: 120
- **Output**: InstallValidator.exe (Output/Debug or Output/Release)
- **Namespace**: SIL.InstallValidator
