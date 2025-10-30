---
owner: FIXME(set-owner)
last-reviewed: 2025-10-30
status: verified
---

# MessageBoxExLib

## Purpose
Enhanced message box library for FieldWorks. Provides extended message box functionality beyond standard Windows message boxes, including additional icons and customization options.

## Key Components
### Key Classes
- **MessageBoxEx**
- **MessageBoxExButton**
- **MessageBoxExManager**
- **MessageBoxTests**

## Technology Stack
- C# .NET WinForms
- Windows Forms extensions
- Custom dialog implementation

## Dependencies
- Depends on: System.Windows.Forms
- Used by: All FieldWorks applications for user notifications

## Build Information
- C# class library project
- Build via: `dotnet build MessageBoxExLib.csproj`
- UI utility library

## Entry Points
- MessageBoxEx.Show() methods
- Enhanced dialog display API

## Related Folders
- **FwCoreDlgs/** - Standard FieldWorks dialogs
- **Common/FwUtils/** - General utilities
- Used throughout FieldWorks applications

## Code Evidence
*Analysis based on scanning 10 source files*

- **Classes found**: 4 public classes
- **Namespaces**: Utils.MessageBoxExLib
