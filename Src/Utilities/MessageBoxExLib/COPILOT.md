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

## Interfaces and Data Models

- **MessageBoxEx** (class)
  - Path: `MessageBoxEx.cs`
  - Public class implementation

- **MessageBoxExButton** (class)
  - Path: `MessageBoxExButton.cs`
  - Public class implementation

- **MessageBoxExManager** (class)
  - Path: `MessageBoxExManager.cs`
  - Public class implementation

- **MessageBoxExResult** (struct)
  - Path: `MessageBoxExResult.cs`

- **MessageBoxExButtons** (enum)
  - Path: `MessageBoxExButtons.cs`

- **MessageBoxExIcon** (enum)
  - Path: `MessageBoxExIcon.cs`

- **TimeoutResult** (enum)
  - Path: `TimeoutResult.cs`

## References

- **Project files**: MessageBoxExLib.csproj, MessageBoxExLibTests.csproj
- **Target frameworks**: net462
- **Key C# files**: AssemblyInfo.cs, MessageBoxEx.cs, MessageBoxExButton.cs, MessageBoxExButtons.cs, MessageBoxExForm.cs, MessageBoxExIcon.cs, MessageBoxExManager.cs, MessageBoxExResult.cs, Tests.cs, TimeoutResult.cs
- **Source file count**: 10 files
- **Data file count**: 2 files
