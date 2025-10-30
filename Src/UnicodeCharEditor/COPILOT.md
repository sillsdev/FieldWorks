---
owner: FIXME(set-owner)
last-reviewed: 2025-10-30
status: verified
---

# UnicodeCharEditor

## Purpose
Specialized tool for viewing and editing Unicode character properties. 
Provides detailed information about Unicode characters, their properties, and rendering 
characteristics. Useful for linguists working with uncommon scripts or diagnosing 
character encoding and display issues.

## Key Components
### Key Classes
- **CharEditorWindow**
- **IcuLockedException**
- **UceException**
- **CustomCharDlg**
- **PUAInstaller**
- **PuaException**
- **ErrorCodesExtensionMethods**
- **LogFile**
- **PUAInstallerTests**

## Technology Stack
- C# .NET WinForms
- Unicode character database integration
- Character property editing

## Dependencies
- Depends on: Common (UI infrastructure), Unicode data files
- Used by: Linguists needing to edit character properties

## Build Information
- C# WinForms application
- Includes test suite
- Build with MSBuild or Visual Studio

## Entry Points
- Standalone application for Unicode character editing
- May be launched from main FieldWorks applications

## Related Folders
- **Common/** - UI infrastructure for the editor
- **Kernel/** - String utilities for Unicode handling
- **LexText/** - May use custom character definitions from editor

## Code Evidence
*Analysis based on scanning 11 source files*

- **Classes found**: 9 public classes
- **Namespaces**: SIL.FieldWorks.UnicodeCharEditor

## Interfaces and Data Models

- **CustomCharDlg** (class)
  - Path: `CustomCharDlg.cs`
  - Public class implementation

- **ErrorCodesExtensionMethods** (class)
  - Path: `ErrorCodes.cs`
  - Public class implementation

- **IcuLockedException** (class)
  - Path: `IcuLockedException.cs`
  - Public class implementation

- **LogFile** (class)
  - Path: `LogFile.cs`
  - Public class implementation

- **PUAInstaller** (class)
  - Path: `PUAInstaller.cs`
  - Public class implementation

- **PuaException** (class)
  - Path: `PuaException.cs`
  - Public class implementation

- **UceException** (class)
  - Path: `UceException.cs`
  - Public class implementation

- **UndoFiles** (struct)
  - Path: `PUAInstaller.cs`

- **ErrorCodes** (enum)
  - Path: `ErrorCodes.cs`

- **IcuErrorCodes** (enum)
  - Path: `IcuErrorCodes.cs`

## References

- **Project files**: UnicodeCharEditor.csproj, UnicodeCharEditorTests.csproj
- **Target frameworks**: net462
- **Key C# files**: CharEditorWindow.Designer.cs, CharEditorWindow.cs, CustomCharDlg.cs, ErrorCodes.cs, HelpTopicPaths.Designer.cs, IcuErrorCodes.cs, IcuLockedException.cs, PUAInstaller.cs, PuaException.cs, UceException.cs
- **Source file count**: 16 files
- **Data file count**: 5 files
