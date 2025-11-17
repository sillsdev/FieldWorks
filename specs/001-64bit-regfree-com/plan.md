# Implementation Plan: FieldWorks 64-bit only + Registration-free COM

**Branch**: `001-64bit-regfree-com` | **Date**: 2025-11-06 | **Spec**: specs/001-64bit-regfree-com/spec.md
**Input**: Feature specification from `/specs/001-64bit-regfree-com/spec.md`

## Summary

Migrate FieldWorks to 64‑bit only and enable registration‑free COM activation. Phase 1 now focuses on the unified FieldWorks.exe launcher (the legacy LexText.exe stub was removed and its UI now lives inside FieldWorks.exe). Technical approach: enforce x64 for host processes (managed and native), extend the existing RegFree build task to generate per‑EXE registration‑free manifests, verify native COM DLL co-location, and run COM‑activating tests under a shared manifest-enabled host. CI builds x64 only and avoids any COM registration.

## Technical Context

**Language/Version**: C# (.NET Framework 4.8) and C++/C++‑CLI (current MSVC toolset)
**Primary Dependencies**: MSBuild, existing SIL FieldWorks build tasks (RegFree), WiX 3.11.x (existing installer toolchain)
**Storage**: N/A (no data schema changes)
**Testing**: Visual Studio Test / NUnit harness with the shared manifest-enabled test host introduced in Phase 6
**Target Platform**: Windows x64 (Windows 10/11)
**Project Type**: Windows desktop suite (multiple EXEs and native DLLs)
**Performance Goals**: No regressions in startup or COM activation time; COM activation succeeds 100% on clean machines
**Constraints**: No registry writes or admin requirements for dev/CI/test; hosts MUST be x64; native VCXPROJ and C++/CLI projects must drop Win32 configurations; libraries may remain AnyCPU where safe
**Scale/Scope**: Phase 1 = core apps only; tools/test EXEs deferred except shared manifest host

Open unknowns resolved in research.md:
- Target frameworks for core apps and tests remain on .NET Framework 4.8; no TFM change is required (D1).
- Native COM DLL coverage relies on the broad include pattern filtered by the RegFree task; manifests will be inspected to confirm coverage (D2).
- Installer updates are limited to removing COM registration steps and packaging the generated manifests intact (D3).

## Constitution Check

Gate assessment before Phase 0:
- Data integrity: No schema/data changes — PASS (no migration required)
- Test evidence: Affects installers/runtime activation → MUST include automated checks or scripted validation (smoke tests, manifest content checks) — REQUIRED
- I18n/script correctness: Rendering engines loaded via COM; ensure smoke tests include complex scripts — REQUIRED
- Licensing: No new third‑party libs anticipated; verify any tooling updates — PASS (verify in PR)
- Stability/performance: Risk of activation failures if manifests incomplete; add broad include + verification — REQUIRED

Proceed to Phase 0 with required validations planned.

Post‑design re‑check (after Phase 1 artifacts added):
- Data integrity: Still N/A — PASS
- Test evidence: Covered via smoke tests and manifest validation steps — PASS (to be enforced in tasks)
- I18n/script correctness: Included in smoke tests — PASS (verify in tasks)
- Licensing: No new deps introduced — PASS
- Stability/performance: Risks mitigated by broad include + verification — PASS

## Project Structure

### Documentation (this feature)

```text
specs/001-64bit-regfree-com/
├── plan.md              # This file
├── research.md          # Phase 0 output (this command will create)
├── data-model.md        # Phase 1 output
├── quickstart.md        # Phase 1 output
├── contracts/           # Phase 1 output
└── tasks.md             # Phase 2 output (/speckit.tasks)
```

### Source Code (repository root)

```text
Build/
└── RegFree.targets               # Extend existing target to generate reg‑free manifests

Src/
├── Common/FieldWorks/FieldWorks.csproj         # Imports RegFree.targets (current launcher)
└── Common/ViewsInterfaces/                     # COM interop definitions (reference only)

Legacy status: the former `Src/LexText/LexTextExe/` host has been decommissioned. All
LexText UX now runs inside `FieldWorks.exe`, so reg-free manifest coverage funnels
through that executable.

FLExInstaller/                                  # Validate installer changes (no registration)
```

**Structure Decision**: Extend the existing `Build/RegFree.targets` logic and import it in core EXE projects; avoid per‑project custom scripts. Keep native COM DLLs next to EXEs to simplify manifest `<file>` references, and add verification tasks to ensure packaging preserves that layout.

## Complexity Tracking

No constitution violations anticipated; no justification table needed.
