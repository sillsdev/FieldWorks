# Navigate To Writing System Font Options

Use this path to inspect or document Writing System Properties > Font behavior,
including Graphite and OpenType Font Features.

## Entry State

- FieldWorks is launched and a project is loaded.
- If screenshots are needed, FieldWorks is foregrounded.

## Steps

1. With WinForms MCP, restore the main FieldWorks window if it is minimized or
   offscreen.
2. Open `Format` > `Set up Vernacular Writing Systems...` or
   `Set up Analysis Writing Systems...`. If WinForms MCP cannot see the dynamic
   submenu command, use the visible-desktop keyboard fallback
   `%o{DOWN}{DOWN}{DOWN}{ENTER}` after the main window is focused.
3. In `Writing System Properties`, inspect the UIA2 tree and click the `Font`
   tab item under `_tabControl`.
4. Capture or inspect the target state.
5. If a temporary evidence font is needed, select it in `m_defaultFontComboBox`
  and cancel the dialog afterward.

## Stable Elements Observed

- Dialog: `FwWritingSystemSetupDlg`
- Writing-system list: `_writingSystemList`
- Tab control: `_tabControl`
- Font tab: `_fontTab`
- Default font control: `_defaultFontControl`
- Default font combo: `m_defaultFontComboBox`
- Font options group: `m_graphiteGroupBox`
- Graphite checkbox: `m_enableGraphiteCheckBox`
- Font Features button: `m_defaultFontFeaturesButton`
- OK button: `_okBtn`
- Cancel button: `_cancelBtn`

## Expected Signals

- The group label for LT-22324 fixed builds is `Font Options`.
- `Enable Graphite` controls Graphite renderer selection only.
- `Font Features` can remain enabled while `Enable Graphite` is unchecked when
  the selected font exposes configurable font features.

## Known Workarounds

- For WinForms MCP, `winforms_raise_event` with `eventName: "invoke"` is often
  more reliable than a click for dialog buttons and popup-opening buttons.
- For WinApp MCP fallback, `mcp_winapp_invoke_element` is often more reliable
  than a click.
- WinForms MCP global `winforms_find_elements` may miss modal dialog children.
  Use `winforms_get_element_tree` on the process, then reuse cached element IDs
  from the returned dialog tree.
- The FieldWorks font combo is owner-drawn. If `winforms_select_item` cannot
  select an offscreen font, open the combo and use keyboard navigation from a
  verified sorted font-family list. For the 2026-04-30 LT-22324 evidence run,
  `Charis SIL` was index 36 from `Alef` in the current Windows/.NET font list.
- Invoking `m_defaultFontFeaturesButton` through WinForms MCP can expose a
  top-level `Menu` window. Re-query the process tree after invoking; menu items
  may become the UIA2 roots while the menu is open.

## Exit

Use `Cancel` for evidence-only sessions. Use `OK` only when the task requires a
project data change.
