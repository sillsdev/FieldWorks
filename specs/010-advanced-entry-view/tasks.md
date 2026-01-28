# Tasks Backlog (Phase 2)

This backlog turns the **Path 3** architecture (Parts/Layout as the view contract) into executable work. Each task links to FR/SC from the spec and artifacts from the plan.

Legend: Priority (P1 highest), Estimate (S ≤ 0.5d, M 1–2d, L 3–5d), Dep (dependencies by Task ID).

## Track 0 — Project setup (P1)

- [X] T0.1 Create net8.0 Avalonia project `Src/LexText/AdvancedEntry.Avalonia`
  - Est: S
  - Accept: Project builds; PropertyGrid package restores; app skeleton launches.
- [X] T0.2 Create test project `Src/LexText/AdvancedEntry.Avalonia/AdvancedEntry.Avalonia.Tests`
  - Est: S
  - Accept: NUnit tests run in CI; baseline green.
- [X] T0.3 Wire logging to FieldWorks diagnostics
  - Est: S
  - Accept: Info/warn/error from module appear in existing logs.

- [X] T0.4 Add shared Avalonia Preview Host
  - Est: S
  - Accept: Can build and launch a module by id without running FieldWorks.

- [X] T0.5 Register AdvancedEntry module + sample data provider
  - Est: S | Dep: T0.4
  - Accept: Preview host can launch AdvancedEntry with empty and sample data modes.

- [X] T0.6 Add convenience launcher script `scripts/Agent/Run-AvaloniaPreview.ps1`
  - Est: S | Dep: T0.4
  - Accept: One command builds + launches preview host with `-Module` and `-Data`.

- [X] T0.7 Add LexText “Advanced New Entry” command entry point
  - Est: M | Dep: T0.1
  - Accept: Command is available from LexText UI, opens the Avalonia window, and does not rely on WinForms or legacy C++ view runtime (FR-001/FR-013).
  - Accept: Feature can be gated/disabled safely (staged rollout) without breaking LexText startup.

## Track 1 — View contract, interpretation, and staging (Path 3) (P1)

- [X] T1.1 Define the Presentation IR (sections, fields, sequences, ghost rules)
  - Est: S
  - Accept: IR can represent the structures in `specs/010-advanced-entry-view/parity-lcmodel-ui.md` (nested senses/examples, sequences, ghost items).
  - Reference: `specs/010-advanced-entry-view/presentation-ir-research.md` (IR goals + reuse points).
- [X] T1.2 Load Parts/Layout definitions (LexEntry starting point)
  - Est: M | Dep: T1.1
  - Accept: Can load and resolve the initial LexEntry root layout entry point (documented in plan.md): `LexEntry` / `detail` / `Normal`.
  - Accept: Shipped defaults resolve from `DistFiles/Language Explorer/Configuration/Parts/` (`LexEntry.fwlayout` + `LexEntryParts.xml`).
  - Accept: Resolution order (override → default → fallback) is implemented and covered by unit tests.
  - Accept: Does not invoke legacy C++ view runtime.
  - Reference: `specs/010-advanced-entry-view/presentation-ir-research.md` (“Inventory + LayoutCache” and override/unification semantics).
- [X] T1.3 Compile Parts/Layout → Presentation IR for LexEntry
  - Est: M | Dep: T1.2
  - Accept: IR output contains at least the P0/P1 groups needed for MVP; ordering/groups match the resolved Parts/Layout definitions for the chosen root view id(s).
  - Accept: IR compilation is deterministic (same inputs → same IR) and has a snapshot-style test.
  - Accept: Initial compiler approach is **Path 3** from `specs/010-advanced-entry-view/presentation-ir-research.md`: translate the existing `XmlVc` `DisplayCommand` graph into a typed Presentation IR (collector env is allowed for debugging/verification, but must not be the public IR contract).
  - Accept: Compiler design reuses managed XMLViews semantics where feasible without requiring the legacy C++ view runtime.

