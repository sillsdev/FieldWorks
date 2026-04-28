---
applyTo: "Src/**/*.{cs,resx}"
name: "fieldworks-ui-review"
description: "Copilot code review checks for FieldWorks WinForms, xWorks, interlinear display, and localization changes"
---

# FieldWorks UI Review Checks

## Purpose

Use these checks for managed UI, WinForms dialogs, xWorks, interlinear display,
and resource changes.

## Crash prevention

- In event handlers and dialog callbacks, verify `sender`, selected items,
  selected indices, model objects, and cast results are checked before use.
- Flag `Items[index]`, `SelectedItems[0]`, `SelectedIndex`, or `as Type` casts
  when bounds or null checks are missing.
- Verify deleted or replaced model objects cannot remain in chooser lists,
  cached selections, mediator state, or reference collections.
- Check that UI updates from background work marshal back to the UI thread.

## Display and layout regressions

- For interlinear, preview pane, dictionary, media-line, and tree/list display
  changes, verify refresh/invalidation and persistence behavior are covered.
- Review `TableLayoutPanel`, `FlowLayoutPanel`, anchoring, docking, minimum size,
  tab order, and resizable dialog behavior when layout files change.
- Flag display logic changes without corresponding tests or explicit manual
  validation notes for the affected user workflow.

## Localization and resources

- User-visible strings belong in `.resx` resources, not hardcoded in C#.
- Resource keys should be stable, descriptive, and reused consistently between
  code, designer files, and localized resources.
- When `.resx` files change, verify designer/resource accessors stay in sync.

## Designer safety

- Keep generated initialization in designer files and application logic in the
  non-designer partial class.
- Flag manual designer edits that remove disposal, component initialization, or
  resource manager usage.
