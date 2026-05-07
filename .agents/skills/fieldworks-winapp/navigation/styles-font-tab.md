# Navigate To Styles Font Tab

Use this path to inspect or document the shared Font Features control in the
Styles dialog.

## Entry State

- FieldWorks is launched and a project is loaded.
- If screenshots are needed, FieldWorks is foregrounded.

## Steps

1. Click the `Format` menu item.
2. Click `Styles...`.
3. In `FwStylesDlg`, click the `Font` tab.
4. Select the target style in `m_lstStyles` if the task requires a specific
   style. For general evidence, `Normal` is usually sufficient.
5. Capture or inspect the Font Features state.

## Stable Elements Observed

- Dialog: `FwStylesDlg`
- Styles list: `m_lstStyles`
- List filter combo: `m_cboTypes`
- Tab control: `m_tabControl`
- Font tab pane: `m_tbFont`
- Font attributes pane: `m_FontAttributes`
- Writing systems list: `m_lstWritingSystems`
- Font combo: `m_cboFontNames`
- Font Features button: `m_btnFontFeatures`
- OK button: `m_btnOk`
- Cancel button: `m_btnCancel`

## Expected Signals

- The Font tab shows `Font features` in the Attributes area.
- The writing-system list includes `<default settings>` and project writing
  systems such as `English`, `Portuguese`, `Sena`, and `Sena (Phonetic)` for
  the Sena 3 sample project.

## Known Workarounds

- The `Styles...` menu item appears only after the `Format` menu is open; if a
  first click does not expose submenu items, click `Format` again and re-query
  menu items.
- Use `mcp_winapp_invoke_element` for `m_btnCancel` when a normal click does not
  close the dialog.

## Exit

Use `Cancel` for evidence-only sessions. Use `OK` only when the task requires a
style change.
