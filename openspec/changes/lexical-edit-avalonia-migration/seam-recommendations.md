# Avalonia Seam Recommendations

This note records the recommended seam direction for the Lexical Edit Avalonia migration. It is advisory; the companion seam docs define the concrete gates and tests. Current implementation is deliberately distinguished from proposed seams.

> **Branch-state correction (2026-06-09).** Earlier revisions of this note described
> `AdvancedEntryEditSession`, `ValidationService`, and `MainWindowViewModel` as the "current
> implementation." Those types are **not present on this branch** (`010-advanced-entry-view-phase-1-2`).
> They live on the prototype branch `010-advanced-entry-preview-prototype` and were never merged here.
> What this branch actually contains is: the typed seam interfaces in `FwAvalonia/Seams/ISeams.cs`
> (5 with pure-logic implementations, `IXCoreCommandBridge`/`IRecordNavigationContext` contract-only,
> `IEditSession` as a seam contract only), the typed view-definition IR under
> `FwAvalonia/ViewDefinition/`, the shared preview path at the region-model boundary, and the
> feature-flagged live host (`LexicalEditHostControl` + `RecordEditView`). The "Current
> implementation" lines below have been re-labelled accordingly:
> the prototype is a **reference to reproduce behind the seam**, not shipped code in this branch.
> See `seam-domain-comparison.md` for the per-domain before/ideal/now/recommended breakdown.

## Coexistence constraints driving these recommendations

These are fixed product constraints; the seam choices below assume them:

1. **Avalonia 11.x only during coexistence.** We stay on the latest 11.x and do **not** move to
   Avalonia 12 until WinForms is fully removed. So the Avalonia 12 WinForms message filter and
   per-thread dispatcher work are unavailable; cross-interop-boundary tab/focus
   ([AvaloniaUI/Avalonia#12025](https://github.com/AvaloniaUI/Avalonia/issues/12025)) and popup-DPI
   quirks must be handled by us, and we host **coarsely** (one Avalonia view per host, not many
   small islands sharing a WinForms tab order).
2. **~1-year coexist phase, then WinForms is deleted.** Each *class* of UI is wholly WinForms **or**
   wholly Avalonia at a time, but different classes run **concurrently** and must cooperate through
   three shared data channels: **selection** ("this is the current lexeme", task 3.12), **copy/paste**
   (task 3.13), and **drag-and-drop** (task 3.14 — product decision 2026-06-09: cross-surface DnD is
   supported, reusing the clipboard payload formats). These bridges are real, must-build, and
   bidirectional — not throwaway scaffolding. Beyond data interchange, four behaviors gate the first
   *editable* slice because both surfaces share one LCModel cache and one window chrome:
   **cross-surface refresh propagation** (`PropChanged`/F5 reach both surfaces, task 3.15), **one
   global undo/redo stack** (LCModel `IActionHandler`, 6.8/6.10 — two stacks is user-visible data
   weirdness), **screen-local command/menu/focus routing** to the active surface (the local phase of
   `avalonia-command-focus`), and **dialog ownership/modality** across the interop boundary (task
   3.16). What *is* throwaway is wiring the new ports into *legacy internals* (e.g. threading
   `RefreshCoordinator` into the live `DataTree`), because that code is deleted at cutover.
3. **XML-layout retirement is a separate effort.** Moving authoring off XML Parts/Layout to a modern
   typed format is desirable but out of scope here; this change keeps the XML→IR importer and treats
   the typed IR as the runtime contract the Avalonia side consumes.

## Recommended path (Path 3): thin enforced surface seam + sequenced convergence

The clean seam is **not** legacy re-plumbed through every port. It is (a) the surface-selection
boundary (`LexicalEditSurfaceResolver`/`Factory` + the new `LexicalEditSurfaceSelectionService`) and
(b) the typed IR as the data contract the Avalonia side consumes. Legacy stays frozen behind the
switch until cutover. Concretely:

- Enforce the **active-host contract** (3.10): the active Avalonia path must not instantiate or drive
  a hidden legacy `DataTree`. This was violated (the original spike drove `m_dataEntryForm.ShowObject` then hid
  it); it is now an audited invariant.
- Replace the **lossy `LexicalEditPocMapper` DTO** on the product route with a
  **typed-definition-backed region model** (4.8); keep only lightweight preview region-model scenarios on the preview path.
- Build the **selection, clipboard, and drag-and-drop bridges** (3.12/3.13/3.14) as bidirectional
  adapters over the shared xCore/LCModel/OS substrate; do not re-plumb legacy internals. Clipboard
  and DnD speak the legacy `"TsString"` OS format so native-Views surfaces interoperate unchanged.
- Treat **cross-surface refresh propagation** (3.15) and **global LCModel undo/redo** as first-editable-
  slice gates, not cleanup: shared-cache consistency stands or falls on the notification loop, and a
  split undo history is the most jarring coexistence failure a user can hit.
- Reproduce the prototype's **LCModel-fenced edit session and validation** behind `IEditSession`/the
  validation seam when the first product editor lands (6.x); the prototype branch is the reference.

Supporting docs:

- `avalonia-edit-sessions.md`
- `avalonia-undo-redo.md`
- `avalonia-validation.md`
- `avalonia-command-focus.md`
- `avalonia-ui-scheduler.md`
- `avalonia-lifetime.md`

## Edit Sessions

**Current implementation (this branch):** `IEditSession` is defined as the seam contract; the old detached preview stub was retired with `poc-retiring.md`. Product editing now rides `IRegionEditContext` plus the real LCModel-backed region session on the xWorks side, while a real fenced LCModel undo-task session (`AdvancedEntryEditSession`, with `Save()`/`Cancel()`) still exists **only on the prototype branch** `010-advanced-entry-preview-prototype` as the reference to reproduce behind `IEditSession`.

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

**Current implementation (this branch):** none. A `ValidationService` performing deterministic required-field checks over Presentation IR (skipping unmaterialized lazy items) exists **only on the prototype branch** `010-advanced-entry-preview-prototype`; reproduce it behind the validation seam when the first editable slice lands.

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

**Current implementation (this branch):** `IXCoreCommandBridge` is contract-only (no implementation). The local Avalonia key bindings and view-model commands referenced here are on the prototype branch `010-advanced-entry-preview-prototype`, not this branch.

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

**Current implementation (this branch):** `IUiScheduler` is defined with an `ImmediateUiScheduler` (synchronous, for tests/non-view code); the live app supplies a dispatcher-backed scheduler at the view edge. Direct dispatcher use in the prototype module lives on `010-advanced-entry-preview-prototype`.

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

**Current implementation (this branch):** `IRegionLifetime`/`RegionLifetime` is implemented and tested (reverse-order, idempotent disposal). The `MainWindowViewModel` ownership pattern referenced here is on the prototype branch `010-advanced-entry-preview-prototype`.

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