- [X] T1.4 Implement detached staged state keyed by IR nodes
  - Est: M | Dep: T1.1
  - Accept: Stage supports nested collections (senses/examples/pronunciations) and can round-trip values from the UI without touching LCModel.
- [X] T1.5 PropertyGrid adapter: IR → PropertyGrid descriptors + editor selection
  - Est: L | Dep: T1.3, T1.4
  - Accept: PropertyGrid renders fields/sections per IR; changing a field updates staged state; works in Preview Host.
- [X] T1.6 ValidationService (IR-aware + LCModel preflight)
  - Est: M | Dep: T1.4
  - Accept: Returns actionable errors without committing (FR-009); required field validation aligned with FR-002 and parity checklist.

- [X] T1.7 Implement Presentation IR compilation cache + stable cache key
  - Est: M | Dep: T1.2, T1.3
  - Accept: Cache key includes (at minimum) project identity, root class id, root layout/part id, and a configuration fingerprint (default config version + user overrides).
  - Accept: Cache invalidation is explicit and covered by unit tests.
  - Accept: Warm compilation reuses cached IR (verified by a unit test that asserts the compiler is not re-run for the same key).
  - Reference: `specs/010-advanced-entry-view/plan.md` (“Path 3 performance acceptance checklist”).

- [X] T1.8 Async compilation boundary + cancellation
  - Est: M | Dep: T1.7, T1.5
  - Accept: Compiler work runs off the UI thread; UI thread only binds results.
  - Accept: Closing the window (or switching entry points) cancels compilation cleanly.
  - Accept: Diagnostics include compilation duration and whether result was cache hit/miss.
  - Reference: `specs/010-advanced-entry-view/presentation-ir-research.md` (why heavy contract/custom-field work can be slow today).

- [ ] T1.9 Virtualization boundary for sequences (large layouts/custom fields)
  - Est: M | Dep: T1.5
  - Accept: Sequence editors do not instantiate controls for all children up-front; they render incrementally/virtually.
  - Accept: Validation does not require materializing all UI editors (validation driven by staged state + IR rules).
  - Reference: `specs/010-advanced-entry-view/plan.md` (“Virtualization boundary”).

## Track 2 — Mapping & transactions (P1)

- [ ] T2.1 AdvancedEntryEditSessionService (BeginEdit/Save/Cancel; duration logging)
  - Est: M
  - Accept: BeginEdit starts a long-lived undo task for the entire edit session.
  - Accept: Save ends the undo task and triggers the explicit commit; Cancel/Close rolls back to the start.
  - Accept: Unit tests cover: (a) cancel rolls back changes, (b) save commits changes, (c) exceptions roll back.
- [ ] T2.2 CommitFence + Undo ownership guards
  - Est: M | Dep: T2.1
  - Accept: While an edit session is active, prevent unrelated code paths from committing/saving the project.
  - Accept: Prevent other services from ending/replacing the outer undo task during editing (e.g., record navigation “auto-save” behaviors that call `EndUndoTask()`).
  - Accept: Tests demonstrate that commit/save attempts are blocked/ignored while the editor is active.
- [ ] T2.3 LCModel-first binding layer (IR → LCModel property access)
  - Est: L | Dep: T1.3, T1.5, T2.1
  - Accept: PropertyGrid edits directly mutate the LCModel-backed model during the edit session.
  - Accept: Multi-WS string fields bind correctly (FR-006) and remain rollbackable.
- [ ] T2.4 New-object creation flows (LexEntry children)
  - Est: L | Dep: T2.3
  - Accept: Creating/removing objects (senses/pronunciations/variants) is rollbackable on Cancel.
  - Accept: Save persists created objects as a single undo unit (FR-012).

## Track 3 — Custom editors (P1)

- [ ] T3.1 WsTextEditor (multi-WS alternative strings; RTL/combining aware)
  - Est: L | Dep: T0.1
  - Accept: Select WS; enter text; preview reflects WS; i18n tests pass (FR-006).
