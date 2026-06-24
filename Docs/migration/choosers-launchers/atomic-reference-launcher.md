# Atomic Reference Launcher (`AtomicReferenceLauncher`)

| | |
|---|---|
| **Legacy class** | `SIL.FieldWorks.Common.Framework.DetailControls.AtomicReferenceLauncher` (`Src/Common/Controls/DetailControls/AtomicReferenceLauncher.cs`) |
| **Area** | App-wide (these are shared controls) |
| **Type** | launcher |
| **Primitive** | owned-control |
| **State** | legacy |
| **Phase** | 1 |
| **Canonical reference** | FwOptionPicker (atomic owned control) |
| **JIRA** | LT-XXXXX |

## What it is
The in-slice control for an atomic (single-valued) object reference: shows the current target as an embedded view plus a launch button that opens a chooser to set/replace it.

## Notes / gotchas
- Owned control embedded in a slice; communicates size changes back to the embedding slice.
- Setting the reference is a model write — wrap in the LCM unit-of-work; preserve undo grouping.

> Stub. Deepen using `Docs/migration/_TEMPLATE.md` (capture legacy PNGs via the `fieldworks-winapp` skill) when this ticket is picked up.
