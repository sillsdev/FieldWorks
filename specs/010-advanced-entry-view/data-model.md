# Data Model (Phase 1)

This models the **detached staged state** and **Presentation IR** used by the Avalonia.PropertyGrid host under **Path 3** (Parts/Layout as the view contract).

- The view contract comes from the existing Parts/Layout configuration.
- A managed interpreter compiles that contract into a stable IR.
- The UI edits a detached staged state keyed by IR nodes.
- On Save, the staged state is materialized into LCModel objects in a single transaction.

Parity scope is tracked in `specs/010-advanced-entry-view/parity-lcmodel-ui.md`.

## Presentation IR (compiled view contract)

The Presentation IR is UI-agnostic and describes what to render/edit.

### Core nodes
- `GroupNode`: named section/group (ordering + nesting)
- `FieldNode`: editable field (label, required/visibility rules, editor kind)
- `SequenceNode`: repeating child collection (ordering, “ghost” add-first behavior)

### Field metadata
- `EditorKind`: WS text, possibility picker, reference picker, feature structure, plain text, etc.
- `ValueType`: scalar vs multi-WS vs reference vs structured
- `ModelPath`: LCModel-oriented logical path (used by materializers and parity checks)
- `Rules`: required/visibility/depends-on/validation hints

### IR requirements
- Must represent nested structures from the parity checklist (Entry → Senses → Examples, etc.)
- Must represent “ghost” semantics (add-first-item affordances) expressed in Parts/Layout

### Performance constraints (from the Path 3 plan)
- Compilation is cached by a stable key (project + root view + configuration fingerprint).
- Compilation runs off the UI thread and is cancellable.
- Sequences are virtualized by default at the UI layer; validation is driven by staged state + IR rules (not by “all editors instantiated”).

## StagedEntryState (detached staged state)

Staged state is the editable, detached data structure the UI binds to. It is keyed by IR node identity rather than a fixed `EntryModel` schema.

### Key design
- `NodeKey`: stable identifier for an IR node instance
- `StagedValue`: typed value container bound to `FieldNode`
- `StagedSequence`: ordered collection bound to `SequenceNode`

### Value forms (examples)
- `WsStringValue`: `Dictionary<WritingSystemId, string>`
- `ScalarStringValue`: string
- `PossibilityRefValue`: id + display path
- `EntryRefValue`: id + headword preview
- `BoolValue`, `IntValue`, `DateValue`, `StTextValue` (as needed for custom fields)

### Nested editing
- Entry-level sequences: pronunciations, alternate forms, entry refs
- Sense-level sequences: examples, subsenses (if configured)

## TemplateProfile (applied to staged state)

Templates are applied by modifying staged state and/or the active view definition:
- Defaults: seed staged values/collections
- Visibility/order overrides: applied as a view overlay to the compiled IR (or as an alternate IR)

Validation rules
- Required fields enforced per IR (aligned with FR-002 and view contract)
- WS requirements enforced for WS-aware fields
- Reference integrity checks for entry/sense references

## Cross-cutting
- `WritingSystemId`: from project WS registry
- `PossibilityRefValue`: id + display path (tree path)
- `EntryRefValue`: id + headword preview

## Materialization contracts

- `EntryMaterializer`: `StagedEntryState` + IR → `LexEntry` (create entry, set forms, attach children)
- Child materializers: senses, examples, pronunciations, variants, entry refs
- Reverse mapping is optional and typically used for preview/summary: staged state → summary view model

## Parity contracts

- IR-level parity: “does the compiled IR contain the expected sections/fields/nesting?”
- LCModel-level parity: “does materialization create LCModel objects whose values match the staged inputs for the same checklist slice?”

These parity checks are driven by `specs/010-advanced-entry-view/parity-lcmodel-ui.md`.
