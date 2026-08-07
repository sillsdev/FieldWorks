# Possibility Atomic Reference Launcher (`PossibilityAtomicReferenceLauncher`)

| | |
|---|---|
| **Legacy class** | `SIL.FieldWorks.Common.Framework.DetailControls.PossibilityAtomicReferenceLauncher` (`Src/Common/Controls/DetailControls/PossibilityAtomicReferenceLauncher.cs`) |
| **Area** | App-wide (these are shared controls) |
| **Type** | launcher |
| **Primitive** | owned-control |
| **State** | legacy |
| **Phase** | 1 |
| **Canonical reference** | FwOptionPicker (atomic owned control) |
| **JIRA** | LT-XXXXX |

## What it is
Atomic reference launcher specialized for a single possibility-list item (e.g. a single category from a `CmPossibilityList`); subclasses `AtomicReferenceLauncher` and implements `IVwNotifyChange`.

## Notes / gotchas
- Listens for model changes (`IVwNotifyChange`) to refresh the displayed value — unsubscribe on dispose to avoid leaks.
- Candidate list is a hierarchical possibility list; chooser is the tree-based list chooser.

> Stub. Deepen using `Docs/migration/_TEMPLATE.md` (capture legacy PNGs via the `fieldworks-winapp` skill) when this ticket is picked up.
