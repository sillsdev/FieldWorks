# App lifetime / startup (`FieldWorks.cs`)

| | |
|---|---|
| **Key files** | `Src/Common/FieldWorks/FieldWorks.cs` (+ `Src/Common/Framework/FwApp.cs`, `IFieldWorksManager.cs`) |
| **Area** | Shell |
| **Type** | shell |
| **Primitive** | n/a |
| **State** | legacy |
| **Phase** | 2 |
| **Census stage** | 11a |
| **JIRA** | LT-XXXXX |

## What it is
Process entry point and application lifetime manager: single-instance/remoting bootstrap, project open/close, app-to-app handoff, and shutdown.

## Notes / gotchas
- Reimplement as the cross-platform host bootstrap; today it is WinForms `Application.Run`-centric.
- Registration-free COM constraint — startup activates native Views/COM via the manifest; do not register globally.
- Coordinates with `FwApp`/`IFieldWorksManager`; deleted at end of coexistence once the net10 shell owns lifetime.

> Stub. Phase-2 (net10/shell/cross-platform). Deepen when the Phase-2 stage that owns it is scheduled.