- [ ] T3.2 PossibilityTreeEditor (hierarchical, “Not sure/Any”, “More…”)
  - Est: L | Dep: T0.1
  - Accept: Picker supports special items; operation menu launches master dialog (FR-002).
- [ ] T3.3 FeatureStructureEditor (closed/complex features)
  - Est: L | Dep: T0.1
  - Accept: Radio-like endpoints; non-duplicate constraints enforced.
- [ ] T3.4 ReferencePickerEditor (entry/sense search selection)
  - Est: M | Dep: T0.1
  - Accept: Search/select reference; stored as EntryRef.

## Track 4 — PropertyGrid host & visibility (P1)

- [ ] T4.1 AdvancedEntryView (PropertyGrid host) with operations menu hooks
  - Est: M | Dep: T1.5, T3.*
  - Accept: Per-field operations visible; context-sensitive.
- [ ] T4.2 Dynamic visibility rules (from IR + runtime filter)
  - Est: M | Dep: T1.5
  - Accept: “Show if non-empty” vs “always show” can be expressed in view definitions and applied at runtime.

- [ ] T4.3 Cancel/Close behavior (discard staged state; no writes)
-  - Est: M | Dep: T4.1, T2.1
  - Accept: Clicking Cancel closes the window and rolls back the edit-session undo task (FR-010).
  - Accept: No commit occurs; DB remains unchanged even if fields were edited.
  - Accept: Any temporary guards (commit-fence / suppressions) are restored on close.

## Track 5 — Summary preview (P2)

- [ ] T5.1 EntrySummaryView (read-only mirror of Entry/Edit)
  - Est: M | Dep: T1.3, T1.4, T4.1
  - Accept: Summarizes all populated fields prior to Save (FR-005); grouping/order driven by the same view contract.

## Track 6 — Observability & errors (P2)

- [ ] T6.1 Log validation failures and Save attempts/results
  - Est: S | Dep: T1.6, T2.*
  - Accept: Logs include counts, WS coverage, durations (FR-009).
- [ ] T6.2 Error UX: inline highlights + non-blocking panel
  - Est: S | Dep: T1.4, T4.1
  - Accept: Save blocked on required fields; user guided to fix (Edge Cases).

- [ ] T6.3 Offline / stale-metadata preflight + user-facing error flow
  - Est: M | Dep: T1.2, T1.6
  - Accept: If required LCModel metadata (WS, possibility lists, etc.) is unavailable/stale, user sees a clear error with a retry option; staged state is preserved until user closes.
  - Accept: No partial writes occur; diagnostics include root cause and context.

## Track 7 — Tests (P1/P2)

Testing approach (preferred): Layered unit/integration tests for IR/staging/validation/materialization plus a small set of headless Avalonia interaction tests for UI wiring. Avoid full Windows UI automation.

- [ ] T7.0 Parity harness: checklist-driven assertions (P0/P1)
  - Est: M | Dep: T1.3, T2.2
  - Accept: Implements a parity test harness that maps items from `specs/010-advanced-entry-view/parity-lcmodel-ui.md` (at least P0/P1) to executable assertions.
  - Accept: Verifies (a) compiled IR contains expected sections/fields/nesting and (b) materialized LCModel contains expected values for the same checklist slice (FR-014, SC-002).

- [ ] T7.1 Unit tests: IR + staging validation (required, WS)
  - Est: M | Dep: T1.1, T1.4, T1.6
  - Accept: Green; covers edge inputs; aligns with parity checklist requirements.

- [ ] T7.1a Headless Avalonia UI interaction tests (invisible)
  - Est: M | Dep: T1.5
  - Accept: Runs without OS-visible windows (headless) and drives the AdvancedEntry view through core interactions.
  - Accept: Tests cover at least: expand/collapse nested `Senses` and `Examples`, typing updates staged state, hover + right-click invoke expected command hooks (or no-op placeholders until operations are implemented).
  - Notes: This is intentionally limited to UI wiring; most behavior remains covered by layered unit tests.
