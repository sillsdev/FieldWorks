# Ghost Reference Vector Launcher (`GhostReferenceVectorLauncher`)

| | |
|---|---|
| **Legacy class** | `SIL.FieldWorks.Common.Framework.DetailControls.GhostReferenceVectorLauncher` (`Src/Common/Controls/DetailControls/GhostReferenceVectorSlice.cs`) |
| **Area** | App-wide (these are shared controls) |
| **Type** | launcher |
| **Primitive** | owned-control |
| **State** | legacy |
| **Phase** | 1 |
| **Canonical reference** | FwReferenceVectorField (vector owned control) |
| **JIRA** | LT-XXXXX |

## What it is
Vector reference launcher used where a reference-vector slice would appear but the owning object does not yet exist (e.g. the Info tab of Texts/Words on a ghost Notebook record); subclasses `ButtonLauncher`. Inner class of `GhostReferenceVectorSlice`.

## Notes / gotchas
- "Ghost" semantics: the owning object is NOT created until the user runs the chooser and clicks OK — porting must defer object creation to the OK path (data-loss/empty-object risk if created early).
- Candidate list comes from `ReferenceTargetServices.RnGenericRecReferenceTargetOwner`; currently used only for ghost Notebook-record properties (YAGNI note in source about configurable candidates).

> Stub. Deepen using `Docs/migration/_TEMPLATE.md` (capture legacy PNGs via the `fieldworks-winapp` skill) when this ticket is picked up.
