---
applyTo: "Src/UnicodeCharEditor/**"
name: "unicodechareditor.instructions"
description: "Auto-generated concise instructions from COPILOT.md for UnicodeCharEditor"
---

# UnicodeCharEditor (Concise)

## Purpose & Scope
Summarized key points from COPILOT.md

## Key Rules
- **Program** (Program.cs) - Entry point with command-line parsing
- `-i, --install` switch - Install CustomChars.xml data to ICU folder without GUI
- `-l, --log` switch - Enable logging to file
- `-v, --verbose` switch - Enable verbose logging
- `-c, --cleanup <processId>` - Clean up locked ICU files (background process)
- Uses CommandLineParser library for argument handling

## Example (from summary)

---
last-reviewed: 2025-10-31
last-reviewed-tree: 55d829f09839299e425827bd1353d70cfb0dde730c43f7d3e8b0ec6e1cf81457
status: reviewed
---

# UnicodeCharEditor

## Purpose
Standalone WinForms application (~4.1K lines) for managing Private Use Area (PUA) character definitions in FieldWorks. Allows linguists to create, edit, and install custom character properties that override Unicode defaults. Writes to CustomChars.xml and installs data into ICU's data folder for use across FieldWorks applications.

## Architecture
C# WinForms application (net48, WinExe) with 16 source files (~4.1K lines). Single-window UI (CharEditorWindow) for editing Private Use Area characters + command-line installer mode (PUAInstaller). Three-layer architecture:
1. **UI layer**: CharEditorWindow (main form), CustomCharDlg (character editor)
2. **Business logic**: PUAInstaller (ICU data modification), character dictionaries
3. **Infrastructure**: LogFile, exceptions, error codes

Workflow: User edits PUA characters → saves to CustomChars.xml → installs to ICU data files → FieldWorks apps use updated character properties for normalization/display.

## Key Components

### Main Application
- **Program** (Program.cs) - Entry point with command-line parsing
  - `-i, --install` switch - Install CustomChars.xml data to ICU folder without GUI
  - `-l, --log` switch - Enable logging to file
  - `-v, --verbose` switch - Enable verbose logging
  - `-c, --cleanup <processId>` - Clean up locked ICU files (background process)
  - Uses CommandLineParser library for argument handling

### Character Editor UI
- **CharEditorWindow** (CharEditorWindow.cs) - Main form implementing IHelpTopicProvider
  - `m_dictCustomChars` - Dictionary<int, PUACharacter> for user overrides from CustomChars.xml
  - `m_dictModifiedChars` - Dictionary<int, PUACharacter> for standard Unicode overrides from UnicodeDataOverrides.txt
  - **PuaListItem** nested class - ListView items with hex code sorting
  - **PuaListItemComparer** - Sorts
