# Launch Or Attach To FieldWorks

Use this path when a task needs the live FieldWorks Language Explorer app.
Choose WinForms MCP first for fresh FieldWorks launches, and use WinApp MCP for
visible desktop diagnostics or fallback.

## Entry State

- Workspace is a Windows/x64 FieldWorks checkout.
- Prefer an existing debug build at `Output/Debug/FieldWorks.exe`.

## ALWAYS force Legacy (WinForms) UI mode first

This skill scrapes the **legacy WinForms** surface for truth screenshots, workflows, and behaviour. The
Avalonia (New) surface is captured by a SEPARATE headless skill — never through this MCP. The WinForms
UIA2 MCP can only see WinForms: if FieldWorks comes up in New (Avalonia) mode, `winforms_get_element_tree`
returns empty and `winforms_take_screenshot` is blank even though the process is healthy.

**Step 0 (mandatory, before every launch) — run BOTH:**
```powershell
.\.claude\skills\fieldworks-winapp\scripts\Set-FieldWorksLegacyMode.ps1
.\.claude\skills\fieldworks-winapp\scripts\Resolve-FieldWorksDevRegistry.ps1
```
- `Set-FieldWorksLegacyMode.ps1` forces `UIMode=Legacy` in libpalaso's settings store
  (`%LOCALAPPDATA%\SIL\SIL FieldWorks\*\user.config`).
- `Resolve-FieldWorksDevRegistry.ps1` ensures `HKCU\SOFTWARE\SIL\FieldWorks\9` `RootCodeDir`/`RootDataDir`
  point at THIS worktree's `DistFiles` (a mismatch leaves the main window unbuilt — blank). It auto-realigns
  when the other worktree is NOT active (no FieldWorks.exe running from it) and was NOT used in the last 24h
  (registry key write time). If it prints `RESULT=ASK_USER`, the other worktree may be active — **ask the
  user** before realigning, then re-run with `-Force` if they approve.

See the script headers and `../references/mcp-setup.md`.

## Preferred Steps: WinForms MCP UIA2

1. Launch debug FieldWorks with `winforms_launch_app`:
   - `path`: `<workspace>\\Output\\Debug\\FieldWorks.exe`
   - `arguments`: **`-db "<Project Name>"` ONLY** (e.g. `-db "Sena 3"`). The valid options are
     `-db / -locale / -restore / -include / -help` — there is **NO `-app` option**. Passing an unknown arg
     (e.g. `-app Flex`) pops a modal **usage dialog** that blocks startup; if that modal goes unnoticed the
     app looks "stuck with a blank window and an empty UIA tree" (loaded ~450 MB,
     responding, but no main window). If you see that signature, the launch arguments are wrong.
   - Project names with a space need real quoting. `winforms_launch_app` passes `arguments` as one string;
     if the project does not open, launch via `[System.Diagnostics.ProcessStartInfo]` with
     `ArgumentList.Add('-db'); ArgumentList.Add('Sena 3')` (quotes each element correctly) and then
     `winforms_attach_to_process`.
2. Keep the returned `pid`; pass it to later process-scoped calls when the MCP
   client exposes a process parameter.
3. Wait for a recognizable app window or menu with `winforms_wait_for_element`.
4. Inspect the tree with `winforms_get_element_tree` before interacting.
5. Use `winforms_click_menu_item`, `winforms_find_element`,
   `winforms_click_element`, `winforms_type_text`, `winforms_set_value`, and
   `winforms_select_item` for element-targeted interaction.
6. Capture screenshots with `winforms_take_screenshot`. Prefer passing the
   FieldWorks `pid` so the server captures the right window via `PrintWindow`.

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

- WinForms MCP launch returns a process ID. `HEADLESS=false` (visible desktop) is
  required — FieldWorks' native Views rendering does not complete on a hidden
  desktop; see `../references/mcp-setup.md`.
- Main window title may initially appear as `The Window` in UI Automation.
- A loaded project window title may include a project name such as `Sena 3`.
- The side pane often contains `Lexicon`, `Texts & Words`, `Grammar`,
  `Notebook`, and `Lists` groups.

## Known Workarounds

- Prefer UIA pattern operations (`winforms_type_text`, `winforms_set_value`,
  `winforms_select_item`, single `winforms_click_element` calls) over
  `winforms_send_keys`, drag/drop, and double-click — they are more reliable.
  (The blanket bans on input simulation applied to the abandoned hidden-desktop
  mode; see `../references/headless-rendering.md`.)
- `mcp_winapp_wait_for_input_idle` may return `not implemented` for
  FieldWorks. Use snapshots and desktop-window order instead.
- `mcp_winapp_take_screenshot` captures the foreground surface. Bring
  FieldWorks forward before relying on screenshots.
- **Blank window + empty UIA tree, process healthy and responding:** first cause
  is New (Avalonia) UI mode — run `scripts/Set-FieldWorksLegacyMode.ps1` and
  relaunch (Step 0 above). If it is STILL blank in Legacy mode, check whether the
  process memory is *flat* (via `tasklist`): flat ≈ loaded-but-stuck (a modal that
  is not enumerated, or the main window failed to build), climbing ≈ still loading
  (a Debug build + Sena 3 migration can take minutes — keep waiting). Also verify
  the dev registry points the running exe at its OWN DistFiles:
  `HKCU\SOFTWARE\SIL\FieldWorks\9` `RootCodeDir`/`RootDataDir` should match the
  worktree of the launched `Output/Debug/FieldWorks.exe`; a mismatch (a different
  worktree's DistFiles) can leave the main window unbuilt. Aligning it is a dev
  registry change — confirm with the user before editing.

## Exit

Close only the FieldWorks instance you launched for the task, using
`winforms_close_app` or `mcp_winapp_close_app`. Do not close a user's
pre-existing app instance unless the user asked for cleanup.
