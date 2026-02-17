---
last-reviewed: 2025-11-01
last-reviewed-tree: 6e77190e724389dc36805e7317baffc3b2b0783186bbb258a5dc6c954632c73d
status: production
---
anchors:
  - change-log-auto
  - purpose
  - architecture
  - key-components
  - errorreportcs-900-lines
  - usageemaildialogcs-350-lines
  - reportingstringsdesignercs-400-lines
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
Language - C#

## Dependencies
- Upstream: Core libraries
- Downstream: Applications

## Interop & Contracts
- ErrorReport static API: ReportNonFatalException(), ReportFatalException(), NotifyUserOfProblem()

## Threading & Performance
- UI thread: Error dialogs must show on UI thread

## Config & Feature Flags
- Email configuration: ErrorReport.EmailAddress, ErrorReport.EmailSubject

## Build Information
- Project: Reporting.csproj

## Interfaces and Data Models
ErrorReport, UsageEmailDialog, ReportingStrings.

## Entry Points
- ErrorReport.ReportNonFatalException(exception): Log non-fatal errors

## Test Index
No test project found.

## Usage Hints
- Fatal errors: `ErrorReport.ReportFatalException(ex);` shows dialog and exits

## Related Folders
- Common/Framework/: Application framework with error handling hooks

## References
See `.cache/copilot/diff-plan.json` for file details.
