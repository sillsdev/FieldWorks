# Possibility Vector Reference Launcher (`PossibilityVectorReferenceLauncher`)

| | |
|---|---|
| **Legacy class** | `SIL.FieldWorks.Common.Framework.DetailControls.PossibilityVectorReferenceLauncher` (`Src/Common/Controls/DetailControls/PossibilityVectorReferenceLauncher.cs`) |
| **Area** | App-wide (these are shared controls) |
| **Type** | launcher |
| **Primitive** | owned-control |
| **State** | legacy |
| **Phase** | 1 |
| **Canonical reference** | FwReferenceVectorField (vector owned control) |
| **JIRA** | LT-XXXXX |

## What it is
Vector reference launcher specialized for possibility-list items (e.g. multiple categories/domains from a `CmPossibilityList`); subclasses `VectorReferenceLauncher` and implements `IVwNotifyChange`. Base class for `SemanticDomainReferenceLauncher`.

## Notes / gotchas
- Listens for model changes (`IVwNotifyChange`) to refresh the displayed list — unsubscribe on dispose.
- Multi-select against a hierarchical possibility list; chooser is the tree-based list chooser.

> Stub. Deepen using `Docs/migration/_TEMPLATE.md` (capture legacy PNGs via the `fieldworks-winapp` skill) when this ticket is picked up.
