## MODIFIED Requirements

### Requirement: Required COM Boundaries Remain Explicit
FieldWorks SHALL preserve registration-free COM for required native boundaries, including Views/FwKernel, RootBox, Graphite-related rendering, Windows TSF input, MSAA accessibility, and Views-facing data-access contracts.

#### Scenario: Cleanup proposal touches required native COM boundary
- **WHEN** a COM-reduction change proposes edits under `Src/views/`, `Src/Common/ViewsInterfaces/`, or required reg-free manifests
- **THEN** the change MUST identify whether the edited COM surface is required or optional before implementation
- **AND** the change MUST NOT remove required native COM activation or globalize COM registration.

### Requirement: Optional Managed COM Surface Removal Includes Manifest Cleanup
When a managed class stops being a COM activation surface, FieldWorks SHALL remove the corresponding class-specific manifest/build inputs in the same implementation slice.

#### Scenario: Managed COM class becomes managed-only
- **WHEN** a class such as `ManagedLgIcuCollator` has `ComVisible(true)` removed
- **THEN** related entries MUST be removed from `Build/RegFree.targets`, `Build/mkall.targets`, and `Src/Common/FieldWorks/BuildInclude.targets` where present
- **AND** generated manifests MUST no longer contain that class as a `clrClass`.

### Requirement: Compatibility Decisions Precede CLSID Removal
FieldWorks SHALL clarify whether an optional managed CLSID is an external compatibility contract before deleting it.

#### Scenario: Optional managed CLSID has no repo-local activation
- **WHEN** code search shows only direct managed construction for a `ComVisible(true)` class
- **THEN** the implementation plan MUST still record whether out-of-repo automation or extensions depend on its CLSID
- **AND** if compatibility is uncertain, the implementation SHOULD stage manifest removal and COM visibility removal separately.

### Requirement: New Managed Code Avoids Expanding COM Surface
New or modified managed FieldWorks code SHALL avoid adding `ComVisible(true)`, direct CLSID activation, or direct ProgID activation unless the boundary is explicitly approved and documented.

#### Scenario: New interop seam is added
- **WHEN** a new managed service needs access to legacy COM functionality
- **THEN** it MUST prefer a FieldWorks-owned adapter interface over direct COM activation at call sites
- **AND** it MUST include tests that do not require machine-local COM registration.
