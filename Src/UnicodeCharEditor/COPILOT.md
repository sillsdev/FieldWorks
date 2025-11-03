---
last-reviewed: 2025-10-31
last-reviewed-tree: 55d829f09839299e425827bd1353d70cfb0dde730c43f7d3e8b0ec6e1cf81457
status: reviewed
---

# UnicodeCharEditor

## Purpose
Standalone WinForms application (~4.1K lines) for managing Private Use Area (PUA) character definitions in FieldWorks. Allows linguists to create, edit, and install custom character properties that override Unicode defaults. Writes to CustomChars.xml and installs data into ICU's data folder for use across FieldWorks applications.

## Architecture
TBD - populate from code. See auto-generated hints below.

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
TBD - populate from code. See auto-generated hints below.

## Dependencies
- **Upstream**: LCModel.Core.Text (PUACharacter, Unicode utilities), Common/FwUtils (FwRegistryHelper, IHelpTopicProvider, MessageBoxUtils), SIL.Utils, CommandLineParser (NuGet), System.Windows.Forms
- **Downstream consumers**: All FieldWorks applications that use ICU for Unicode normalization and character properties
- **External data**: ICU data folder (UnicodeData.txt, nfkc.txt, nfc.txt), CustomChars.xml (user data in local settings)

## Interop & Contracts
TBD - populate from code. See auto-generated hints below.

## Threading & Performance
TBD - populate from code. See auto-generated hints below.

## Config & Feature Flags
TBD - populate from code. See auto-generated hints below.

## Build Information
TBD - populate from code. See auto-generated hints below.

## Interfaces and Data Models
TBD - populate from code. See auto-generated hints below.

## Entry Points
TBD - populate from code. See auto-generated hints below.

## Test Index
TBD - populate from code. See auto-generated hints below.

## Usage Hints
TBD - populate from code. See auto-generated hints below.

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
## Test Infrastructure
- **UnicodeCharEditorTests/** subfolder with 1 test file
- **PUAInstallerTests** - Tests for ICU data installation logic
- Run via: `dotnet test` or Visual Studio Test Explorer
