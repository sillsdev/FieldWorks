---
last-reviewed: 2025-10-31
last-reviewed-tree: 19c464d2f9bdf9361a01fce5ca6e4b9de824edaf8021eb9aa0571131da250f2d
status: draft
---
anchors:
  - change-log-auto
  - purpose
  - architecture
  - key-components
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

<!-- copilot:auto-change-log start -->
## Change Log (auto)

This section is populated by running:
1. `python .github/plan_copilot_updates.py --folders <Folder>`
2. `python .github/copilot_apply_updates.py --folders <Folder>`

Do not edit this block manually; rerun the scripts above after code or doc updates.
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

## Dependencies
- Upstream: Core libraries
- Downstream: Applications

## Interop & Contracts
- Input CSV: installerTestMetadata.csv

## Threading & Performance
- Single-threaded: Sequential file processing

## Config & Feature Flags
No configuration. Behavior controlled by input CSV.

## Build Information
- Project file: InstallValidator.csproj (net48, OutputType=Exe)

## Interfaces and Data Models
See Key Components section above.

## Entry Points
- InstallValidator.exe: Console executable

## Test Index
- Test project: InstallValidatorTests/

## Usage Hints
- Generate metadata: Installer creates installerTestMetadata.csv with expected file list, MD5s, versions, dates

## Related Folders
- FLExInstaller/: Creates installerTestMetadata.csv during install

## References
See `.cache/copilot/diff-plan.json` for file details.
