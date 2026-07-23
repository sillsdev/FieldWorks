# Phonological Environment Reference Launcher (`PhoneEnvReferenceLauncher`)

| | |
|---|---|
| **Legacy class** | `SIL.FieldWorks.Common.Framework.DetailControls.PhoneEnvReferenceLauncher` (`Src/Common/Controls/DetailControls/PhoneEnvReferenceLauncher.cs`) |
| **Area** | App-wide (these are shared controls) |
| **Type** | launcher |
| **Primitive** | owned-control |
| **State** | legacy |
| **Phase** | 1 |
| **Canonical reference** | FwReferenceVectorField (vector owned control) |
| **JIRA** | LT-XXXXX |

## What it is
The in-slice control for editing the set of phonological environments on an allomorph; shows the environment strings and a launch button, subclasses `ReferenceLauncher`.

## Notes / gotchas
- Environment strings have their own parse/validation syntax (slash, underscore, brackets) — porting must preserve environment validation and error display, not just plain text editing.
- Communicates size changes back to the embedding slice.

> Stub. Deepen using `Docs/migration/_TEMPLATE.md` (capture legacy PNGs via the `fieldworks-winapp` skill) when this ticket is picked up.
