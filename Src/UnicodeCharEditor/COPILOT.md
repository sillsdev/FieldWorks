---
last-reviewed: 2025-10-31
last-reviewed-tree: 467ec7f9c098941a46701a02a5f00e986ee7fc493548e5109d143c1e5e805cda
status: reviewed
---

<!-- copilot:auto-change-log start -->
## Change Log (auto)

This section is populated by running:
1. `python .github/plan_copilot_updates.py --folders <Folder>`
2. `python .github/copilot_apply_updates.py --folders <Folder>`

Do not edit this block manually; rerun the scripts above after code or doc updates.
<!-- copilot:auto-change-log end -->

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
  - **PuaListItemComparer** - Sorts by Unicode codepoint value
  - `ReadDataFromUnicodeFiles()` - Loads Unicode base data
  - `ReadCustomCharData()` - Loads CustomChars.xml
  - `WriteCustomCharData()` - Saves modifications to CustomChars.xml
- **CustomCharDlg** (CustomCharDlg.cs) - Dialog for editing individual character properties

### ICU Data Installation
- **PUAInstaller** (PUAInstaller.cs) - Installs CustomChars.xml into ICU data files
  - `Install(string icuDir, string customCharsFile)` - Main installation method
  - **UndoFiles** struct - Tracks backup files for rollback
  - Handles file locking via cleanup child process spawning
  - Modifies ICU's UnicodeData.txt, nfkc.txt, nfc.txt files

### Supporting Infrastructure
- **LogFile** (LogFile.cs) - File-based logging with `IsLogging`, `IsVerbose` properties
- **IcuLockedException** (IcuLockedException.cs) - Exception for ICU file access failures
- **UceException**, **PuaException** (UceException.cs, PuaException.cs) - Application-specific exceptions
- **ErrorCodes** enum (ErrorCodes.cs) - Application error code enumeration
- **IcuErrorCodes** enum (IcuErrorCodes.cs) - ICU-specific error codes
- **ErrorCodesExtensionMethods** - Extension methods for error code handling

## Technology Stack
- **Language**: C#
- **Target framework**: .NET Framework 4.8.x (net48)
- **Application type**: WinExe (Windows GUI application with command-line support)
- **UI framework**: System.Windows.Forms (WinForms)
- **Key libraries**:
  - LCModel.Core.Text (PUACharacter class, Unicode utilities)
  - Common/FwUtils (FwRegistryHelper, IHelpTopicProvider, MessageBoxUtils)
  - SIL.Utils
  - CommandLineParser (NuGet package for command-line parsing)
  - System.Windows.Forms (WinForms controls)
- **External data files**: ICU UnicodeData.txt, nfkc.txt, nfc.txt (Unicode normalization data)
- **User data**: CustomChars.xml (stored in local settings folder)
- **Platform**: Windows-only (file system paths, registry access)

## Dependencies
- **Upstream**: LCModel.Core.Text (PUACharacter, Unicode utilities), Common/FwUtils (FwRegistryHelper, IHelpTopicProvider, MessageBoxUtils), SIL.Utils, CommandLineParser (NuGet), System.Windows.Forms
- **Downstream consumers**: All FieldWorks applications that use ICU for Unicode normalization and character properties
- **External data**: ICU data folder (UnicodeData.txt, nfkc.txt, nfc.txt), CustomChars.xml (user data in local settings)

## Interop & Contracts
- **ICU data file modification**: PUAInstaller modifies ICU text files
  - Files: UnicodeData.txt (character properties), nfkc.txt (NFKC normalization), nfc.txt (NFC normalization)
  - Location: ICU data folder (typically Program Files\SIL\FieldWorks\Icu*\data\)
  - Format: Tab-delimited text with semicolon-separated fields per Unicode spec
- **CustomChars.xml contract**:
  - Location: Local application data folder (%LOCALAPPDATA%\SIL\FieldWorks\)
  - Format: XML with PUACharacter elements (codepoint, category, combining class, decomposition, etc.)
  - Schema: Defined by PUACharacter class serialization
- **Command-line interface**:
  - `-i, --install`: Install CustomChars.xml to ICU without GUI
  - `-l, --log`: Enable file logging
  - `-v, --verbose`: Verbose logging
  - `-c, --cleanup <processId>`: Clean up locked ICU files (spawned by installer)
  - Exit codes: 0 = success, non-zero = error (ErrorCodes enum values)
