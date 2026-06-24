# Vector Reference Launcher (`VectorReferenceLauncher`)

| | |
|---|---|
| **Legacy class** | `SIL.FieldWorks.Common.Framework.DetailControls.VectorReferenceLauncher` (`Src/Common/Controls/DetailControls/VectorReferenceLauncher.cs`) |
| **Area** | App-wide (these are shared controls) |
| **Type** | launcher |
| **Primitive** | owned-control |
| **State** | legacy |
| **Phase** | 1 |
| **Canonical reference** | FwReferenceVectorField (vector owned control) |
| **JIRA** | LT-XXXXX |

## What it is
The in-slice control for a vector (multi-valued) object reference: shows the current list of targets as an embedded view plus a launch button that opens a chooser to add/remove/reorder items.

## Notes / gotchas
- Owned control embedded in a slice; communicates size changes back to the embedding slice.
- Multi-target edits (add/remove/reorder) are model writes — wrap in the LCM unit-of-work; preserve undo grouping.

> Stub. Deepen using `Docs/migration/_TEMPLATE.md` (capture legacy PNGs via the `fieldworks-winapp` skill) when this ticket is picked up.
