---
last-reviewed: 2025-11-01
last-reviewed-tree: 4b3215ece83f3cc04a275800cd77b630c2b5418bb20632848b9ce46df61d2e90
status: production
---

# Reporting

## Purpose
Error reporting and diagnostic information collection infrastructure from SIL.Core. Wraps SIL.Reporting.ErrorReport functionality for FieldWorks integration. Provides ErrorReport class for gathering error details, ReportingStrings localized resources, and UsageEmailDialog for user feedback. Used throughout FieldWorks for exception handling and crash reporting.

## Architecture
TBD - populate from code. See auto-generated hints below.

## Key Components

### ErrorReport.cs (~900 lines)
- **ErrorReport**: Static exception reporting API (from SIL.Core/SIL.Reporting)
  - **ReportNonFatalException(Exception)**: Logs non-fatal errors
  - **ReportFatalException(Exception)**: Shows fatal error dialog, terminates app
  - **AddStandardProperties()**: Adds system info (OS, RAM, etc.)
  - EmailAddress, EmailSubject properties for crash report submission
  - NotifyUserOfProblem() for user-facing errors
- Note: Implementation likely in SIL.Reporting NuGet package (not in this folder)

### UsageEmailDialog.cs (~350 lines)
- **UsageEmailDialog**: WinForms dialog for optional user feedback
  - Collects email address, allows user comments on error
  - Privacy-conscious design ("don't show again" checkbox)
  - Integrates with ErrorReport submission workflow

### ReportingStrings.Designer.cs (~400 lines)
- **ReportingStrings**: Localized string resources (Designer-generated)
  - Error messages, dialog text, report templates
  - Culture-specific formatting

## Technology Stack
TBD - populate from code. See auto-generated hints below.

## Dependencies
- **SIL.Reporting**: ErrorReport implementation (NuGet package)
- **SIL.Core**: Base utilities
- **System.Windows.Forms**: Dialog infrastructure
- **Consumer**: All FieldWorks applications (Common/Framework, DebugProcs) for exception handling

## Interop & Contracts
TBD - populate from code. See auto-generated hints below.

## Threading & Performance
TBD - populate from code. See auto-generated hints below.

## Config & Feature Flags
TBD - populate from code. See auto-generated hints below.

## Build Information
- **Project**: Reporting.csproj
- **Type**: Library (.NET Framework 4.6.2)
- **Output**: Reporting.dll (FieldWorks wrapper)
- **Namespace**: SIL.Utils (wrapper), SIL.Reporting (core)
- **Source files**: 4 files (~1554 lines)
- **Resources**: ErrorReport.resx, ReportingStrings.resx, UsageEmailDialog.resx, App.config

## Interfaces and Data Models
TBD - populate from code. See auto-generated hints below.

## Entry Points
TBD - populate from code. See auto-generated hints below.

## Test Index
No test project found.

## Usage Hints
TBD - populate from code. See auto-generated hints below.

## Related Folders
- **Common/Framework/**: Application framework with error handling hooks
- **DebugProcs/**: Debug diagnostics and assertion handlers
- **SIL.Core/SIL.Reporting**: External NuGet package with ErrorReport implementation

## References
- **SIL.Reporting.ErrorReport**: Main exception reporting class
- **System.Windows.Forms.Form**: UsageEmailDialog base class

## References (auto-generated hints)
- Project files:
  - Utilities/Reporting/Reporting.csproj
- Key C# files:
  - Utilities/Reporting/AssemblyInfo.cs
  - Utilities/Reporting/ErrorReport.cs
  - Utilities/Reporting/ReportingStrings.Designer.cs
  - Utilities/Reporting/UsageEmailDialog.cs
- Data contracts/transforms:
  - Utilities/Reporting/App.config
  - Utilities/Reporting/ErrorReport.resx
  - Utilities/Reporting/ReportingStrings.resx
  - Utilities/Reporting/UsageEmailDialog.resx
