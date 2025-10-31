---
last-reviewed: 2025-10-30
last-verified-commit: 9611cf70e
status: draft
---

# UnicodeCharEditor

## Purpose
Specialized tool for viewing and editing Unicode character properties.
Provides detailed information about Unicode characters, their properties, and rendering
characteristics. Useful for linguists working with uncommon scripts or diagnosing
character encoding and display issues.

## Architecture
C# library with 16 source files. Contains 1 subprojects: UnicodeCharEditor.

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

## Interop & Contracts
Uses COM for cross-boundary calls.

## Threading & Performance
Threading model: explicit threading, synchronization.

## Config & Feature Flags
Config files: App.config.

## Build Information
- C# WinForms application
- Includes test suite
- Build with MSBuild or Visual Studio

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

## Entry Points
- Standalone application for Unicode character editing
- May be launched from main FieldWorks applications

## Test Index
Test projects: UnicodeCharEditorTests. 1 test files. Run via: `dotnet test` or Test Explorer in Visual Studio.

## Usage Hints
Library component. Reference in consuming projects. See Dependencies section for integration points.

## Related Folders
- **Common/** - UI infrastructure for the editor
- **Kernel/** - String utilities for Unicode handling
- **LexText/** - May use custom character definitions from editor

## References

- **Project files**: UnicodeCharEditor.csproj, UnicodeCharEditorTests.csproj
- **Target frameworks**: net462
- **Key C# files**: CharEditorWindow.Designer.cs, CharEditorWindow.cs, CustomCharDlg.cs, ErrorCodes.cs, HelpTopicPaths.Designer.cs, IcuErrorCodes.cs, IcuLockedException.cs, PUAInstaller.cs, PuaException.cs, UceException.cs
- **Source file count**: 16 files
- **Data file count**: 5 files

## References (auto-generated hints)
- Project files:
  - Src/UnicodeCharEditor/BuildInclude.targets
  - Src/UnicodeCharEditor/UnicodeCharEditor.csproj
  - Src/UnicodeCharEditor/UnicodeCharEditorTests/UnicodeCharEditorTests.csproj
- Key C# files:
  - Src/UnicodeCharEditor/CharEditorWindow.Designer.cs
  - Src/UnicodeCharEditor/CharEditorWindow.cs
  - Src/UnicodeCharEditor/CustomCharDlg.cs
  - Src/UnicodeCharEditor/ErrorCodes.cs
  - Src/UnicodeCharEditor/HelpTopicPaths.Designer.cs
  - Src/UnicodeCharEditor/IcuErrorCodes.cs
  - Src/UnicodeCharEditor/IcuLockedException.cs
  - Src/UnicodeCharEditor/LogFile.cs
  - Src/UnicodeCharEditor/PUAInstaller.cs
  - Src/UnicodeCharEditor/Program.cs
  - Src/UnicodeCharEditor/Properties/AssemblyInfo.cs
  - Src/UnicodeCharEditor/Properties/Resources.Designer.cs
  - Src/UnicodeCharEditor/Properties/Settings.Designer.cs
  - Src/UnicodeCharEditor/PuaException.cs
  - Src/UnicodeCharEditor/UceException.cs
  - Src/UnicodeCharEditor/UnicodeCharEditorTests/PUAInstallerTests.cs
- Data contracts/transforms:
  - Src/UnicodeCharEditor/App.config
  - Src/UnicodeCharEditor/CharEditorWindow.resx
  - Src/UnicodeCharEditor/CustomCharDlg.resx
  - Src/UnicodeCharEditor/HelpTopicPaths.resx
  - Src/UnicodeCharEditor/Properties/Resources.resx
## Code Evidence
*Analysis based on scanning 11 source files*

- **Classes found**: 9 public classes
- **Namespaces**: SIL.FieldWorks.UnicodeCharEditor
