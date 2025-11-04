---
last-reviewed: 2025-11-01
last-reviewed-tree: 4b3215ece83f3cc04a275800cd77b630c2b5418bb20632848b9ce46df61d2e90
status: production
---

# Reporting

## Purpose
Error reporting and diagnostic information collection infrastructure from SIL.Core. Wraps SIL.Reporting.ErrorReport functionality for FieldWorks integration. Provides ErrorReport class for gathering error details, ReportingStrings localized resources, and UsageEmailDialog for user feedback. Used throughout FieldWorks for exception handling and crash reporting.

## Architecture
Thin wrapper library (~1554 lines, 4 C# files) around SIL.Reporting NuGet package. Provides FieldWorks-specific error reporting with ErrorReport static API, UsageEmailDialog for user feedback, and ReportingStrings localized resources. Integrates SIL.Core error reporting infrastructure into FieldWorks exception handling pipeline.

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
- **Language**: C#
- **Target framework**: .NET Framework 4.6.2 (net462)
- **Library type**: Class library wrapper around SIL.Reporting
- **UI framework**: System.Windows.Forms (UsageEmailDialog)
- **Key libraries**: SIL.Reporting (NuGet), SIL.Core
- **Resources**: ErrorReport.resx, ReportingStrings.resx, UsageEmailDialog.resx, App.config

## Dependencies
- **SIL.Reporting**: ErrorReport implementation (NuGet package)
- **SIL.Core**: Base utilities
- **System.Windows.Forms**: Dialog infrastructure
- **Consumer**: All FieldWorks applications (Common/Framework, DebugProcs) for exception handling

## Interop & Contracts
- **ErrorReport static API**: ReportNonFatalException(), ReportFatalException(), NotifyUserOfProblem()
- **UsageEmailDialog**: Modal WinForms dialog for user feedback collection
- **Email submission**: Configurable EmailAddress, EmailSubject properties
- **System info**: AddStandardProperties() adds OS, RAM, .NET version to reports
- **Privacy**: "Don't show again" checkbox for user control

## Threading & Performance
- **UI thread**: Error dialogs must show on UI thread
- **Synchronous**: ReportFatalException terminates app (blocks)
- **Asynchronous email**: UsageEmailDialog may submit email async
- **Lightweight**: Minimal overhead for non-fatal exceptions (logging only)

## Config & Feature Flags
- **Email configuration**: ErrorReport.EmailAddress, ErrorReport.EmailSubject
- **Report detail level**: Configurable via SIL.Reporting settings
- **User privacy**: UsageEmailDialog "don't show again" persisted
- **Localization**: ReportingStrings.resx for multi-language support

## Build Information
- **Project**: Reporting.csproj
- **Type**: Library (.NET Framework 4.6.2)
- **Output**: Reporting.dll (FieldWorks wrapper)
- **Namespace**: SIL.Utils (wrapper), SIL.Reporting (core)
- **Source files**: 4 files (~1554 lines)
- **Resources**: ErrorReport.resx, ReportingStrings.resx, UsageEmailDialog.resx, App.config

## Interfaces and Data Models
- **ErrorReport**: Static error reporting API from SIL.Reporting
- **UsageEmailDialog**: WinForms dialog for user feedback (email, comments)
- **ReportingStrings**: Designer-generated localized resources

## Entry Points
- **ErrorReport.ReportNonFatalException(exception)**: Log non-fatal errors
- **ErrorReport.ReportFatalException(exception)**: Show error dialog, terminate
- **ErrorReport.NotifyUserOfProblem(message)**: User-facing error notification
- **UsageEmailDialog.ShowDialog()**: Collect user feedback

## Test Index
No test project found.

## Usage Hints
- **Fatal errors**: `ErrorReport.ReportFatalException(ex);` shows dialog and exits
- **Non-fatal**: `ErrorReport.ReportNonFatalException(ex);` logs only
- **User notification**: `ErrorReport.NotifyUserOfProblem("Message");` shows message
- **Configuration**: Set ErrorReport.EmailAddress before first use
- **Best practice**: Wrap top-level exception handlers with ReportFatalException

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
