## ADDED Requirements

### Requirement: Durable validation rules live behind a FieldWorks-owned validation seam

Durable lexical validation rules SHALL live behind a FieldWorks-owned validation seam that can evaluate staged edits independently of Avalonia control materialization.

#### Scenario: Validation runs without forcing editor creation
- **WHEN** a migrated lexical region validates staged data
- **THEN** validation SHALL be able to evaluate required rules, cross-field rules, and commit gates without forcing live editor or control materialization

### Requirement: Avalonia presents validation through native binding surfaces

Avalonia editors SHALL present validation state through native Avalonia binding and validation surfaces such as `INotifyDataErrorInfo`, `DataValidationErrors`, or equivalent UI adapters.

#### Scenario: Field issue appears in Avalonia editor
- **WHEN** the validation seam reports a field-scoped issue for visible staged data
- **THEN** the corresponding Avalonia editor SHALL expose the error state through native Avalonia validation presentation and accessibility metadata

### Requirement: Package validators remain subordinate to the validation seam

DataAnnotations, CommunityToolkit validation helpers, FluentValidation, or similar libraries MAY be used behind the validation seam or for simple dialogs, but SHALL NOT replace the FieldWorks-owned validation contract for migrated lexical editing.

#### Scenario: FluentValidation stays behind seam
- **WHEN** a validator library is used to implement cross-field, collection, async, or localized rules
- **THEN** the library SHALL feed structured validation results into the FieldWorks-owned validation seam rather than becoming the public migration contract
