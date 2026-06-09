---
name: fieldworks-winforms-to-avalonia-migration
description: Use when planning, reviewing, or implementing FieldWorks WinForms/xWorks/DataTree/XMLViews migration paths to Avalonia, including seam extraction and parity coverage.
---

# FieldWorks WinForms To Avalonia Migration

## Core Rule
Migrate by proving behavior first, extracting seams second, and introducing Avalonia controls only after legacy behavior has executable parity evidence.

## Workflow
1. Prove current behavior, including global UI wiring and fallback behavior.
2. Extract clean seams and explicit host contracts before exposing new product wiring.
3. Promote Avalonia from preview to product only after persistence, localization, and parity evidence exists.
4. Keep WinForms fallback explicit until the migrated region manifest says otherwise.

## Required Baselines
- Entry points: `RecordEditView`, `DataTree`, `SliceFactory`, XMLViews browse/table views, launchers, popup choosers, and command/listener wiring.
- Global wiring: app-setting source, `PropertyTable`/mediator broadcast, live host refresh, focus/command routing, and explicit fallback or blocked state for every affected host.
- Semantics: object/class binding, flid/field binding, labels, visibility, ghost state, expansion, focus order, writing-system metadata, accessibility identity, and localization keys.
- User workflows: create/edit/save/cancel, chooser OK/cancel, undo/redo, refresh/postponed `PropChanged`, keyboard focus restoration, and disposal/unsubscribe.

## Architecture Checks
- Keep WinForms Designer-safe code isolated from extracted logic.
- Extract humble objects/services for modal decisions and data-loss classifiers before replacing controls.
- Put an editor registry or adapter boundary in front of legacy `SliceFactory` behavior before mixing legacy and Avalonia editors.
- Keep the global UI mode contract explicit: the switch may be app-wide, but each consumer must have a deliberate supported, fallback, or blocked state.
- Do not let the active Avalonia host instantiate or drive hidden legacy `DataTree` or menu infrastructure except through explicitly approved baseline adapters.
- Treat product command wiring as product behavior, not preview scaffolding.

## Review Red Flags
- A PR mixes plans, tests, infrastructure, product UI wiring, and unrelated behavior changes.
- Tests manually invoke `OnPropertyChanged`, `ShowRecord`, or similar handlers to simulate runtime wiring.
- Active Avalonia routing depends on a lossy POC DTO mapper or partial `LexEntry`-only fallback without an explicit product contract.
- Avalonia integration is validated only through `-BuildAvalonia` or ad hoc commands instead of the normal repo build/test path.
- Task checkboxes claim UIA2/IME/accessibility/localization parity while evidence says substitute, placeholder, skipped, or future work.
- Avalonia preview data modifies or pretends to modify real project data without a real edit-session contract.

## Handoff
State what is legacy baseline, what is extracted seam, what is Avalonia prototype, what each affected host does under the global switch, and what remains outside parity.