# Dictionary Configuration (`DictionaryConfigurationDlg`)

| | |
|---|---|
| **Legacy class** | `SIL.FieldWorks.XWorks.DictionaryConfigurationDlg` (`Src/xWorks/DictionaryConfigurationDlg.cs`) |
| **Area** | Dictionary-config |
| **Type** | dialog |
| **Primitive** | TREE |
| **State** | legacy |
| **Phase** | 1 |
| **Canonical reference** | ChooserDialog (tree + detail), but this is a large bespoke screen — see gotchas |
| **JIRA** | LT-XXXXX |

## What it is
The main Dictionary Configuration editor: a configuration tree (`DictionaryConfigurationTreeControl`) on the left, a per-node details pane (`DictionaryDetailsView` family) on the right, and a live HTML preview, with OK/Apply/Cancel and a "Manage Views" entry point. Implements `IDictionaryConfigurationView` (MVC view, driven by `DictionaryConfigurationController`).

## Notes / gotchas
- LARGE, COMPLEX screen — not a simple plain-form. Composes three regions: tree control, swappable details panels (`DetailsView`, `ListOptionsView`, `PictureOptionsView`, `GroupingOptionsView`, `*OverPanel`), and a preview.
- HARD dependency on **GeckoWebBrowser**: the preview (`m_preview.NativeBrowser`) is asserted to be a `GeckoWebBrowser`; node highlighting walks `GeckoElement`s in the rendered HTML. Migration must reproduce or replace the HTML preview + element-highlight feature.
- Details panels are owned WinForms UserControls hosted in this dialog (covered by directory scope, not separately stubbed).
- MVC: behaviour lives in `DictionaryConfigurationController`; the dialog is mostly the view surface.

> Stub. Deepen using `Docs/migration/_TEMPLATE.md` (capture legacy PNGs via the `fieldworks-winapp` skill) when this ticket is picked up.
