## MODIFIED Requirements

### Requirement: Encoding Converter Access Uses FieldWorks Adapter Seams
New or changed FieldWorks code that uses SIL Encoding Converters SHALL depend on a FieldWorks-owned provider or service interface rather than directly constructing `SilEncConverters40.EncConverters` at application call sites.

#### Scenario: Encoding converter lookup is added or modified
- **WHEN** code under `Src/FwCoreDlgs/`, `Src/ParatextImport/`, `Src/LexText/`, or related import/configuration workflows needs converter lookup
- **THEN** the call site MUST use the FieldWorks-owned adapter seam for the selected workflow
- **AND** direct `new EncConverters()` construction MUST remain centralized inside the production adapter.

### Requirement: Encoding Converter Adapter Preserves Existing Behavior
The Encoding Converter adapter SHALL preserve existing converter lookup, exception handling, missing-converter behavior, and conversion results for each migrated workflow.

#### Scenario: Converter is missing or unavailable
- **WHEN** a migrated workflow cannot instantiate or locate a requested converter
- **THEN** the user-visible error behavior MUST match the previous workflow-specific behavior
- **AND** tests MUST be able to simulate the failure without machine-installed converters.

### Requirement: No Replacement Package Is Introduced Now
The first Encoding Converter cleanup SHALL NOT replace `encoding-converters-core` with a new NuGet package.

#### Scenario: Adapter implementation is added
- **WHEN** the adapter is implemented
- **THEN** it MUST wrap the existing `SilEncConverters40` / `encoding-converters-core` dependency
- **AND** it MUST use existing NUnit/Moq infrastructure for tests rather than adding a new mocking framework.
