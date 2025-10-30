---
owner: FIXME(set-owner)
last-reviewed: 2025-10-30
status: verified
---

# Utilities/Reporting

## Purpose
Error reporting functionality for FieldWorks. Provides infrastructure for collecting, displaying, and submitting error reports when exceptions or problems occur.

## Key Components
- **Reporting.csproj** - Error reporting library
- **ErrorReport.cs** - Error report collection and display
- Dialog image resources for error reporting UI

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
