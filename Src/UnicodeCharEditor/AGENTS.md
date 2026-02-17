---
last-reviewed: 2025-10-31
last-reviewed-tree: 467ec7f9c098941a46701a02a5f00e986ee7fc493548e5109d143c1e5e805cda
status: reviewed
---
anchors:
  - change-log-auto
  - purpose
  - architecture
  - key-components
  - technology-stack
  - dependencies
  - interop--contracts
  - threading--performance
  - config--feature-flags
  - build-information
  - interfaces-and-data-models
  - entry-points
  - test-index
  - usage-hints
  - related-folders
  - references

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
- **Program**: Entry point with command-line parsing (-i install, -l log, -v verbose, -c cleanup)
- **CharEditorWindow/CustomCharDlg**: Main form and character editor dialog for PUA character management
- **PUAInstaller**: Installs CustomChars.xml into ICU data files (UnicodeData.txt, nfkc.txt, nfc.txt)
- **LogFile, exceptions**: Infrastructure for logging and error handling

## Technology Stack
C# WinForms (net48, WinExe). Key libraries: LCModel.Core.Text (PUACharacter), Common/FwUtils, CommandLineParser. External data: ICU UnicodeData.txt, CustomChars.xml.

## Dependencies
**Upstream**: LCModel.Core.Text, Common/FwUtils, CommandLineParser
**Downstream**: All FieldWorks applications (via ICU Unicode normalization)

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
Launch via Tools→Unicode Character Editor, add PUA characters (U+E000–U+F8FF), set properties, save to CustomChars.xml, install to ICU data files, restart FLEx. Command-line: `UnicodeCharEditor.exe --install --log`.

## Related Folders
- **LCModel.Core/**: PUACharacter class
- **Common/FwUtils/**: Registry access

## References
16 C# files (~4.1K lines). Key: Program.cs, CharEditorWindow.cs, PUAInstaller.cs. See `.cache/copilot/diff-plan.json` for file listings.
