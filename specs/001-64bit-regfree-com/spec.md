# Feature Specification: FieldWorks 64-bit only + Registration-free COM

**Feature Branch**: `001-64bit-regfree-com`
**Created**: 2025-11-06
**Status**: Draft
**Input**: User description: "FieldWorks 64-bit only + Registration-free COM migration plan"

## User Scenarios & Testing *(mandatory)*

<!--
  IMPORTANT: User stories should be PRIORITIZED as user journeys ordered by importance.
  Each user story/journey must be INDEPENDENTLY TESTABLE - meaning if you implement just ONE of them,
  you should still have a viable MVP (Minimum Viable Product) that delivers value.

  Assign priorities (P1, P2, P3, etc.) to each story, where P1 is the most critical.
  Think of each story as a standalone slice of functionality that can be:
  - Developed independently
  - Tested independently
  - Deployed independently
  - Demonstrated to users independently
-->

### User Story 1 - Build and run without COM registration (Priority: P1)

Developers can build and run FieldWorks on a clean Windows machine without administrator privileges or COM registration; all COM activation works via application manifests.

**Why this priority**: Eliminates a major setup blocker (admin rights, regsvr32), reduces friction for contributors, and de-risks environments where registry writes are restricted.

**Independent Test**: On a clean dev VM with no FieldWorks COM registrations, build Debug|x64, copy outputs if needed, and launch the primary executable(s); verify no class‑not‑registered errors occur.

**Acceptance Scenarios**:

1. Given a machine with no FieldWorks COM registrations, When building and launching `FieldWorks.exe` (Debug|x64), Then the app starts without any COM class‑not‑registered errors.
2. Given a machine with no FieldWorks COM registrations, When running a developer tool or test executable that activates COM, Then COM objects instantiate successfully without prior registration steps.

---

### User Story 2 - Ship and run as 64‑bit only (Priority: P2)

End users and QA receive x64‑only builds; installation and launch succeed without COM registration steps.

**Why this priority**: Simplifies packaging/runtimes, removes WOW32 confusion, and aligns with modern Windows environments.

**Independent Test**: Build Release|x64 artifacts, install or stage them on a clean test machine, and launch; confirm no COM registration is required and the app runs normally.

**Acceptance Scenarios**:

1. Given published x64 artifacts, When installing or staging on a clean machine, Then the application launches and performs primary flows without COM registration.

---

### User Story 3 - CI builds are x64‑only, no registry writes (Priority: P3)

CI produces x64‑only artifacts and does not perform COM registration; tests and smoke checks pass with registration‑free manifests.

**Why this priority**: Ensures reproducibility and parity with developer machines; prevents hidden dependencies on machine state.

**Independent Test**: Inspect CI logs for absence of registration calls and presence of generated manifests; run a subset of tests/tools that create COM objects to validate activation.

**Acceptance Scenarios**:

1. Given CI build pipelines, When building the solution, Then no x86 configurations are built and no COM registration commands appear in logs.
2. Given CI test executions that create COM objects, When they run, Then no admin privileges or COM registrations are needed for success.

---

[Add more user stories as needed, each with an assigned priority]

### Edge Cases

<!--
  ACTION REQUIRED: The content in this section represents placeholders.
  Fill them out with the right edge cases.
-->

- Missing or unlisted native COM DLL: COM activation fails with class‑not‑registered; manifests must include `<file>` entries for all required servers.
- Proxy/stub availability: Interfaces requiring external proxies must be covered; otherwise, marshaling failures occur across apartment boundaries.
- AnyCPU libraries loaded by WinExe: If host is x64 and library assumes x86, load fails; hosts must be explicitly x64.
- Tests that indirectly activate COM: Test hosts without manifests will fail; ensure manifest coverage or host under a manifest‑enabled executable.

## Requirements *(mandatory)*

<!--
  ACTION REQUIRED: The content in this section represents placeholders.
  Fill them out with the right functional requirements.
-->

### Functional Requirements

