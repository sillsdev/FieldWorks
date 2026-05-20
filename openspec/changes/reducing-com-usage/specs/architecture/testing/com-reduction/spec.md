## ADDED Requirements

### Requirement: COM Reduction Uses Characterization Tests
COM-reduction changes SHALL include characterization tests or explicit smoke checks before behavior-sensitive cleanup.

#### Scenario: DebugProcs COM activation is changed
- **WHEN** `Src/Common/FwUtils/DebugProcs.cs` changes its COM activation behavior
- **THEN** tests MUST cover construction failure tolerance, idempotent disposal, and debug-output behavior that does not require real COM activation.

#### Scenario: Clipboard OLE cleanup is removed
- **WHEN** `ModuleEntry::SetClipboard` ownership cleanup is removed
- **THEN** existing managed clipboard behavior MUST be validated with tests and a manual copy/paste smoke check.

### Requirement: Manifest Diffs Are Required For COM Surface Removal
Every optional COM-surface removal SHALL include manifest validation showing only targeted optional CLSIDs were removed.

#### Scenario: Managed COM class is removed from reg-free inputs
- **WHEN** a managed COM class is removed from `Build/RegFree.targets`, `Build/mkall.targets`, or `Src/Common/FieldWorks/BuildInclude.targets`
- **THEN** generated manifests MUST be checked for absence of the removed CLSID
- **AND** required native COM entries MUST remain present.

### Requirement: Risky COM Work Is Optional Until Clarified
Work that touches policy-gated or behavior-sensitive areas SHALL be marked optional until the decision and validation plan are explicit.

#### Scenario: A task proposes deleting non-Windows shims
- **WHEN** a task proposes removing `ViewInputManager` or `ManagedVwWindow`
- **THEN** the task MUST depend on a Windows-first policy decision
- **AND** the Windows `VwTextStore` path MUST be validated after the change.

#### Scenario: A task proposes picture, `VwDrawRootBuffered`, or `UnknownProp` cleanup
- **WHEN** the task touches rendering-adjacent or data-boundary COM behavior
- **THEN** it MUST be marked optional or moved to a separate change unless its scope is limited to documentation or adapter-only preparation.
