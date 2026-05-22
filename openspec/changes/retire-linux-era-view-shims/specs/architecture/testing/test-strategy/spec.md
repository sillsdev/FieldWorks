## ADDED Requirements

### Requirement: Windows-First Views Shim Retirement Requires Native and Manifest Validation
Retiring `ViewInputManager` or `ManagedVwWindow` SHALL include validation for native Views behavior and reg-free COM manifest output.

#### Scenario: Shim source and build entries are removed
- **WHEN** implementation removes either Linux-era managed Views shim from source, solution, or manifest inputs
- **THEN** validation MUST include a native Views test pass using the repository test script
- **AND** validation MUST include reg-free manifest tests or generated-manifest inspection for the retired CLSIDs.

#### Scenario: Views IDL or generated interop artifacts change
- **WHEN** `Src/views/Views.idh` or generated Views interop artifacts are updated
- **THEN** validation MUST account for native-before-managed build ordering
- **AND** stale generated outputs MUST be cleaned or regenerated before claiming the change is complete.

### Requirement: Windows RootSite Smoke Testing Covers Input and Geometry
Shim retirement SHALL include manual smoke coverage for the Windows behaviors previously protected by the shims' conceptual responsibilities.

#### Scenario: Manual input smoke is performed
- **WHEN** the rebuilt application is smoke-tested after shim removal
- **THEN** the tester MUST edit text in a RootSite field, switch keyboards if available, and test IME/composition if available
- **AND** any failures MUST block completion until triaged against the native `VwTextStore` path.

#### Scenario: Manual selection geometry smoke is performed
- **WHEN** the rebuilt application is smoke-tested after `ManagedVwWindow` removal
- **THEN** the tester MUST exercise mouse selection, keyboard selection, and PageUp/PageDown navigation in a RootSite field
- **AND** visible page movement MUST remain consistent with the pre-removal Windows behavior.
