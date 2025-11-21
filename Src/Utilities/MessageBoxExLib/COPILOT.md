---
last-reviewed: 2025-11-01
last-reviewed-tree: 0e6c1fb90b9acd157bd784e82d4850c6ae2209a41ebb28982ba1d71c0e51be99
status: production
---

<!-- copilot:auto-change-log start -->
## Change Log (auto)

This section is populated by running:
1. `python .github/plan_copilot_updates.py --folders <Folder>`
2. `python .github/copilot_apply_updates.py --folders <Folder>`

Do not edit this block manually; rerun the scripts above after code or doc updates.
<!-- copilot:auto-change-log end -->

# MessageBoxExLib

## Purpose
Enhanced message box library from CodeProject (http://www.codeproject.com/cs/miscctrl/MessageBoxEx.asp). Extends standard Windows MessageBox with custom buttons, "don't show again" checkbox (saved response), custom icons, timeout support, and richer formatting. Used throughout FieldWorks for user notifications and confirmation dialogs.

## Architecture
Enhanced MessageBox library (~1646 lines, 9 C# files) providing drop-in replacement for System.Windows.Forms.MessageBox with extended features. Three-layer design:
1. **API layer**: MessageBoxEx sealed class (static Show() methods)
2. **UI layer**: MessageBoxExForm (internal WinForms dialog with custom buttons, icons, timeout)
3. **Persistence layer**: MessageBoxExManager (saved responses in registry/config)

Original source: CodeProject (http://www.codeproject.com/cs/miscctrl/MessageBoxEx.asp), adapted for FieldWorks.

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
- **Language**: C#
- **Target framework**: .NET Framework 4.8.x (net48)
- **Library type**: Class library (DLL)
- **UI framework**: System.Windows.Forms
- **Key libraries**: System.Drawing (icons, fonts), System.Media (sound playback)
- **Resources**: StandardButtonsText.resx (localized button text), MessageBoxExForm.resx (dialog layout), custom icons (Icon_2.ico through Icon_5.ico)
- **Namespace**: Utils.MessageBoxExLib

## Dependencies
- **System.Windows.Forms**: Dialog infrastructure
- **Consumer**: All FieldWorks applications (FwCoreDlgs, xWorks, LexText, etc.) for user notifications

## Interop & Contracts
- **MessageBox replacement**: Static Show() methods compatible with System.Windows.Forms.MessageBox
  - Returns: string (button text clicked) or MessageBoxExResult struct
  - Overloads: Various combinations of text, caption, buttons, icon, owner window
- **Custom buttons**: AddButton(string text, string value) for non-standard button sets
- **Saved responses**: "Don't show again" checkbox with MessageBoxExManager persistence
  - Storage: Registry or app config (configurable)
  - Key: Caption + Text hash for unique identification
- **Timeout support**: Timeout property (milliseconds) auto-closes dialog
  - Returns: TimeoutResult.Timeout or TimeoutResult.Default
- **Sound playback**: PlayAlertSound property triggers system sounds (Exclamation, Question, etc.)
- **Custom icons**: CustomIcon property for application-specific icons
- **Owner window**: IWin32Window parameter for modal dialog parenting

## Threading & Performance
- **UI thread required**: Must call from UI thread (WinForms requirement)
- **Modal dialog**: Show() blocks until user responds or timeout
- **Timeout timer**: System.Windows.Forms.Timer for auto-close (no background thread)
- **Performance**: Lightweight (dialog construction <50ms)
- **Saved response lookup**: Fast (in-memory cache after first load from registry/config)
- **Sound playback**: Asynchronous (System.Media.SoundPlayer)
- **Button layout**: Dynamic sizing based on text length and button count
- **Memory**: Minimal overhead (dialog disposed after Show())

## Config & Feature Flags
- **AllowSaveResponse**: Enable "Don't show again" checkbox (default: false)
- **SaveResponseText**: Checkbox label text (default: "Don't show this message again")
- **UseSavedResponse**: Check saved responses before showing dialog (default: true)
- **PlayAlertSound**: Play system sound for icon type (default: true)
- **Timeout**: Auto-close after milliseconds (default: 0 = no timeout)
- **MessageBoxExManager settings**:
  - Storage location: Registry vs config file
  - Saved responses indexed by caption+text hash
- **Button text localization**: StandardButtonsText.resx for OK/Cancel/Yes/No/etc.
- **Custom font**: Font property for dialog text (default: system font)

## Build Information
- **Project**: MessageBoxExLib.csproj
- **Type**: Library (.NET Framework 4.8.x)
- **Output**: MessageBoxExLib.dll
- **Namespace**: Utils.MessageBoxExLib
- **Source files**: 10 files (~1646 lines)
- **Resources**: MessageBoxExForm.resx, StandardButtonsText.resx, Icon_2.ico through Icon_5.ico

## Interfaces and Data Models

### Classes
- **MessageBoxEx**: Main API (sealed, IDisposable)
  - Static Show() methods returning string or MessageBoxExResult
  - Properties: Caption, Text, Icon, CustomIcon, Font, AllowSaveResponse, Timeout
  - Methods: AddButton(), AddButtons(), Dispose()
- **MessageBoxExForm**: Internal WinForms dialog
  - Dynamic button layout, icon display, checkbox, timeout timer
  - Methods: Show(IWin32Window owner)
- **MessageBoxExManager**: Saved response persistence
  - Methods: GetSavedResponse(), SaveResponse(), ClearSavedResponses()

### Enums
- **MessageBoxExButtons**: OK, OKCancel, YesNo, YesNoCancel, RetryCancel, AbortRetryIgnore
- **MessageBoxExIcon**: Information, Warning, Error, Question, None
- **TimeoutResult**: Default, Timeout

### Structs
- **MessageBoxExButton**: Text (string), Value (string)
- **MessageBoxExResult**: Value (string), Saved (bool)

## Entry Points
- **MessageBoxEx.Show()**: Primary entry point (static methods)
  - Basic: `MessageBoxEx.Show("Message", "Caption")`
  - With buttons: `MessageBoxEx.Show("Message", "Caption", MessageBoxExButtons.YesNo)`
  - Custom buttons: `var mb = new MessageBoxEx(); mb.AddButton("Custom", "value"); mb.Show();`
  - With timeout: `mb.Timeout = 5000; mb.Show();` (5 second timeout)
- **Saved responses**: `mb.AllowSaveResponse = true; mb.UseSavedResponse = true;`
- **Common usage patterns**:
  - Confirmation: `MessageBoxEx.Show("Confirm?", "Title", MessageBoxExButtons.YesNo)`
  - Error: `MessageBoxEx.Show("Error!", "Error", MessageBoxExButtons.OK, MessageBoxExIcon.Error)`
  - Custom: Create instance, configure, call Show()

## Test Index
Test project: MessageBoxExLibTests with Tests.cs. Run via Test Explorer or `dotnet test`.

## Usage Hints
- **Drop-in replacement**: Replace `MessageBox.Show()` with `MessageBoxEx.Show()`
- **Custom buttons**: `var mb = new MessageBoxEx(); mb.AddButton("Option 1", "opt1"); mb.AddButton("Option 2", "opt2"); string result = mb.Show();`
- **"Don't show again"**: `mb.AllowSaveResponse = true;` (checkbox appears automatically)
- **Timeout**: `mb.Timeout = 10000;` (closes after 10 seconds)
- **Saved response check**: If user checked "don't show again", Show() returns saved value without displaying
- **Clear saved**: `MessageBoxExManager.ClearSavedResponses()` to reset all saved responses
- **Common pitfalls**:
  - Forgetting to dispose custom instance (use `using` or call Dispose())
  - Not checking for TimeoutResult.Timeout return value
  - Saved responses persist across sessions (clear if behavior changes)
- **Best practices**:
  - Use static Show() for simple cases
  - Use instance + AddButton() for custom scenarios
  - Test timeout behavior (ensure graceful handling)
- **Localization**: Button text from StandardButtonsText.resx (supports multiple languages)

## Related Folders
- **FwCoreDlgs/**: Standard FieldWorks dialogs (uses MessageBoxEx)
- **Common/FwUtils/**: General utilities
- Used throughout FieldWorks applications

## References
- **System.Windows.Forms.Form**: Base dialog class
- **System.Windows.Forms.IWin32Window**: Owner window interface
- **Utils.MessageBoxExLib.MessageBoxExManager**: Saved response persistence

## Auto-Generated Project and File References
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
