# Project Properties (`FwProjPropertiesDlg`)

| | |
|---|---|
| **Legacy class** | `SIL.FieldWorks.FwCoreDlgs.FwProjPropertiesDlg` (`Src/FwCoreDlgs/FwProjPropertiesDlg.cs`) |
| **Area** | App-wide (project settings) |
| **Type** | dialog |
| **Primitive** | TABS |
| **State** | legacy |
| **Phase** | 1 |
| **Canonical reference** | tabs→OptionsDialog |
| **JIRA** | LT-XXXXX |

## What it is
The Project Properties dialog — edits project-level settings (name, description, writing systems, linked-files location, etc.).

## Notes / gotchas
- Views-coupled (references `IVwRootSite`/selection-based rendering for some fields).
- Multi-tab settings surface.

> Stub. Deepen using `Docs/migration/_TEMPLATE.md` (capture legacy PNGs via the `fieldworks-winapp` skill) when this ticket is picked up.