- [ ] T7.2 Unit tests: Materializers (forms, children)
  - Est: M | Dep: T2.*
  - Accept: Staged state → LCModel parity checks.
- [ ] T7.3 Integration tests: end-to-end Save (single transaction, undo) + Cancel no-write
  - Est: L | Dep: T2.1, T2.2, T4.1
  - Accept: Save persists exactly once (explicit commit) and one undo step reverts the entire Save (FR-011/012).
  - Accept: Cancel path performs no commit and leaves the DB unchanged (FR-010).
  - Accept: Guardrail test: attempts to commit/save during an active edit session are blocked/ignored.
- [ ] T7.4 i18n tests: RTL/combining in WsTextEditor
  - Est: S | Dep: T3.1
  - Accept: Correct rendering and round-trip.
- [ ] T7.5 Performance checks: Save ≤ 250 ms typical
  - Est: S | Dep: T2.*
  - Accept: Measured on baseline hardware; report in logs (SC-001/002 proxy).

- [ ] T7.6 Performance acceptance tests: compilation caching + UI-thread boundary
  - Est: S | Dep: T1.7, T1.8
  - Accept: Unit tests assert (a) stable cache key behavior and (b) compilation does not run on the UI thread (by capturing the calling scheduler/context).
  - Accept: Logs include cache hit/miss and compilation duration.

## Track 8 — Templates (P2)

- [ ] T8.1 TemplateService (load/apply defaults; override tracking)
  - Est: M | Dep: T1.3, T4.2
  - Accept: Defaults applied on open; user overrides preserved (FR-007/008).

## Track 9 — Docs, licensing, and COPILOT docs (P2)

- [ ] T9.1 License notices for Avalonia + PropertyGrid (MIT)
  - Est: S
  - Accept: Notices included; compliance confirmed.
- [ ] T9.2 Update relevant `Src/**/COPILOT.md` and folder docs
  - Est: S | Dep: All
  - Accept: Docs reflect new project and editors.
- [ ] T9.3 Quickstart walkthrough verified
  - Est: S | Dep: All
  - Accept: Steps execute cleanly on a clean machine.

---

## Milestones

- M1 (P1 scope): T0 + T1 + T2 + T3.1 + T4 + T7.1–T7.3 ⇒ MVP (create/save entry end-to-end) using Path 3 view contract
- M2: T3.2–T3.4 + T5 + T6 + T8 + T7.4–T7.5 ⇒ Feature-complete advanced entry
- M3: T9 ⇒ Documentation and compliance

## References
- Spec: `specs/010-advanced-entry-view/spec.md`
- Plan: `specs/010-advanced-entry-view/plan.md`
- Parity checklist: `specs/010-advanced-entry-view/parity-lcmodel-ui.md`
- Analysis: `Avalonia Property copilot analysis.md`
- Presentation IR research: `specs/010-advanced-entry-view/presentation-ir-research.md`
- Legacy parity: `Src/LexText/LexTextControls/PatternView.cs`, `PatternVcBase.cs`, `PopupTreeManager.cs`, `POSPopupTreeManager.cs`, `FeatureStructureTreeView.cs`
- Avalonia.PropertyGrid: `PropertyGrid.axaml.cs`, `PropertyGrid.axaml`

Key code touchpoints identified during research (implementation should reuse/learn from these):
- `Src/XCore/Inventory.cs` (override/unification semantics)
- `Src/Common/Controls/XMLViews/LayoutCache.cs` (layout/part resolution + inventories)
- `Src/Common/Controls/XMLViews/XmlVc.cs` (managed interpreter + `DisplayCommand` layer)
- `Src/Common/Controls/XMLViews/PartGenerator.cs` (custom-field expansion cost center)
