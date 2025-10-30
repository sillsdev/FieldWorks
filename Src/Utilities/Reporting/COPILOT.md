---
last-reviewed: 2025-10-30
last-verified-commit: 9611cf70e
status: draft
---

# Reporting

## Purpose
Error reporting and diagnostic information collection infrastructure.
Implements functionality for gathering error details, capturing system state, displaying error
reports to users, and optionally submitting crash reports. Helps improve software quality by
collecting diagnostic information when issues occur.

## Key Components
### Key Classes
- **UsageEmailDialog**
- **ErrorReporter**

## Technology Stack
- C# .NET WinForms
- Error collection and reporting
- Exception handling infrastructure

## Dependencies
- Depends on: Common utilities, System libraries
- Used by: All FieldWorks applications for error handling

## Build Information
- C# class library project
- Build via: `dotnet build Reporting.csproj`
- Error reporting infrastructure

## Entry Points
- ErrorReport class for exception handling
- Error report dialog display
- Error submission functionality

## Related Folders
- **Common/Framework/** - Application framework with error handling
- **DebugProcs/** - Debug diagnostics (related functionality)
- Used throughout FieldWorks for exception handling

## Code Evidence
*Analysis based on scanning 3 source files*

- **Classes found**: 2 public classes
- **Namespaces**: SIL.Utils

## Interfaces and Data Models

- **ErrorReporter** (class)
  - Path: `ErrorReport.cs`
  - Public class implementation

- **UsageEmailDialog** (class)
  - Path: `UsageEmailDialog.cs`
  - Public class implementation

## References

- **Project files**: Reporting.csproj
- **Target frameworks**: net462
- **Key C# files**: AssemblyInfo.cs, ErrorReport.cs, ReportingStrings.Designer.cs, UsageEmailDialog.cs
- **Source file count**: 4 files
- **Data file count**: 4 files

## Architecture
C# library with 4 source files.

## Interop & Contracts
Uses P/Invoke for cross-boundary calls.

## Threading & Performance
Threading model: explicit threading.

## Config & Feature Flags
Config files: App.config.

## Test Index
No tests found in this folder. Tests may be in a separate Test folder or solution.

## Usage Hints
Library component. Reference in consuming projects. See Dependencies section for integration points.
