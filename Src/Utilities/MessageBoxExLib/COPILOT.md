---
last-reviewed: 2025-10-30
last-verified-commit: 9611cf70e
status: draft
---

# MessageBoxExLib

## Purpose
Enhanced message box library with extended functionality.
Provides message boxes with additional features beyond standard Windows message boxes,
such as custom button layouts, checkboxes for "don't show again", and richer formatting.
Enables better user communication throughout FieldWorks applications.

## Architecture
C# library with 10 source files. Contains 1 subprojects: MessageBoxExLib.

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

## Interop & Contracts
Uses P/Invoke for cross-boundary calls.

## Threading & Performance
Single-threaded or thread-agnostic code. No explicit threading detected.

## Config & Feature Flags
No explicit configuration or feature flags detected.

## Build Information
- C# class library project
- Build via: `dotnet build MessageBoxExLib.csproj`
- UI utility library

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

## Entry Points
- MessageBoxEx.Show() methods
- Enhanced dialog display API

## Test Index
Test projects: MessageBoxExLibTests. 1 test files. Run via: `dotnet test` or Test Explorer in Visual Studio.

## Usage Hints
Library component. Reference in consuming projects. See Dependencies section for integration points.

## Related Folders
- **FwCoreDlgs/** - Standard FieldWorks dialogs
- **Common/FwUtils/** - General utilities
- Used throughout FieldWorks applications

## References

- **Project files**: MessageBoxExLib.csproj, MessageBoxExLibTests.csproj
- **Target frameworks**: net462
- **Key C# files**: AssemblyInfo.cs, MessageBoxEx.cs, MessageBoxExButton.cs, MessageBoxExButtons.cs, MessageBoxExForm.cs, MessageBoxExIcon.cs, MessageBoxExManager.cs, MessageBoxExResult.cs, Tests.cs, TimeoutResult.cs
- **Source file count**: 10 files
- **Data file count**: 2 files

## References (auto-generated hints)
- Project files:
  - Utilities/MessageBoxExLib/MessageBoxExLib.csproj
  - Utilities/MessageBoxExLib/MessageBoxExLibTests/MessageBoxExLibTests.csproj
- Key C# files:
  - Utilities/MessageBoxExLib/AssemblyInfo.cs
  - Utilities/MessageBoxExLib/MessageBoxEx.cs
  - Utilities/MessageBoxExLib/MessageBoxExButton.cs
  - Utilities/MessageBoxExLib/MessageBoxExButtons.cs
  - Utilities/MessageBoxExLib/MessageBoxExForm.cs
  - Utilities/MessageBoxExLib/MessageBoxExIcon.cs
  - Utilities/MessageBoxExLib/MessageBoxExLibTests/Tests.cs
  - Utilities/MessageBoxExLib/MessageBoxExManager.cs
  - Utilities/MessageBoxExLib/MessageBoxExResult.cs
  - Utilities/MessageBoxExLib/TimeoutResult.cs
- Data contracts/transforms:
  - Utilities/MessageBoxExLib/MessageBoxExForm.resx
  - Utilities/MessageBoxExLib/Resources/StandardButtonsText.resx
## Code Evidence
*Analysis based on scanning 10 source files*

- **Classes found**: 4 public classes
- **Namespaces**: Utils.MessageBoxExLib
