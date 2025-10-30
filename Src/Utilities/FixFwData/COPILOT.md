---
last-reviewed: 2025-10-30
last-verified-commit: 9611cf70e
status: draft
---

# FixFwData

## Purpose
Command-line tool for repairing FieldWorks project data files.
Provides automated data integrity checking, error detection, and repair functionality that can be
run outside the main FieldWorks application. Useful for data recovery, troubleshooting, and
batch maintenance of project files.

## Key Components
No major public classes identified.

## Technology Stack
- C# .NET console application
- Data validation and repair algorithms

## Dependencies
- Depends on: Utilities/FixFwDataDll (core repair logic), Cellar (data model)
- Used by: Administrators and support staff for data repair

## Build Information
- C# console application
- Build via: `dotnet build FixFwData.csproj`
- Command-line utility

## Entry Points
- Main() method with command-line argument parsing
- Invokes FixFwDataDll for actual repair work

## Related Folders
- **Utilities/FixFwDataDll/** - Core data repair library
- **Cellar/** - Data model being repaired
- **MigrateSqlDbs/** - Database migration (related functionality)

## Code Evidence
*Analysis based on scanning 1 source files*

- **Namespaces**: FixFwData

## References

- **Project files**: FixFwData.csproj
- **Target frameworks**: net462
- **Key C# files**: AssemblyInfo.cs, Program.cs
- **Source file count**: 2 files
- **Data file count**: 0 files

## Interfaces and Data Models
See code analysis sections above for key interfaces and data models. Additional interfaces may be documented in source files.

## Architecture
C# library with 2 source files.

## Interop & Contracts
Uses COM for cross-boundary calls.

## Threading & Performance
Single-threaded or thread-agnostic code. No explicit threading detected.

## Config & Feature Flags
No explicit configuration or feature flags detected.

## Test Index
No tests found in this folder. Tests may be in a separate Test folder or solution.

## Usage Hints
Console application. Build and run via command line or Visual Studio. See Entry Points section.
