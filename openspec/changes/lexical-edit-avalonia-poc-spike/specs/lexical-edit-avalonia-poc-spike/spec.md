## ADDED Requirements

### Requirement: The POC spike never changes default behavior

The proof-of-concept SHALL be gated by a feature flag that defaults to the existing WinForms Lexical
Edit surface. The Avalonia surface SHALL only be constructed when the flag is explicitly enabled.

#### Scenario: Default build runs WinForms unchanged
- **WHEN** FieldWorks runs without the POC flag set
- **THEN** the Lexical Edit surface SHALL be the existing WinForms `DataTree`/`Slice` path
- **AND** no Avalonia runtime, host, or POC slice SHALL be constructed

#### Scenario: Flag enables the Avalonia POC slice
- **WHEN** the `FW_AVALONIA_LEXEDIT` flag (or its `PropertyTable`/registry override) is enabled
- **THEN** the host SHALL construct the Avalonia POC surface for the target slice instead of the
  WinForms surface for that slice
- **AND** the same build SHALL be able to run either surface without recompilation

### Requirement: The POC proves an in-process host bridge or records the fallback

The spike SHALL attempt in-process embedding of the Avalonia slice into the WinForms host on
.NET Framework 4.8 first, and SHALL record measured evidence of success or the documented fallback.

#### Scenario: In-process embedding is proven
- **WHEN** the in-process host-bridge tasks complete
- **THEN** the evidence report SHALL state whether `WinFormsAvaloniaControlHost` rendered, sized, and
  received focus correctly under the FieldWorks net48 startup at 100% and 150% DPI

#### Scenario: In-process embedding fails within the time box
- **WHEN** in-process embedding cannot be proven within the spike time box
- **THEN** the spike SHALL record the failure and switch to the out-of-process net8 preview-host
  fallback rather than expanding scope

### Requirement: The POC slice reproduces functional fidelity and density, not pixels

The Avalonia POC slice SHALL reproduce the WinForms baseline's labels, editor affordances, focus
order, writing-system text behavior, and information density to near-pixel tolerance.

#### Scenario: Three representative editors are present
- **WHEN** the Avalonia POC slice is shown for a `LexEntry`
- **THEN** it SHALL render a multi-writing-system lexeme-form editor, a morph type popup chooser, and
  one sense-gloss multi-writing-system editor over the live LCModel data

#### Scenario: Parity is captured semantically and by density
- **WHEN** the POC slice is compared to the WinForms baseline
- **THEN** a normalized semantic snapshot (label, field, flid, editor kind, visibility, focus order,
  accessibility name, writing-system metadata) SHALL be captured for both surfaces
- **AND** density measurements (visible rows, label/editor column widths, line height) SHALL be
  captured at 100% and 150% DPI
- **AND** every difference SHALL be classified as accepted near-pixel variance, font/rendering
  variance, missing data, or regression

### Requirement: The POC slice has no native viewing or Graphite dependency

The Avalonia POC slice SHALL NOT instantiate or call native Views/C++ display, layout, measurement,
hit testing, selection, or editor-realization code, nor Graphite render engines, at runtime.

#### Scenario: Headless test asserts no native/Graphite dependency
- **WHEN** the Avalonia.Headless POC test renders the slice and commits an edit
- **THEN** it SHALL pass without instantiating native Views or Graphite render engines

### Requirement: Editing commits through the fenced LCModel edit session

The POC slice SHALL commit and cancel edits through the existing fenced LCModel edit-session model,
with control-local text undo allowed only as subordinate leaf behavior.

#### Scenario: Commit and cancel match the WinForms slice
- **WHEN** a user edits the lexeme form or sense gloss in the Avalonia POC slice and commits
- **THEN** the LCModel state SHALL match the result of the equivalent WinForms edit
- **AND** cancelling SHALL leave the LCModel state unchanged

### Requirement: The spike ends with evidence and a go/no-go

The spike SHALL conclude with a written evidence report that updates the roadmap estimates and gives
an explicit recommendation for the regional Lexical Edit migration.

#### Scenario: Evidence report exists before handoff
- **WHEN** the spike is considered complete
- **THEN** `spike-evidence.md` SHALL record the host-bridge result, density/fidelity comparison,
  edit-commit/cancel behavior, defects, and a go/no-go for `datatree-model-view-separation` and
  `lexical-edit-avalonia-migration`
