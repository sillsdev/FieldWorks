---
owner: FIXME(set-owner)
last-reviewed: 2025-10-30
status: verified
---

# UnicodeCharEditor

## Purpose
Unicode character editor tool for FieldWorks. Provides a specialized interface for viewing, editing, and managing Unicode character properties, enabling linguists to work with complex Unicode characters and special symbols.

## Key Components
- **UnicodeCharEditor.csproj** - Main character editor application
- **UnicodeCharEditorTests/UnicodeCharEditorTests.csproj** - Editor tests


## Key Classes/Interfaces
- **IcuLockedException**
- **CustomCharDlg**
- **PUAInstaller**
- **UndoFiles**
- **PuaException**
- **ErrorCodes**
- **IcuErrorCodes**

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

## Testing
- Run tests: `dotnet test UnicodeCharEditor/UnicodeCharEditorTests/UnicodeCharEditorTests.csproj`
- Tests cover character editing and Unicode handling

## Entry Points
- Standalone application for Unicode character editing
- May be launched from main FieldWorks applications

## Related Folders
- **Common/** - UI infrastructure for the editor
- **Kernel/** - String utilities for Unicode handling
- **LexText/** - May use custom character definitions from editor


## References
- **Project Files**: UnicodeCharEditor.csproj
- **Key C# Files**: CharEditorWindow.cs, CustomCharDlg.cs, ErrorCodes.cs, IcuErrorCodes.cs, IcuLockedException.cs, LogFile.cs, PUAInstaller.cs, Program.cs
