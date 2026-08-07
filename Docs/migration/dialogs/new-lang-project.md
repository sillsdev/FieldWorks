# New Language Project (`FwNewLangProject`)

| | |
|---|---|
| **Legacy class** | `SIL.FieldWorks.FwCoreDlgs.FwNewLangProject` (`Src/FwCoreDlgs/FwNewLangProject.cs`) |
| **Area** | App-wide (project creation) |
| **Type** | dialog |
| **Primitive** | owned-control (wizard steps) |
| **State** | legacy |
| **Phase** | 1 |
| **Canonical reference** | owned-control form→InsertEntryDialog |
| **JIRA** | LT-XXXXX |

## What it is
The New Language Project dialog — collects project name and initial writing systems to create a new FLEx project.

## Notes / gotchas
- Hosts owned sub-controls: `FwNewProjectProjectNameControl`, `FwNewLangProjWritingSystemsControl`, `FwNewLangProjMoreWsControl`, `FwChooseAnthroListCtrl` (anthropology-list picker, backed by `FwChooseAnthroListModel`), and `WizardStep` (driven by `FwNewLangProjectModel` / `NewLangProjStep`). Fold these into this dialog's migration.
- Wizard-style multi-step flow.

> Stub. Deepen using `Docs/migration/_TEMPLATE.md` (capture legacy PNGs via the `fieldworks-winapp` skill) when this ticket is picked up.
