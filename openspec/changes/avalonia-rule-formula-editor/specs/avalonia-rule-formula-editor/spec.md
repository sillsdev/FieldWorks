## ADDED Requirements

### Requirement: Avalonia rule-formula editor edits phonological/morphological rules

FieldWorks SHALL provide an Avalonia interactive rule-formula editor that views and edits the cells of a
phonological or morphological rule (`PhSegmentRule`/`PhRegularRule`/`PhMetathesisRule`/`MoCompoundRule`)
with functional parity to the legacy `RuleFormulaControl`, with the editor view LCModel-free and all rule
mutations performed by an xWorks/Morphology adapter inside one undoable unit of work.

#### Scenario: A rule composes and renders on the Avalonia surface
- **WHEN** a rule tool (e.g. `PhonologicalRuleEdit`) is shown with `UIMode=New` over a populated rule
- **THEN** the rule-formula grid SHALL render the rule's cells (phonemes, natural classes, boundaries, slots) and SHALL NOT show the unsupported-type fallback

#### Scenario: Editing a cell commits one undoable change
- **WHEN** the user inserts, deletes, reorders, or sets a cell and the gesture completes
- **THEN** the change SHALL be applied to the rule through the adapter as exactly one undoable unit of work
- **AND** reopening the rule SHALL show the change round-tripped

#### Scenario: The editor view holds no native engine or LCModel dependency
- **WHEN** the engine-isolation audit runs over the FwAvalonia rule-formula editor
- **THEN** it SHALL find no native Views/Graphite/Uniscribe reference and no direct LCModel mutation in the view

### Requirement: Supporting Grammar cell editors are available

FieldWorks SHALL provide the Avalonia cell/segment editors the rule tools embed — IPA symbol,
phonological-environment string, natural-class selection, and ad-hoc co-prohibition groups — each
registered so its Grammar tool resolves to the Avalonia edit surface under `UIMode=New`.

#### Scenario: A registered Grammar rule tool resolves to Avalonia
- **WHEN** a registered rule tool (`phonemeEdit`, `EnvironmentEdit`, `naturalClassedit`, `compoundRuleAdvancedEdit`, `AdhocCoprohibEdit`) is shown with `UIMode=New`
- **THEN** the surface decision SHALL be Avalonia and its bespoke editor SHALL render
- **AND** with `UIMode=Legacy` the tool SHALL keep the WinForms editor
