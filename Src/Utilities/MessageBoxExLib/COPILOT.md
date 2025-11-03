---
last-reviewed: 2025-11-01
last-reviewed-tree: fd48d4d12ff66731f0299c2c03fb169e6418ec5e4c698429feacecf10f3ce67e
status: production
---

# MessageBoxExLib

## Purpose
Enhanced message box library from CodeProject (http://www.codeproject.com/cs/miscctrl/MessageBoxEx.asp). Extends standard Windows MessageBox with custom buttons, "don't show again" checkbox (saved response), custom icons, timeout support, and richer formatting. Used throughout FieldWorks for user notifications and confirmation dialogs.

## Architecture
TBD - populate from code. See auto-generated hints below.

## Key Components

### MessageBoxEx.cs (~200 lines)
- **MessageBoxEx**: Main API (sealed class, IDisposable)
  - **Show()**: Static methods returning string (button text clicked)
  - Properties: Caption, Text, CustomIcon, Icon, Font, AllowSaveResponse, SaveResponseText, UseSavedResponse, PlayAlertSound, Timeout
  - **AddButton(string text, string value)**: Custom button configuration
  - **AddButtons()**: Helper for standard button sets
  - Uses MessageBoxExManager for saved responses

### MessageBoxExForm.cs (~700 lines)
- **MessageBoxExForm**: WinForms dialog (internal)
  - Dynamic button layout, icon display, checkbox for "don't show again"
  - **Show(IWin32Window owner)**: Displays modal dialog
  - Timeout support (closes after specified milliseconds)
  - Sound playback based on icon type

### Supporting Types (~50 lines total)
- **MessageBoxExButton**: Custom button (Text, Value properties)
- **MessageBoxExButtons**: Enum (OK, OKCancel, YesNo, YesNoCancel, etc.)
- **MessageBoxExIcon**: Enum (Information, Warning, Error, Question, None)
- **MessageBoxExResult**: Struct (Value, Saved response fields)
- **TimeoutResult**: Enum (Default, Timeout)
- **MessageBoxExManager**: Manages saved responses (registry or config)

## Technology Stack
TBD - populate from code. See auto-generated hints below.

## Dependencies
- **System.Windows.Forms**: Dialog infrastructure
- **Consumer**: All FieldWorks applications (FwCoreDlgs, xWorks, LexText, etc.) for user notifications

## Interop & Contracts
TBD - populate from code. See auto-generated hints below.

## Threading & Performance
TBD - populate from code. See auto-generated hints below.

## Config & Feature Flags
TBD - populate from code. See auto-generated hints below.

## Build Information
- **Project**: MessageBoxExLib.csproj
- **Type**: Library (.NET Framework 4.6.2)
- **Output**: MessageBoxExLib.dll
- **Namespace**: Utils.MessageBoxExLib
- **Source files**: 10 files (~1646 lines)
- **Resources**: MessageBoxExForm.resx, StandardButtonsText.resx, Icon_2.ico through Icon_5.ico

## Interfaces and Data Models
TBD - populate from code. See auto-generated hints below.

## Entry Points
TBD - populate from code. See auto-generated hints below.

## Test Index
Test project: MessageBoxExLibTests with Tests.cs. Run via Test Explorer or `dotnet test`.

## Usage Hints
TBD - populate from code. See auto-generated hints below.

## Related Folders
- **FwCoreDlgs/**: Standard FieldWorks dialogs (uses MessageBoxEx)
- **Common/FwUtils/**: General utilities
- Used throughout FieldWorks applications

## References
- **System.Windows.Forms.Form**: Base dialog class
- **System.Windows.Forms.IWin32Window**: Owner window interface
- **Utils.MessageBoxExLib.MessageBoxExManager**: Saved response persistence

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
