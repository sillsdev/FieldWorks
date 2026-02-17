## ADDED Requirements

### Requirement: Pure-logic launcher methods have unit tests

All pure-logic methods (no WinForms dependencies) on launcher classes SHALL have unit tests. At minimum this covers `MorphTypeAtomicLauncher.IsStemType()`, `CheckForAffixDataLoss()`, and `CheckForStemDataLoss()`.

#### Scenario: IsStemType correctly classifies all morph type GUIDs

- **WHEN** `IsStemType()` is called with each of the `MoMorphTypeTags.kguidMorphType*` GUIDs
- **THEN** it SHALL return `true` for stem, root, bound-root, bound-stem, clitic, enclitic, proclitic, particle, and phrase types, and `false` for all affix types (prefix, suffix, infix, etc.)

#### Scenario: CheckForAffixDataLoss detects affix data

- **WHEN** `CheckForAffixDataLoss` is called on a morph with affix-type data (e.g., inflection features, affix slots)
- **THEN** it SHALL return `true` indicating data would be lost

#### Scenario: CheckForStemDataLoss detects stem data

- **WHEN** `CheckForStemDataLoss` is called on a morph with stem-type data
- **THEN** it SHALL return `true` indicating data would be lost

### Requirement: DataTree refresh protocol has integration tests

The `DoNotRefresh` → `RefreshListNeeded` → `RefreshList` protocol SHALL have integration tests covering the key interaction patterns.

#### Scenario: Refresh occurs after DoNotRefresh release with RefreshListNeeded

- **WHEN** `DoNotRefresh` is set to `true`, data is changed, `RefreshListNeeded` is set to `true`, and then `DoNotRefresh` is set to `false`
- **THEN** slices SHALL reflect the updated data (e.g., `ifdata` slices disappear when their data is cleared)

#### Scenario: No refresh without RefreshListNeeded

- **WHEN** `DoNotRefresh` is set to `true`, data is changed, and `DoNotRefresh` is set to `false` WITHOUT setting `RefreshListNeeded`
- **THEN** slices SHALL remain stale (this documents the bug pattern)

#### Scenario: Multiple PropChanged during DoNotRefresh

- **WHEN** multiple `PropChanged` notifications arrive while `DoNotRefresh` is `true`
- **THEN** a single `RefreshList` call SHALL process all accumulated changes when `DoNotRefresh` is released

### Requirement: Seam interfaces enable test doubles

An `IDataTreeServices` interface SHALL be introduced to abstract the dependency bundle (LcmCache, Mediator, PropertyTable) that Slices and Launchers currently access through concrete `DataTree` references.

#### Scenario: Interface is consumable by existing code

- **WHEN** the `IDataTreeServices` interface is introduced
- **THEN** existing Slice and Launcher code SHALL compile without changes (the interface is additive, implemented by `DataTree`)

#### Scenario: Tests can use mock services

- **WHEN** a test needs to verify launcher logic in isolation
- **THEN** it SHALL be able to provide a test implementation of `IDataTreeServices` without constructing a full DataTree or WinForms control hierarchy

### Requirement: Launcher logic is extractable via Humble Object pattern

Business-critical logic in launchers (morph type swap, data loss checking) SHALL be extractable into POCO classes that can be tested without WinForms dependencies. The launcher class becomes a thin shell delegating to the logic class.

#### Scenario: MorphTypeSwapLogic is independently testable

- **WHEN** `MorphTypeSwapLogic` (or equivalent) is created
- **THEN** it SHALL contain `SwapValues`, `IsStemType`, `CheckForAffixDataLoss`, `ChangeAffixToStem`, and `ChangeStemToAffix` logic
- **AND** it SHALL be testable with only `LcmCache` (via `MemoryOnlyBackendProvider`) and no WinForms controls

#### Scenario: Existing launcher behavior is preserved

- **WHEN** the Humble Object extraction is applied to `MorphTypeAtomicLauncher`
- **THEN** all existing `DetailControlsTests` SHALL continue to pass
- **AND** the user-visible behavior SHALL be identical
