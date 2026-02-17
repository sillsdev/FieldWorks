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
C# .NET Framework 4.8.x class library. System.Windows.Forms (UI), System.Drawing (icons), System.Media (sound). Resources: StandardButtonsText.resx (localization), custom icons.

## Dependencies
Consumes: System.Windows.Forms. Used by: All FieldWorks applications (FwCoreDlgs, xWorks, LexText) for user notifications.

## Interop & Contracts
MessageBox replacement with static Show() methods. Returns string or MessageBoxExResult. Custom buttons via AddButton(). Saved responses ("don't show again") via MessageBoxExManager (registry/config storage). Timeout support auto-closes dialog. Sound playback via PlayAlertSound.

## Threading & Performance
UI thread required (WinForms). Modal dialog blocks until response/timeout. Lightweight construction (<50ms). Saved response cache fast after first load.

## Config & Feature Flags
AllowSaveResponse (enable "don't show again"), SaveResponseText (checkbox label), UseSavedResponse, PlayAlertSound, Timeout (milliseconds), Custom font. MessageBoxExManager stores responses by caption+text hash.

## Build Information
MessageBoxExLib.csproj (net48, Library). Output: MessageBoxExLib.dll. 10 files (~1646 lines).

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
MessageBoxEx.Show() static methods: basic `Show("Message", "Caption")`, with buttons/icons, custom buttons via instance + AddButton(), timeout support.

## Test Index
MessageBoxExLibTests/Tests.cs. Run via Test Explorer or `dotnet test`.

## Usage Hints
Drop-in MessageBox.Show() replacement. Custom buttons: create instance, AddButton(), Show(). Enable "don't show again" via AllowSaveResponse. Set Timeout for auto-close. Saved responses persist (clear with MessageBoxExManager.ClearSavedResponses()). Localized button text in StandardButtonsText.resx. Dispose custom instances.

## Related Folders
FwCoreDlgs/ (uses MessageBoxEx), Common/FwUtils/. Used throughout FieldWorks.

## References
MessageBoxExLib.csproj (net48). Key files: MessageBoxEx.cs, MessageBoxExForm.cs (~700 lines), MessageBoxExManager.cs. Original source: CodeProject. See `.cache/copilot/diff-plan.json` for complete file listing.