- **Process spawning**: PUAInstaller spawns child process with --cleanup for locked file handling
  - Purpose: Retry ICU file modification after parent releases locks
- **Registry access**: FwRegistryHelper for FieldWorks installation paths
- **Help system**: IHelpTopicProvider for F1 help integration

## Threading & Performance
UI thread for all operations (WinForms single-threaded). Synchronous file I/O. Spawns child process for locked file retry.

## Config & Feature Flags
Command-line switches: --install (headless), --log, --verbose, --cleanup. CustomChars.xml in %LOCALAPPDATA%\SIL\FieldWorks\. ICU data folder via registry.

## Build Information
C# WinExe. Build via `msbuild UnicodeCharEditor.csproj`. Output: UnicodeCharEditor.exe.

## Interfaces and Data Models
IHelpTopicProvider for F1 help. PUACharacter (from LCModel.Core.Text) for PUA definitions. Exceptions: IcuLockedException, UceException, PuaException.

## Entry Points
GUI mode: `UnicodeCharEditor.exe`. Command-line: `--install` (headless), `--log --verbose` (logging). Invoked from FLEx via Tools→Unicode Character Editor.

## Test Index
- **Test project**: UnicodeCharEditorTests/UnicodeCharEditorTests.csproj
- **Test file**: PUAInstallerTests.cs
  - Tests PUAInstaller.Install() logic
  - Verifies ICU data file modification
  - Tests backup/rollback on error
  - Tests file locking handling
- **Test coverage**:
  - ICU data installation: Verify UnicodeData.txt, nfkc.txt, nfc.txt updated
  - Backup creation: Verify .bak files created before modification
  - Rollback on error: Verify .bak files restored on IcuLockedException
  - CustomChars.xml parsing: Verify PUACharacter serialization
- **Manual testing**:
  - Launch GUI: UnicodeCharEditor.exe
  - Add PUA character (e.g., U+E000), set properties
  - Save and install
  - Verify ICU data files updated
  - Test in FLEx: Use PUA character in text, verify proper display/normalization
- **Test runners**: Visual Studio Test Explorer, `dotnet test`
- **Test data**: Sample CustomChars.xml files for various scenarios

## Usage Hints
- **Typical workflow**:
  1. Launch: Tools→Unicode Character Editor in FLEx (or standalone UnicodeCharEditor.exe)
  2. Add PUA character: Click "Add", enter codepoint (e.g., E000 for U+E000)
  3. Set properties: General category, combining class, decomposition, name
  4. Save: File→Save (writes CustomChars.xml)
  5. Install: File→Install (modifies ICU data files)
  6. Restart FLEx to use updated character properties
- **Private Use Area ranges**:
  - U+E000–U+F8FF: BMP Private Use Area (main range for custom characters)
  - U+F0000–U+FFFFD, U+100000–U+10FFFD: Supplementary planes (less common)
- **Common properties**:
  - **General Category**: Lo (Other Letter) for linguistic symbols
  - **Combining Class**: 0 (base), 1-254 (combining marks), 255 (special)
  - **Decomposition**: Optional canonical or compatibility decomposition
  - **Name**: Descriptive name for the character
- **Command-line installation**:
  ```cmd
  UnicodeCharEditor.exe --install --log
  ```
  - Installs without GUI, logs to UnicodeCharEditor.log
- **Troubleshooting**:
  - **ICU files locked**: Close all FLEx instances, retry installation
  - **Changes not applied**: Restart FLEx after installation
  - **CustomChars.xml not found**: Save at least once to create file
- **Common pitfalls**:
  - Forgetting to install after editing (changes only saved to XML, not ICU)
  - Not restarting FLEx (ICU data loaded at startup)
  - Using non-PUA codepoints (can break Unicode compliance)
  - Invalid decomposition (must reference valid Unicode codepoints)
- **Advanced usage**:
  - UnicodeDataOverrides.txt: Override standard Unicode properties (expert users only)
  - Batch editing: Edit CustomChars.xml directly, run --install
- **Backup**: CustomChars.xml backed up automatically before installation

## Related Folders
- **LCModel.Core/**: PUACharacter class
- **Common/FwUtils/**: Registry access

## References
16 C# files (~4.1K lines). Key: Program.cs, CharEditorWindow.cs, PUAInstaller.cs. See `.cache/copilot/diff-plan.json` for file listings.
