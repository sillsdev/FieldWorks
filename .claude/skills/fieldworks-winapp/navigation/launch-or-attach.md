# Launch Or Attach To FieldWorks

Use this path when a task needs the live FieldWorks Language Explorer app.
Choose WinForms MCP first for fresh FieldWorks launches, and use WinApp MCP for
visible desktop diagnostics or fallback.

## Entry State

- Workspace is a Windows/x64 FieldWorks checkout.
- Prefer an existing debug build at `Output/Debug/FieldWorks.exe`.

## Preferred Steps: WinForms MCP UIA2

1. Launch debug FieldWorks with `winforms_launch_app`:
   - `path`: `<workspace>\\Output\\Debug\\FieldWorks.exe`
2. Keep the returned `pid`; pass it to later process-scoped calls when the MCP
   client exposes a process parameter.
3. Wait for a recognizable app window or menu with `winforms_wait_for_element`.
4. Inspect the tree with `winforms_get_element_tree` before interacting.
5. Use `winforms_click_menu_item`, `winforms_find_element`,
   `winforms_click_element`, `winforms_type_text`, `winforms_set_value`, and
   `winforms_select_item` for headless-safe interaction.
6. Capture screenshots with `winforms_take_screenshot`. Prefer passing the
   FieldWorks `pid` so the server can use the hidden-desktop window directly.

## Fallback Steps: WinApp MCP UIA3

1. If FieldWorks is already running, attach to the existing process with
   `mcp_winapp_attach_to_app` or `mcp_winapp_attach_to_pid`.
2. Otherwise launch debug FieldWorks with `mcp_winapp_launch_app`:
   - `exePath`: `<workspace>\\Output\\Debug\\FieldWorks.exe`
3. If `mcp_winapp_wait_for_input_idle` is not implemented or fails, continue
   with direct window discovery.
4. Use `mcp_winapp_list_windows` and `mcp_winapp_list_desktop_windows` to verify
   the app is visible.
5. Use `mcp_winapp_get_snapshot` to confirm FieldWorks has a main window and a
   recognizable UI tree.
6. If screenshots capture VS Code or another foreground app, press `ALT+ESCAPE`
   with `mcp_winapp_press_key_combo` until FieldWorks is first in desktop window
   order or a FieldWorks element has focus.

## Expected Signals

- WinForms MCP launch returns a process ID and can operate on a hidden desktop
  because `.vscode/mcp.json` sets `HEADLESS=true`.
- Main window title may initially appear as `The Window` in UI Automation.
- A loaded project window title may include a project name such as `Sena 3`.
- The side pane often contains `Lexicon`, `Texts & Words`, `Grammar`,
  `Notebook`, and `Lists` groups.

## Known Workarounds

- In WinForms MCP headless mode, avoid `winforms_send_keys`, drag/drop, and
  double-click. Use UIA pattern operations such as `winforms_type_text`,
  `winforms_set_value`, `winforms_select_item`, and single
  `winforms_click_element` calls.
- `mcp_winapp_wait_for_input_idle` may return `not implemented` for
  FieldWorks. Use snapshots and desktop-window order instead.
- `mcp_winapp_take_screenshot` captures the foreground surface. Bring
  FieldWorks forward before relying on screenshots.

## Exit

Close only the FieldWorks instance you launched for the task, using
`winforms_close_app` or `mcp_winapp_close_app`. Do not close a user's
pre-existing app instance unless the user asked for cleanup.
