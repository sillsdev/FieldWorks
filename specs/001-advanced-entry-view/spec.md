# Feature Specification: Advanced New Entry Avalonia View

**Feature Branch**: `001-advanced-entry-view`
**Created**: 2025-11-03
**Status**: Draft
**Input**: User description: "Create a new Advanced New Entry view that replaces InsertEntryDlg with an Avalonia PropertyGrid-based UI mirroring the LexText Entry/Edit view and backed by the LCM data model as a new Visual Studio project"

## Clarifications

### Session 2025-11-04

- Q: What persistence strategy should the Advanced New Entry use (staged DTO/view-model vs long transaction vs streaming edits)? → A: Use detached DTO/view-model staging in-memory and apply all changes in a single LCModel transaction on Save; do not write during edit; Cancel discards; prefer a single undo unit for the Save action.

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

### Key Entities *(include if feature involves data)*

- **LexEntry**: Core lexical entry object storing lexical forms, citation form, and metadata such as morph type and status.
- **LexSense**: Represents senses associated with the entry, including gloss, definition, and example sentences in multiple writing systems.
- **LexPronunciation**: Holds pronunciation representations and audio references for the entry.
- **LexVariant**: Captures spelling variants, complex forms, and cross-references to other entries.
- **TemplateProfile**: New configuration construct describing default values, sections to show/hide, and ordering for the Advanced New Entry property grid.

Presentation models (DTOs) and mapping glue used by Option A:
- **EntryModel (DTO)**: Detached, PropertyGrid-friendly model for a new entry; holds lexical forms (multi-WS), morph type, POS, senses, pronunciations, variants; implements change notification and attribute-based validation/visibility.
- **SenseModel / ExampleModel (DTOs)**: Models for senses and example sentences with gloss/definition in multiple writing systems.
- **PronunciationModel / VariantModel (DTOs)**: Models for pronunciations and variants.
- **TemplateProfileModel (DTO)**: Mirrors TemplateProfile for UI application and overrides.
- **EntryMapper / SenseMapper / PronunciationMapper / VariantMapper**: Materialize DTOs into LCModel objects inside a single transaction on Save; also support DTO hydration for previews.
- **ValidationService**: Aggregates DTO annotations and consults LCModel preflight checks (e.g., duplicates, referential integrity) without committing.
- **LcmTransactionService**: Opens/commits/rolls back the single Save transaction; integrates with undo/redo when possible.
- **PropertyGrid Integration (descriptors/editors)**: Custom type descriptors, operations menus, and editors for FieldWorks-specific types (WS-aware strings, possibility lists, feature structures, references) hosted by Avalonia.PropertyGrid.

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
- **SC-003**: Support tickets related to incorrect or missing entry metadata during creation decrease by 40% within one release cycle after rollout.
- **SC-004**: At least 80% of pilot users rate the advanced view as "easier" or "no harder" than the legacy InsertEntryDlg in post-release surveys.

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