- **FR-001**: Builds MUST be 64‑bit only across managed and native projects; Win32/x86 configurations are removed from solution and CI.
- **FR-002**: Each executable that activates COM MUST produce a registration‑free COM manifest at build time and ship it alongside the executable.
- **FR-003**: Developer and CI builds MUST NOT call `regsvr32` or invoke `DllRegisterServer`; COM activation MUST rely solely on manifests.
- **FR-004**: The application MUST launch and complete primary flows on a machine with no FieldWorks COM registrations present.
- **FR-005**: Build outputs MUST contain no x86 artifacts; CI MUST enforce `/p:Platform=x64` (or equivalent) for all builds.
- **FR-006**: Test executables that create COM objects MUST succeed without admin rights or registry modifications by running under a shared manifest‑enabled host.
- **FR-007**: Native COM DLLs required at runtime MUST be co‑located with the executable (or referenced via manifest `codebase` entries) so manifests can resolve them.
- **FR-008**: Manifests MUST include entries for all required CLSIDs, IIDs, and type libraries for COM servers used by the executable’s primary flows.

- **FR-009**: Phase 1 scope is limited to the unified `FieldWorks.exe` launcher (the legacy `LexText.exe` stub has been removed). Other user‑facing tools and test executables are deferred to a later phase.
- **FR-010**: Enforce x64 for host processes only. Libraries may remain AnyCPU where compatible and not performing native/COM interop that requires explicit x64; components that bridge to native/COM MUST target x64.
- **FR-011**: Provide and adopt a shared manifest‑enabled test host to satisfy FR‑006; individual test executables do not need bespoke manifests in Phase 1.

### Key Entities *(include if feature involves data)*

- **Executable**: A user‑facing or test host process that may activate COM; must ship with a registration‑free manifest.
- **Native COM DLL**: A native library exporting COM classes and type libraries; must be discoverable per the executable’s manifest at runtime.
- **Registration‑free Manifest**: XML assembly manifest declaring `<file>`, `<comClass>`, `<typelib>`, and related entries enabling COM activation without registry.

### Dependencies & Assumptions

- Windows target environments are x64 (Windows 10/11); 32‑bit OS support is out of scope.
- Existing COM interfaces and marshaling behavior remain unchanged; this is a packaging/build/runtime activation change, not an API change.
- Installer changes are limited to removing COM registration steps and ensuring manifests are preserved with executables.
- Native COM DLLs and dependent native libraries remain locatable at runtime (same‑directory layout or equivalent as today).

## Success Criteria *(mandatory)*

<!--
  ACTION REQUIRED: Define measurable success criteria.
  These must be technology-agnostic and measurable.
-->

### Measurable Outcomes

- **SC-001**: From a clean Windows machine (no FieldWorks COM registrations), launching the primary executable(s) succeeds with zero COM class‑not‑registered errors (100% of P1 scenarios pass).
- **SC-002**: 0 occurrences of COM registration commands (e.g., `regsvr32`, `DllRegisterServer`) in developer and CI build logs for this feature’s scope.
- **SC-003**: 100% of produced artifacts for targeted solutions are x64; no x86/Win32 binaries are present in the final output directories.
- **SC-004**: For executables that activate COM, generated manifests include entries for all required CLSIDs/IIDs (spot‑check against known GUIDs), and smoke tests validate activation without registry.
- **SC-005**: COM‑activating test suites run under non‑admin accounts with no pre‑registration steps, with ≥95% of previously registration‑dependent tests passing unchanged.

## Constitution Alignment Notes

- Data integrity: If this feature alters stored data or schemas, include an explicit
  migration plan and tests/scripted validation in this spec.
- Internationalization: If text rendering or processing is affected, specify complex
  script scenarios to validate (e.g., right‑to‑left, combining marks, Graphite fonts). Rendering engines are expected to remain functional; smoke tests should include complex scripts.
- Licensing: List any new third‑party libraries and their licenses; confirm compatibility
  with LGPL 2.1 or later.
