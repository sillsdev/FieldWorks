## ADDED Requirements

### Requirement: Avalonia interlinear editor renders and edits wordform analyses

FieldWorks SHALL provide an Avalonia interlinear editor that renders a `WfiWordform`'s analyses as aligned
interlinear (wordform line, morpheme breakdown, lex-gloss, grammatical info) and edits each morph-bundle's
morph, sense, and MSA with functional parity to the legacy Sandbox-backed `InterlinearSlice`, with the
editor view LCModel-free (no Sandbox) and all analysis writes performed by an xWorks/Morphology adapter
inside one undoable unit of work.

#### Scenario: A wordform's analyses render on the Avalonia surface
- **WHEN** the `Analyses` tool is shown with `UIMode=New` over a populated wordform
- **THEN** the interlinear control SHALL render the aligned analysis lines and SHALL NOT show the unsupported-type fallback

#### Scenario: Editing a morph-bundle commits with MSA cleanup
- **WHEN** the user changes a bundle's morph/sense/MSA and the gesture completes
- **THEN** the analysis SHALL be written back through the adapter as one undoable unit of work
- **AND** an MSA no surviving sense of the analysis uses SHALL be pruned (parity with the legacy Sandbox write-back)
- **AND** reopening the wordform SHALL show the edit round-tripped

#### Scenario: The interlinear view holds no Sandbox, native engine, or LCModel dependency
- **WHEN** the engine-isolation audit runs over the FwAvalonia interlinear control
- **THEN** it SHALL find no native Views/Graphite/Uniscribe reference, no Sandbox, and no direct LCModel mutation in the view

#### Scenario: The Analyses tool honors the UI-mode gate
- **WHEN** `Analyses` is shown with `UIMode=New`
- **THEN** the surface decision SHALL be Avalonia
- **AND** with `UIMode=Legacy` the tool SHALL keep the WinForms interlinear
