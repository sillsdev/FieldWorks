---
last-reviewed: 2025-10-31
last-verified-commit: 4e824be
status: reviewed
---

# UnicodeCharEditor

## Purpose
Standalone WinForms application (~4.1K lines) for managing Private Use Area (PUA) character definitions in FieldWorks. Allows linguists to create, edit, and install custom character properties that override Unicode defaults. Writes to CustomChars.xml and installs data into ICU's data folder for use across FieldWorks applications.

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

## Dependencies
- **Upstream**: LCModel.Core.Text (PUACharacter, Unicode utilities), Common/FwUtils (FwRegistryHelper, IHelpTopicProvider, MessageBoxUtils), SIL.Utils, CommandLineParser (NuGet), System.Windows.Forms
- **Downstream consumers**: All FieldWorks applications that use ICU for Unicode normalization and character properties
- **External data**: ICU data folder (UnicodeData.txt, nfkc.txt, nfc.txt), CustomChars.xml (user data in local settings)

## Test Infrastructure
- **UnicodeCharEditorTests/** subfolder with 1 test file
- **PUAInstallerTests** - Tests for ICU data installation logic
- Run via: `dotnet test` or Visual Studio Test Explorer

## Related Folders
- **LCModel.Core/** - PUACharacter class definition, Unicode utilities
- **Common/FwUtils/** - Registry access, help topic provider interface
- **Kernel/** - May consume installed PUA character definitions

## References
- **Project**: UnicodeCharEditor.csproj (.NET Framework 4.6.2 WinExe)
- **Test project**: UnicodeCharEditorTests/UnicodeCharEditorTests.csproj
- **16 CS files** (~4.1K lines): Program.cs, CharEditorWindow.cs, CustomCharDlg.cs, PUAInstaller.cs, LogFile.cs, exceptions, enums
- **Resources**: CharEditorWindow.resx, CustomCharDlg.resx, HelpTopicPaths.resx
- **Config**: App.config
