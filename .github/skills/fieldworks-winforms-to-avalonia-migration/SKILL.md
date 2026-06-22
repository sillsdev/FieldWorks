---
name: fieldworks-winforms-to-avalonia-migration
description: Use when planning, reviewing, or implementing FieldWorks WinForms/xWorks/DataTree/XMLViews migration paths to Avalonia, including seam extraction and parity coverage.
---

# FieldWorks WinForms To Avalonia Migration

## Core Rule
Migrate by proving behavior first, extracting seams second, and introducing Avalonia controls only after legacy behavior has executable parity evidence.

## Required Baselines
- Entry points: `RecordEditView`, `DataTree`, `SliceFactory`, XMLViews browse/table views, launchers, popup choosers, and command/listener wiring.
- Semantics: object/class binding, flid/field binding, labels, visibility, ghost state, expansion, focus order, writing-system metadata, accessibility identity, and localization keys.
- User workflows: create/edit/save/cancel, chooser OK/cancel, undo/redo, refresh/postponed `PropChanged`, keyboard focus restoration, and disposal/unsubscribe.

## Architecture Checks
- Keep WinForms Designer-safe code isolated from extracted logic.
- Extract humble objects/services for modal decisions and data-loss classifiers before replacing controls.
- Put an editor registry or adapter boundary in front of legacy `SliceFactory` behavior before mixing legacy and Avalonia editors.
- Treat product command wiring as product behavior, not preview scaffolding.

## Review Red Flags
- A PR mixes plans, tests, infrastructure, product UI wiring, and unrelated behavior changes.
- Task checkboxes claim UIA2/IME/accessibility/localization parity while evidence says substitute, placeholder, skipped, or future work.
- Avalonia preview data modifies or pretends to modify real project data without a real edit-session contract.

## Handoff
State what is legacy baseline, what is extracted seam, what is Avalonia prototype, and what remains outside parity.