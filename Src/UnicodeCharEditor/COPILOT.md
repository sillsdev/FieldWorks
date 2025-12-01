---
last-reviewed: 2025-10-31
last-reviewed-tree: 55d829f09839299e425827bd1353d70cfb0dde730c43f7d3e8b0ec6e1cf81457
status: reviewed
---

<!-- copilot:auto-change-log start -->
## Change Log (auto)

- Snapshot: HEAD~1
- Risk: none
- Files: 0 (code=0, tests=0, resources=0)

### Prompt seeds
- Update COPILOT.md for Src/UnicodeCharEditor. Prioritize Purpose/Architecture sections using planner data.
- Highlight API or UI updates, then confirm Usage/Test sections reflect 0 files changed (code=0, tests=0, resources=0); risk=none.
- Finish with verification notes and TODOs for manual testing.
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
- **UI thread**: All UI operations on main thread (WinForms single-threaded model)
- **Synchronous operations**: File I/O and ICU data installation synchronous
- **Performance characteristics**:
  - Unicode data loading: Fast (<1 second for UnicodeData.txt ~1.5MB)
  - CustomChars.xml load/save: Fast (<100ms for typical PUA definitions)
  - ICU data installation: Moderate (1-3 seconds, modifies 3 files)
  - File locking retry: Can add seconds if ICU files locked by other processes
- **Dictionary lookups**: Dictionary<int, PUACharacter> for O(1) character access
  - m_dictCustomChars: User-defined PUA overrides
  - m_dictModifiedChars: Standard Unicode overrides
- **ListView sorting**: PuaListItemComparer sorts by numeric codepoint (efficient)
- **No background threading**: All operations on UI thread with progress feedback via UI updates
- **File locking handling**: Spawns child process with --cleanup to retry on IcuLockedException
- **No caching**: Unicode data loaded fresh on startup (~1 second overhead)

## Config & Feature Flags
- **Command-line mode switches**:
  - `--install`: Headless installation (no GUI), installs CustomChars.xml to ICU
  - `--log`: Enable LogFile.IsLogging (writes to UnicodeCharEditor.log)
  - `--verbose`: Enable LogFile.IsVerbose (detailed logging)
  - `--cleanup <processId>`: Internal mode for locked file retry (spawned by installer)
- **CustomChars.xml location**: %LOCALAPPDATA%\SIL\FieldWorks\CustomChars.xml
  - Created on first save, persists user PUA definitions
- **ICU data folder**: Discovered via registry (FwRegistryHelper)
  - Typical: Program Files\SIL\FieldWorks\Icu*\data\
- **UnicodeDataOverrides.txt**: Optional file for standard Unicode property overrides
  - Allows modifying non-PUA characters (advanced use)
- **Backup files**: PUAInstaller creates .bak files before modifying ICU data
  - UndoFiles struct tracks backups for rollback on error
- **Error handling**: ErrorCodes enum for application errors, IcuErrorCodes for ICU-specific
- **Help topics**: HelpTopicPaths.resx for F1 help mapping

## Build Information
- **Project type**: C# WinExe (Windows GUI application)
- **Build**: `msbuild UnicodeCharEditor.csproj` or via FieldWorks.sln
- **Output**: UnicodeCharEditor.exe (standalone executable)
- **Dependencies**:
  - LCModel.Core.Text (PUACharacter)
  - Common/FwUtils (FW utilities)
  - SIL.Utils
  - CommandLineParser (NuGet)
  - System.Windows.Forms
- **Test project**: UnicodeCharEditorTests/UnicodeCharEditorTests.csproj (1 test file)
- **Resources**: 3 .resx files (CharEditorWindow, CustomCharDlg, HelpTopicPaths)
- **Config**: App.config (assembly bindings, settings)
- **Deployment**: Included in FLEx installer, accessible via Tools menu or standalone execution

## Interfaces and Data Models

### Interfaces
- **IHelpTopicProvider** (from Common/FwUtils)
  - Purpose: F1 help integration
  - Implementation: CharEditorWindow provides help topics for context-sensitive help
  - Methods: GetHelpTopic() returns help topic key

### Data Models
- **PUACharacter** (from LCModel.Core.Text)
  - Purpose: Represents Private Use Area character with Unicode properties
  - Properties: CodePoint (int), GeneralCategory, CombiningClass, Decomposition, Name, etc.
  - Usage: Serialized to/from CustomChars.xml

- **PuaListItem** (nested class in CharEditorWindow)
  - Purpose: ListView item wrapper for PUA characters
  - Properties: Character, Text (hex representation)
  - Sorting: Via PuaListItemComparer by numeric codepoint

- **UndoFiles** (struct in PUAInstaller)
  - Purpose: Track ICU data file backups for rollback
  - Properties: UnicodeDataBakPath, NfkcBakPath, NfcBakPath
  - Usage: Created before ICU modification, restored on error

### Exceptions
- **IcuLockedException**: Thrown when ICU data files locked by another process
- **UceException**: General UnicodeCharEditor exception (base class)
- **PuaException**: PUA-specific exception

### Enums
- **ErrorCodes**: Application error codes (Success, FileAccessError, InvalidArgument, etc.)
- **IcuErrorCodes**: ICU-specific error codes (mapped from ICU return values)

## Entry Points
- **GUI mode** (default): `UnicodeCharEditor.exe`
  - Launches CharEditorWindow
  - User edits PUA characters, saves to CustomChars.xml
  - Manual install via File→Install or automatic on save
- **Command-line install**: `UnicodeCharEditor.exe --install`
  - Headless mode: Installs CustomChars.xml to ICU without GUI
  - Used by FLEx installer or automated scripts
  - Exit code indicates success/failure
- **Logging mode**: `UnicodeCharEditor.exe --log --verbose`
  - Enables detailed logging to UnicodeCharEditor.log
  - Useful for troubleshooting installation issues
- **Cleanup mode** (internal): `UnicodeCharEditor.exe --cleanup <processId>`
  - Spawned by PUAInstaller when ICU files locked
  - Waits for parent process to exit, retries installation
- **Invocation from FLEx**: Tools→Unicode Character Editor
  - Launches UnicodeCharEditor.exe as separate process
- **Typical workflows**:
  - Create PUA character: Add entry, set properties, save, install
  - Modify existing: Edit in list, change properties, save
  - Remove character: Delete from list, save, install
  - Batch install: Edit offline, run with --install flag

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
- **LCModel.Core/** - PUACharacter class definition, Unicode utilities
- **Common/FwUtils/** - Registry access, help topic provider interface
- **Kernel/** - May consume installed PUA character definitions

## References
- **Project**: UnicodeCharEditor.csproj (.NET Framework 4.8.x WinExe)
- **Test project**: UnicodeCharEditorTests/UnicodeCharEditorTests.csproj
- **16 CS files** (~4.1K lines): Program.cs, CharEditorWindow.cs, CustomCharDlg.cs, PUAInstaller.cs, LogFile.cs, exceptions, enums
- **Resources**: CharEditorWindow.resx, CustomCharDlg.resx, HelpTopicPaths.resx
- **Config**: App.config

## Auto-Generated Project and File References
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
## Test Infrastructure
- **UnicodeCharEditorTests/** subfolder with 1 test file
- **PUAInstallerTests** - Tests for ICU data installation logic
- Run via: `dotnet test` or Visual Studio Test Explorer
