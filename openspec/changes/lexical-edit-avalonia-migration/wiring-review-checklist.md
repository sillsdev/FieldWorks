# UI Wiring Review Checklist (Task 3.11)

A repeatable checklist for every feature-flag or host-routing change that affects which UI surface is
active. It operationalizes the `fieldworks-ui-wiring-review` skill for this change. Attach a filled copy
to each PR that touches surface selection, `PropertyTable`/mediator routing, or host replacement.

## Scope

- [ ] Reviewed against the branch-only diff (`main..HEAD`), not a calendar-day commit list.
- [ ] Listed **every** `RecordEditView` consumer / host affected (Lexicon, Grammar, Notebook, Lists,
      Words), and each has a deliberate behavior under both UI modes: supported Avalonia, explicit
      legacy fallback, or resource-backed blocked state.

## Wiring path (trace end to end)

- [ ] **Setting source** — where the mode is read (app setting / persisted `UIMode` preference).
- [ ] **Persisted state** — persistence flag set/cleared correctly; no accidental global persistence.
- [ ] **`PropertyTable` key** — `UIMode` (and `currentContentControl` for per-tool resolution).
- [ ] **Broadcast** — change is delivered via the real mediator/property broadcast, not a manual
      `OnPropertyChanged(...)` call in production or tests.
- [ ] **Listener registration** — the host subscribes/unsubscribes symmetrically (no leak).
- [ ] **Resolution** — routed through `LexicalEditSurfaceSelectionService` (3.9), not ad-hoc reads of
      settings/property-table state scattered in the host.
- [ ] **Host reload path** — current content is re-shown on switch without a tool reload.
- [ ] **Focus / command target routing** — active surface added to Ctrl+Tab/message targets; inactive
      surface removed.
- [ ] **Save / `PrepareToGoAway()`** — routes to the active surface only.
- [ ] **Fallback / blocked state** — non-migrated hosts fall back explicitly.

## Active-host contract (3.10)

- [ ] The active Avalonia path does **not** instantiate or drive a hidden legacy `DataTree`/menu
      infrastructure (no `EnsureLegacySurfaceInitialized` / `DataTree.ShowObject` while Avalonia is
      active), except through an adapter explicitly declared in `ActiveHostContract.AllowedBaselineAdapters`.
- [ ] An audit test proves it (e.g. `RecordEditViewActiveHostContractTests`).

## Product vs preview boundary

- [ ] Product route uses a **typed-definition-backed region model** (`LexicalEditRegionModel`), not a
      lossy `LexicalEditPocMapper` DTO or preview host code.
- [ ] Preview-only artifacts (`PocEntryDto`, `PocPreviewDataProvider`, sample data) are not on any
      product route.
- [ ] Product-facing strings are localizable; remaining prototype strings are called out as gaps.
- [ ] Stable, nonlocalized `AutomationId`s on user-facing controls; localized names/tooltips allowed.

## Build / test graph

- [ ] Validated through the normal repo path (`./build.ps1`, `./test.ps1`) plus host-specific tests —
      not a branch-only `-BuildAvalonia` lane as the primary evidence.
- [ ] Tests drive the real setting + broadcast path; none simulate wiring via direct handler calls.
