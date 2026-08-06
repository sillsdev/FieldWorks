## ADDED Requirements

### Requirement: Windows Views Uses Native Input and Window Geometry Paths
Windows FieldWorks SHALL use native Views/Win32 paths for RootSite input management and window geometry after Linux-era managed shim removal.

#### Scenario: RootBox input manager is initialized on Windows
- **WHEN** `VwRootBox::Init()` runs in a Windows build
- **THEN** it MUST create or retain the native `VwTextStore` path
- **AND** it MUST obtain the active input manager through `IID_IViewInputMgr` from `VwTextStore`, not through managed `ViewInputManager` COM activation.

#### Scenario: Selection and page geometry are calculated on Windows
- **WHEN** Views code needs the RootSite client rectangle for page movement or visible page height
- **THEN** it MUST use the Win32 `GetClientRect` path for the RootSite HWND
- **AND** it MUST NOT depend on `ManagedVwWindow` for Windows geometry.

### Requirement: Views Shim Removal Preserves User-Visible Editing Behavior
Removing Linux-era managed Views shims SHALL preserve Windows text editing, keyboard switching, IME composition, selection movement, and page navigation behavior.

#### Scenario: A user edits text in a RootSite field
- **WHEN** the user types, switches keyboards, or uses an IME/composition flow in a RootSite field
- **THEN** text input and composition behavior MUST match the pre-removal Windows behavior.

#### Scenario: A user navigates text by selection and page movement
- **WHEN** the user moves the selection with keyboard/mouse or PageUp/PageDown
- **THEN** Views selection and page movement MUST continue to use the same visible client area semantics as before shim removal.
