# Morph Type Atomic Launcher (`MorphTypeAtomicLauncher`)

| | |
|---|---|
| **Legacy class** | `SIL.FieldWorks.Common.Framework.DetailControls.MorphTypeAtomicLauncher` (`Src/Common/Controls/DetailControls/MorphTypeAtomicLauncher.cs`) |
| **Area** | App-wide (these are shared controls) |
| **Type** | launcher |
| **Primitive** | owned-control |
| **State** | legacy |
| **Phase** | 1 |
| **Canonical reference** | FwOptionPicker (atomic owned control) |
| **JIRA** | LT-XXXXX |

## What it is
Atomic reference launcher for the morph type of a lexeme/allomorph; subclasses `PossibilityAtomicReferenceLauncher` and opens the `MorphTypeChooser` (with its "Show all types" toggle).

## Notes / gotchas
- Launches `MorphTypeChooser` rather than the generic list chooser — carry over the "show all types" behaviour.
- Changing morph type can change allomorph class/behaviour in the model; verify the write path and any side effects.

> Stub. Deepen using `Docs/migration/_TEMPLATE.md` (capture legacy PNGs via the `fieldworks-winapp` skill) when this ticket is picked up.
