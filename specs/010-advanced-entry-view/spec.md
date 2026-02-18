# Feature Specification: Advanced New Entry Avalonia View

**Feature Branch**: `010-advanced-entry-view`
**Created**: 2025-11-03
**Status**: Draft
**Input**: User description: "Create a new Advanced New Entry view that replaces InsertEntryDlg with an Avalonia PropertyGrid-based UI mirroring the LexText Entry/Edit view and backed by the LCM data model as a new Visual Studio project"

## Clarifications

### Session 2025-11-04

- Q: What persistence strategy should the Advanced New Entry use (staged DTO/view-model vs long transaction vs streaming edits)? → A: Use detached DTO/view-model staging in-memory and apply all changes in a single LCModel transaction on Save; do not write during edit; Cancel discards; prefer a single undo unit for the Save action.

### Session 2025-12-17

- Decision: Choose **Path 3** for view construction: **reuse legacy view definitions (Parts/Layout) as the view contract**, interpreted by a new **managed (C#) layout/interpreter layer** that renders/editors in Avalonia.
- Constraint: We will **sunset all C++ UI/view code** over time. New work must not depend on the legacy C++ view runtime.
- Reference: Parity scope and migration constraints are tracked in `specs/010-advanced-entry-view/parity-lcmodel-ui.md`.

## User Scenarios & Testing *(mandatory)*

<!--
  IMPORTANT: User stories should be PRIORITIZED as user journeys ordered by importance.
  Each user story/journey must be INDEPENDENTLY TESTABLE - meaning if you implement just ONE of them,
  you should still have a viable MVP (Minimum Viable Product) that delivers value.

  Assign priorities (P1, P2, P3, etc.) to each story, where P1 is the most critical.
  Think of each story as a standalone slice of functionality that can be:
  - Developed independently
  - Tested independently
  - Deployed independently
  - Demonstrated to users independently
-->

### User Story 1 - Capture full lexical entry on creation (Priority: P1)

Lexicographers launch the Advanced New Entry view from LexText to create a new entry, populate lexical form(s), morphology, pronunciation, and senses within a single Avalonia-based property grid, and commit the entry to the LCM data store without leaving the workflow.

**Why this priority**: This is the core value of the feature—users must be able to create a complete entry in one operation to justify replacing InsertEntryDlg.

**Independent Test**: Trigger the Advanced New Entry command, complete all required fields, and verify a new LexEntry object persists to the project database with the configured properties.

**Acceptance Scenarios**:

1. **Given** a user selects "Advanced New Entry" from the LexText toolbar, **When** they supply a primary lexical form, pick a morph type, and press Save, **Then** a new LexEntry appears in LexText with those values and opens in the standard Entry/Edit view.
2. **Given** the property grid exposes multi-writing-system forms, **When** the user adds spelling variants in two writing systems, **Then** both forms are stored on the new entry and visible in the Entry/Edit view immediately after creation.

---

### User Story 2 - Review and adjust advanced properties before save (Priority: P2)

Advanced users need to inspect and tweak optional attributes (complex form types, variants, grammatical category, custom fields) in the property grid before committing the entry so that the data matches organizational standards.

**Why this priority**: Reduces downstream editing by allowing advanced metadata to be set during creation; critical for teams with strict data requirements but secondary to core creation.

**Independent Test**: Populate optional sections in the property grid, preview the summary panel, and confirm the resulting entry contains all supplied optional data without post-save editing.

**Acceptance Scenarios**:

1. **Given** the property grid displays optional attribute groups, **When** the user enters variant spellings, classification tags, and custom fields, **Then** those values are saved on the appropriate LCM objects upon confirmation.
2. **Given** the property grid provides a read-only summary panel, **When** the user reviews it prior to save, **Then** it lists every populated field so they can catch omissions before committing.

---

### User Story 3 - Reuse templates and defaults for rapid entry (Priority: P3)

Language project managers define reusable default profiles (e.g., preselected morph type, default sense template) that the property grid applies, enabling faster entry creation for common patterns.

**Why this priority**: Speeds bulk entry workflows and aligns with existing team conventions but can ship after core creation flows.

**Independent Test**: Associate a saved template with the Advanced New Entry command, trigger the view, and confirm the property grid seeds fields and metadata according to the template without manual re-entry.

**Acceptance Scenarios**:

1. **Given** a template with preset morph type and default gloss structure, **When** the user opens the advanced view, **Then** those defaults populate the property grid before any user input.
2. **Given** the user overrides a template value, **When** they save the entry, **Then** the overridden value persists, and the template remains unchanged for future use.

---

[Add more user stories as needed, each with an assigned priority]

### Edge Cases

- Required LCM fields (lexical form, morph type) left blank: the view must block save, highlight missing fields, and allow navigation to resolve them.
- Multiple writing systems with complex scripts (RTL, combining marks) must render correctly in Avalonia controls and round-trip to LCM.
- Users opening the advanced view while offline or with stale LCM metadata must receive a clear error and retry option without corrupting data.
- Templates referencing deleted custom fields must gracefully skip those fields and alert the user to update the template.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The application MUST provide an "Advanced New Entry" command from LexText that opens an Avalonia-based window without relying on Windows Forms or C++ visual components.
- **FR-002**: The Avalonia property grid MUST surface required LCM LexEntry attributes (lexical forms, citation form, morph type, part of speech) with validation that blocks save when incomplete.
- **FR-003**: Users MUST be able to add, edit, and remove senses, pronunciations, and variants within the property grid prior to saving the new entry.
- **FR-004**: The view MUST use the LCM data model APIs/shared services to create the LexEntry and associated child objects in the active project database upon user confirmation.
- **FR-005**: The UI MUST display a live summary (preview) of the entry mirroring the LexText Entry/Edit view so users can verify data before saving.
- **FR-006**: The property grid MUST support multiple writing systems, respecting project defaults and enabling per-field writing system selection.
- **FR-007**: The system MUST allow project administrators to define reusable default templates for the advanced view and persist those templates in project settings.
- **FR-008**: Template application MUST prepopulate fields on load while allowing users to override any value before save.
- **FR-009**: The view MUST log validation errors and save attempts to the existing diagnostics framework so QA can trace data issues.
- **FR-010**: On cancel, the view MUST discard any staged objects and leave the project database unchanged.
- **FR-011**: The system MUST persist all changes in a single LCModel transaction only when the user selects Save; no writes occur during editing.
- **FR-012**: The Save operation SHOULD be exposed as a single undoable unit consistent with FieldWorks undo/redo patterns.
- **FR-013**: The Advanced New Entry view MUST be constructed from a **view-definition contract** (legacy Parts/Layout configuration) via a **managed interpreter** and rendered in Avalonia; it MUST NOT require Windows Forms or legacy C++ view components.
- **FR-014**: The system MUST support parity verification against the legacy Entry/Edit surface area using the checklist in `specs/010-advanced-entry-view/parity-lcmodel-ui.md` (including nested structures and behavioral requirements like “ghost” items).
- **FR-015**: View-contract interpretation (Parts/Layout → Presentation IR) MUST be cacheable and MUST run off the UI thread; sequence rendering MUST be virtualized to avoid creating large numbers of live editors for large/customized layouts.

### Key Entities *(include if feature involves data)*

- **LexEntry**: Core lexical entry object storing lexical forms, citation form, and metadata such as morph type and status.
- **LexSense**: Represents senses associated with the entry, including gloss, definition, and example sentences in multiple writing systems.
- **LexPronunciation**: Holds pronunciation representations and audio references for the entry.
- **LexVariant**: Captures spelling variants, complex forms, and cross-references to other entries.
- **TemplateProfile**: New configuration construct describing default values, sections to show/hide, and ordering for the Advanced New Entry property grid.

Presentation models (DTOs) and mapping glue used by Option A:
Presentation models (staged state) and mapping glue (detached edit, single transaction save):
- **StagedEntryState**: Detached staged state for a new entry. Instead of hardcoding a single `EntryModel` shape, the staged state is driven by the interpreted view contract (fields + sequences) while still supporting strong typing where it matters (e.g., WS text).
- **Layout Contract (Parts/Layout)**: The existing XML configuration that describes sections, ordering, nested sequences, “ghost” items, and custom-field patterns.
- **Layout Interpreter / Compiler**: Managed (C#) component that loads Parts/Layout definitions and compiles them into a stable **Presentation IR**.
- **Presentation IR**: A UI-agnostic tree describing groups/sections, fields, sequences, required/visibility rules, ghost rules, and editor kinds.
- **PropertyGrid Adapter**: Binds the Presentation IR to Avalonia.PropertyGrid (type descriptors and editor selection) while reading/writing the detached staged state.
- **EntryMaterializer**: Applies staged state into LCModel objects inside a single transaction on Save; contains mapping rules for nested children.
- **ValidationService**: Validates staged state using IR requirements + LCModel preflight checks without committing.
- **LcmTransactionService**: Opens/commits/rolls back the single Save transaction; integrates with undo/redo when possible.

Legacy references to inform parity and custom editor behavior:
- `Src/LexText/LexTextControls/PatternView.cs`, `PatternVcBase.cs` — bespoke pattern editor semantics to be reimplemented as a dedicated Avalonia control (not a PropertyGrid field).
- `Src/LexText/LexTextControls/PopupTreeManager.cs`, `POSPopupTreeManager.cs` — hierarchical possibility pickers with special items ("Not sure/Any", "More…").
- `Src/LexText/LexTextControls/FeatureStructureTreeView.cs` — feature-structure selection behavior.
Avalonia.PropertyGrid extension points and theming:
- `PropertyGrid.axaml.cs`, `PropertyGrid.axaml` in Avalonia.PropertyGrid repo — property operations menus, dynamic visibility, control templates; basis for custom editors and theming.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Users complete a new lexical entry (required fields plus at least one sense) via the advanced view within 3 minutes on average during usability testing.
- **SC-002**: 95% of advanced view saves result in entries whose data fully matches the traditional Entry/Edit view fields (verified by automated parity checks).

Parity definition for SC-002:
- “Matches” is evaluated against `specs/010-advanced-entry-view/parity-lcmodel-ui.md` (fields, nesting, and required behaviors), not against any specific legacy C++ UI implementation.
- **SC-003**: Support tickets related to incorrect or missing entry metadata during creation decrease by 40% within one release cycle after rollout.
- **SC-004**: At least 80% of pilot users rate the advanced view as "easier" or "no harder" than the legacy InsertEntryDlg in post-release surveys.

### Performance Acceptance (enforced by Track 1 + tests)

- **SC-005**: Parts/Layout compilation to Presentation IR is deterministic and cached by a stable key; warm loads reuse cached results.
- **SC-006**: UI remains responsive for large layouts because compilation runs off the UI thread and sequence rendering is virtualized.

## Constitution Alignment Notes

- Data integrity: If this feature alters stored data or schemas, include an explicit
  migration plan and tests/scripted validation in this spec.
- Internationalization: If text rendering or processing is affected, specify complex
  script scenarios to validate (e.g., right‑to‑left, combining marks, Graphite fonts).
- Licensing: List any new third‑party libraries and their licenses; confirm compatibility
  with LGPL 2.1 or later.

- Data integrity: Entry creation must reuse existing LCM transaction patterns; automated tests should compare entries created via the advanced view against legacy flows to ensure no orphan objects.
- Internationalization: Validate property grid rendering with RTL scripts, combining marks, and stacked diacritics; ensure Avalonia font fallback equals current Windows Forms behavior.
- Licensing: Avalonia UI (MIT) and Avalonia.PropertyGrid (MIT) must undergo license review and attribution updates to confirm compatibility with FieldWorks distribution.
