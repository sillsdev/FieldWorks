# Semantic Domain Reference Launcher (`SemanticDomainReferenceLauncher`)

| | |
|---|---|
| **Legacy class** | `SIL.FieldWorks.Common.Framework.DetailControls.SemanticDomainReferenceLauncher` (`Src/Common/Controls/DetailControls/SemanticDomainReferenceLauncher.cs`) |
| **Area** | App-wide (these are shared controls) |
| **Type** | launcher |
| **Primitive** | owned-control |
| **State** | legacy |
| **Phase** | 1 |
| **Canonical reference** | FwReferenceVectorField (vector owned control) |
| **JIRA** | LT-XXXXX |

## What it is
The in-slice control for a sense's semantic domains; subclasses `PossibilityVectorReferenceLauncher` and launches the dedicated `SemanticDomainsChooser` (tree + search) rather than the generic list chooser.

## Notes / gotchas
- `internal` class; overrides chooser launch to open `SemanticDomainsChooser` (with its search/suggest panel).
- Multi-target edit against the semantic-domain possibility list — wrap writes in the LCM unit-of-work.

> Stub. Deepen using `Docs/migration/_TEMPLATE.md` (capture legacy PNGs via the `fieldworks-winapp` skill) when this ticket is picked up.
