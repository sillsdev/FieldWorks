# Implementation Plan: Convergence Path – PrivateAssets on Test Packages

**Branch**: `spec/005-convergence-private-assets` | **Date**: 2025-11-14 | **Spec**: [specs/005-convergence-private-assets/spec.md](specs/005-convergence-private-assets/spec.md)
**Input**: Feature specification from `specs/005-convergence-private-assets/spec.md`

## Summary

Standardize `PrivateAssets="All"` on every `SIL.LCModel.*.Tests` PackageReference that ships reusable test utilities so those transitive test dependencies never leak into consumer projects. Research identified **12** managed test projects referencing the three helper packages (`SIL.LCModel.Core.Tests`, `SIL.LCModel.Tests`, `SIL.LCModel.Utils.Tests`); the remaining ~34 test projects do not reference these packages and are intentionally left untouched in this convergence. We will lean on the Convergence python automation (`convergence.py private-assets audit|convert|validate`) to audit the full set, convert only the approved helper packages, and validate via MSBuild + NU1102 scans. All edits run inside the fw-agent container to respect FieldWorks build prerequisites.

**User Story Mapping**
- **US1 (P1)** — LCM helper packages remain private: covers the audit+conversion loop confined to the three `SIL.LCModel.*.Tests` packages.
- **US2 (P2)** — Validation + documentation guardrail: captures validation runs, NU1102 scans, and quickstart updates so the workflow can be repeated.

## Technical Context

**Language/Version**: Python 3.11 scripts (Convergence framework) plus MSBuild-driven C# project files
**Primary Dependencies**: `convergence.py` base classes, `xml.etree.ElementTree`, MSBuild traversal (`FieldWorks.proj`), NuGet packages `SIL.LCModel.Core.Tests|Tests|Utils.Tests`
**Storage**: N/A (reads/writes `.csproj` files in-place)
**Testing**: `python convergence.py private-assets validate`, `msbuild FieldWorks.sln /m /p:Configuration=Debug`, targeted NU1102 log scan
**Target Platform**: Windows fw-agent containers (FieldWorks worktree) running VS/MSBuild toolchain
**Project Type**: Repository automation plus `.csproj` normalization
**Performance Goals**: Audit + conversion complete in <5 minutes for 46 projects; `convergence.py` sub-commands finish in <1 minute each on dev hardware
**Constraints**: Do not touch non-LCM package references; preserve existing formatting/order; only edit via scripted conversions; run in containerized agent to avoid host registry dependencies
**Scale/Scope**: 46 managed test projects evaluated; 12 projects referencing the three `SIL.LCModel.*.Tests` packages are in scope for edits

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

- **Data integrity**: No runtime/user data touched; only `.csproj` metadata changes. Migration plan therefore limited to git rollback guidance ✅
- **Test evidence**: `convergence.py ... validate` plus `msbuild FieldWorks.sln /m /p:Configuration=Debug` act as regression gates; failure criteria documented ✅
- **I18n/script correctness**: Not applicable; no UI/text-rendering paths touched ✅
- **Licensing**: No new dependencies introduced; merely annotating existing NuGet references ✅
- **Stability/performance**: Changes reduce consumer dependency surface; risks mitigated by audit + validation scripts ✅

## Project Structure

### Documentation (this feature)

```text
specs/005-convergence-private-assets/
├── spec.md
├── plan.md
├── research.md          # Phase 0 output
├── data-model.md        # Phase 1 output
├── quickstart.md        # Phase 1 output
└── contracts/
    └── private-assets.openapi.yaml
```

### Source Code (repository root)

```text
Src/
├── xWorks/xWorksTests/                         # references SIL.LCModel.*.Tests
├── XCore/xCoreTests/
├── XCore/xCoreInterfaces/xCoreInterfacesTests/
├── XCore/SilSidePane/SilSidePaneTests/
├── UnicodeCharEditor/UnicodeCharEditorTests/
├── Utilities/XMLUtils/XMLUtilsTests/
├── Utilities/MessageBoxExLib/MessageBoxExLibTests/
├── ParatextImport/ParatextImportTests/
├── LexText/LexTextControls/LexTextControlsTests/
├── LexText/Discourse/DiscourseTests/
├── LexText/Morphology/MorphologyEditorDllTests/
└── ... (see research.md for the full enumerated project list)
```

**Structure Decision**: Treat this as a mono-repo automation effort touching only those `Src/**/Tests` projects that reference the helper packages listed above. Each affected `.csproj` remains in-place; automation iterates over the enumerated directories above, while all other test projects remain untouched to honor the clarified scope.

## Complexity Tracking

No Constitution violations outstanding; table not required.

## Phase 0 – Research Outline

1. Capture authoritative list of `SIL.LCModel.*.Tests` packages and confirm they are the only scope needing `PrivateAssets`.
2. Document the audit/convert/validate flow plus log artifacts (CSV + console output) to guarantee deterministic runs.
3. Record msbuild + NU1102 validation expectations and rollback strategy.

See `research.md` for decisions, rationale, and alternatives.

## Phase 1 – Design & Contracts

1. `data-model.md`: Describe `TestProject`, `PackageReference`, and `AuditFinding` entities plus state transitions (Audit → Convert → Validate).
2. `contracts/private-assets.openapi.yaml`: Model the three CLI operations as service endpoints (audit/convert/validate) for clarity when scripting.
3. `quickstart.md`: Provide copy/paste commands for audit, convert, validate, including container reminders and verification steps.

## Phase 2 – Implementation Outline (Execution Stops Here)

1. Run `python convergence.py private-assets audit` to regenerate `private_assets_audit.csv` and inspect for only `SIL.LCModel.*.Tests` rows.
2. Execute `python convergence.py private-assets convert --decisions private_assets_decisions.csv` limited to approved rows; review diffs locally.
3. Validate via `python convergence.py private-assets validate` followed by `msbuild FieldWorks.sln /m /p:Configuration=Debug` inside `fw-agent-3`.
4. If validation passes, proceed to `/speckit.tasks` for execution task breakdown.
