# Avalonia Seam Recommendations

This note records the recommended seam direction for the Lexical Edit Avalonia migration. It is advisory; the companion seam docs define the concrete gates and tests. Current implementation is deliberately distinguished from proposed seams.

Supporting docs:

- `avalonia-edit-sessions.md`
- `avalonia-undo-redo.md`
- `avalonia-validation.md`
- `avalonia-command-focus.md`
- `avalonia-ui-scheduler.md`
- `avalonia-lifetime.md`

## Edit Sessions

**Current implementation:** `AdvancedEntryEditSession` is a concrete fenced LCModel undo-task session with `Save()` and `Cancel()`.

**Recommendation:** Keep the direct LCModel fenced undo-task model for the first editable slice, then extract a FieldWorks-owned edit-session seam only with lifecycle, rollback, and global undo/redo tests.

**Alternatives considered:**

1. Direct LCModel fenced session.
Pros: closest to current code and LCModel action-handler semantics.
Cons: cancel/save semantics need strong tests before many fields are editable.

2. Staged draft model.
Pros: clean pre-commit validation and cancel behavior.
Cons: larger mapping layer and conflict/stale-state work.

3. Package-led draft editing using ReactiveUI or similar.
Pros: good local ergonomics.
Cons: does not solve LCModel transactions and adds framework commitment.

**Revisit trigger:** adopt staged drafts only after a first-slice test proves direct sessions create unacceptable complexity or user-visible risk.

**References:** Avalonia data validation, Avalonia commanding/hotkeys, MVVM Toolkit, ReactiveUI commands/validation.

## Undo and Redo

**Current implementation:** local save/cancel exists; there is no implemented `IUndoRedoCoordinator`.

**Recommendation:** Keep control-local text undo as leaf behavior, but make global undo/redo authoritative through FieldWorks/LCModel transaction routing.

**Alternatives considered:**

1. Pure FieldWorks global transaction stack.
Pros: correct persisted-state semantics.
Cons: needs control integration work.

2. Package-first object/view-model history.
Pros: easy prototype.
Cons: risks conflicting histories and wrong LCModel source of truth.

3. Hybrid local text undo plus global LCModel undo.
Pros: best desktop editing UX while preserving domain history.
Cons: requires explicit focus/command routing rules.

**Revisit trigger:** only for a specific owned control that needs richer document-local undo and still commits through LCModel.

## Validation

**Current implementation:** `ValidationService` performs deterministic required-field checks over Presentation IR and skips unmaterialized lazy items.

**Recommendation:** Use a FieldWorks-owned validation model with Avalonia presentation adapters, preferably `INotifyDataErrorInfo` or `DataValidationErrors` where that maps cleanly to controls.

**Alternatives considered:**

1. Native Avalonia validation only.
Pros: simple and idiomatic.
Cons: insufficient for cross-object rules, localization metadata, and non-materialized nodes.

2. FluentValidation/ReactiveUI behind the seam.
Pros: strong rule composition.
Cons: should remain implementation detail, not migration contract.

3. Domain validation seam with Avalonia adapters.
Pros: reusable across tests, preview host, and shell integration.
Cons: requires structured issue paths and localization contract.

**Revisit trigger:** collapse to native-only validation only for isolated dialogs or surfaces with no LCModel/cross-object semantics.

## Command and Focus

**Current implementation:** the spike has local Avalonia key bindings and view-model commands. There is no XCore command bridge yet.

**Recommendation:** Use local Avalonia commands for first-slice/preview behavior; introduce a FieldWorks/XCore bridge only during shell integration.

**Alternatives considered:**

1. Avalonia built-ins only.
Pros: fast and idiomatic.
Cons: insufficient for shell menus, command state, and active target resolution.

2. MVVM package commands.
Pros: nice local command ergonomics.
Cons: still needs shell routing.

3. Custom bridge to XCore/property state.
Pros: correct shell integration.
Cons: easy to over-preserve legacy quirks if introduced too early.

**Revisit trigger:** narrow or defer the bridge if shell-global command needs are smaller than expected.

## UI Scheduler

**Current implementation:** dispatcher calls are used directly in the Avalonia module; no shared scheduler seam exists.

**Recommendation:** Introduce a thin `IUiScheduler` only where non-view code needs testable UI-thread marshalling, cancellation, or exception propagation. Keep direct dispatcher use at concrete UI edges.

**Alternatives considered:**

1. Direct dispatcher everywhere.
Pros: simplest code.
Cons: hard to fake and leaks Avalonia into service layers.

2. Thin wrapper.
Pros: easy to test and small.
Cons: can become pointless if it only renames APIs.

3. Reactive scheduler abstraction.
Pros: powerful for reactive screens.
Cons: unnecessary as global default.

**Revisit trigger:** collapse low-value wrappers that provide no test or architecture value.

## Lifetime

**Current implementation:** `MainWindowViewModel` owns and disposes the loaded lifetime on save/cancel; no `ILexicalLifetimeManager` exists.

**Recommendation:** Keep ownership explicit in the view model for the first slice; extract a lifetime manager only after late-loader, idempotent-disposal, event-unsubscribe, and shell-unload tests exist.

**Alternatives considered:**

1. Direct Avalonia lifetime everywhere.
Pros: quickest for preview-host code.
Cons: spreads shutdown/disposal policy.

2. Thin lifetime manager.
Pros: testable owner for sessions, loaders, callbacks, and shell registrations.
Cons: premature if first slice stays small.

3. Heavy region/document lifetime framework.
Pros: strongest explicit ownership tree.
Cons: overdesign until repeated cross-screen lifetime failures prove need.

**Revisit trigger:** introduce heavier framework only when repeated region/window ownership bugs appear.

## Research References

- Avalonia official docs: data validation, binding validation, commanding, keyboard/hotkeys, focus, dispatcher threading, headless testing, app lifetimes, windows/dialogs, accessibility automation properties.
- Microsoft docs: MVVM Toolkit commands and `ObservableValidator`.
- ReactiveUI docs: commands, validation, and scheduler testing.
- HarfBuzz official docs: Graphite2 shaping requires HarfBuzz built with Graphite2 enabled and is not enabled by default.