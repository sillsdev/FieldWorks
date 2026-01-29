# Research Findings: FieldWorks 64-bit only + Registration-free COM

Created: 2025-11-06 | Branch: 001-64bit-regfree-com

## Decisions and Rationale

### D1. Target frameworks for core apps and tests
- Decision: Maintain current target frameworks used by core apps/tests; do not change TFM in this feature. Document exact TFM during tasks execution.
- Rationale: Scope focuses on bitness and COM activation, not managed runtime upgrades.
- Alternatives considered: Upgrade to newer .NET TFM now (Rejected: increases blast radius; not required for reg-free COM).

### D2. Enumerating native COM DLLs for manifests
- Decision: Start with broad include pattern (all native DLLs next to EXE) and let the RegFree task filter to COM-eligible DLLs; refine if needed.
- Rationale: Minimizes risk of missing servers; tooling already ignores non-COM DLLs.
- Alternatives: Curated static list per EXE (Rejected: drifts easily; higher maintenance).

### D3. Installer impact boundaries
- Decision: Remove/disable COM registration in installer flows for core apps; ensure generated manifests are packaged intact. No other installer modernization in this phase.
- Rationale: Aligns with spec non-goals; limits scope.
- Alternatives: Broader WiX modernization (Rejected: out of scope).

### D4. AnyCPU assemblies in x64 hosts
- Decision: Enforce x64 for host processes; allow AnyCPU libraries where they do not bridge to native/COM. Audit COM/native-bridging assemblies to target x64.
- Rationale: Avoid WOW32 confusion; keep effort minimal for pure managed libraries.
- Alternatives: Force x64 for all assemblies (Rejected: unnecessary churn).

### D5. Test strategy for COM activation
- Decision: Use a shared manifest-enabled host for COM-activating tests; avoid per-test EXE manifests in Phase 1.
- Rationale: Centralized maintenance; consistent environment for tests.
- Alternatives: Per-test EXE manifests (Rejected: higher maintenance for limited benefit).

### D6. CI enforcement
- Decision: Ensure CI builds with /p:Platform=x64 for all solutions; add checks to fail if Win32 artifacts are produced.
- Rationale: Guarantees policy in automated environments.
- Alternatives: Advisory only (Rejected: risk of regression).

## Open Questions Resolved

- TFM changes? → No changes in this feature; document existing.
- Exact list of COM servers? → Start broad; confirm via manifest inspection and smoke tests.
- Installer registration steps? → Remove for core apps; keep manifests.

## Validation Methods

- Build x64 only; verify generated manifests contain expected CLSIDs/IIDs (spot-check known GUIDs).
- Launch core apps on clean VM with no registrations; ensure zero class-not-registered errors.
- Run COM-activating tests under shared host; verify pass rates without admin rights.
