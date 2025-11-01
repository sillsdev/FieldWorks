---
last-reviewed: 2025-11-01
last-verified-commit: HEAD
status: production
---

# Reporting

## Purpose
Error reporting and diagnostic information collection infrastructure from SIL.Core. Wraps SIL.Reporting.ErrorReport functionality for FieldWorks integration. Provides ErrorReport class for gathering error details, ReportingStrings localized resources, and UsageEmailDialog for user feedback. Used throughout FieldWorks for exception handling and crash reporting.

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

## Dependencies
- **SIL.Reporting**: ErrorReport implementation (NuGet package)
- **SIL.Core**: Base utilities
- **System.Windows.Forms**: Dialog infrastructure
- **Consumer**: All FieldWorks applications (Common/Framework, DebugProcs) for exception handling

## Build Information
- **Project**: Reporting.csproj
- **Type**: Library (.NET Framework 4.6.2)
- **Output**: Reporting.dll (FieldWorks wrapper) 
- **Namespace**: SIL.Utils (wrapper), SIL.Reporting (core)
- **Source files**: 4 files (~1554 lines)
- **Resources**: ErrorReport.resx, ReportingStrings.resx, UsageEmailDialog.resx, App.config

## Test Index
No test project found.

## Related Folders
- **Common/Framework/**: Application framework with error handling hooks
- **DebugProcs/**: Debug diagnostics and assertion handlers
- **SIL.Core/SIL.Reporting**: External NuGet package with ErrorReport implementation

## References
- **SIL.Reporting.ErrorReport**: Main exception reporting class
- **System.Windows.Forms.Form**: UsageEmailDialog base class
