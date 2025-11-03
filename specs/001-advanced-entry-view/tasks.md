# Tasks Backlog (Phase 2)

This backlog turns the Option A architecture into executable work. Each task links to FR/SC from the spec and artifacts from the plan.

Legend: Priority (P1 highest), Estimate (S ≤ 0.5d, M 1–2d, L 3–5d), Dep (dependencies by Task ID).

## Track 0 — Project setup (P1)

- [ ] T0.1 Create net8.0 Avalonia project `Src/LexText.AdvancedEntry.Avalonia`
  - Est: S
  - Accept: Project builds; PropertyGrid package restores; app skeleton launches.
- [ ] T0.2 Create test project `Src/LexText.AdvancedEntry.Avalonia.Tests`
  - Est: S
  - Accept: NUnit tests run in CI; baseline green.
- [ ] T0.3 Wire logging to FieldWorks diagnostics
  - Est: S
  - Accept: Info/warn/error from module appear in existing logs.

## Track 1 — DTOs & validation (P1)

- [ ] T1.1 Implement DTO base `LcmPropertyModelBase` (INotifyPropertyChanged, helpers)
  - Est: S
  - Accept: Derived models raise changes; works with PropertyGrid.
- [ ] T1.2 Implement `EntryModel` with attributes ([Display], [Required], visibility)
  - Est: M | Dep: T1.1
  - Accept: Renders in PropertyGrid; required fields enforced (FR-002).
- [ ] T1.3 Implement `SenseModel`, `ExampleModel`, `PronunciationModel`, `VariantModel`, `TemplateProfileModel`
  - Est: M | Dep: T1.1
  - Accept: Collections editable; default values applied.
- [ ] T1.4 ValidationService (DTO + LCModel preflight)
  - Est: M | Dep: T1.2, T1.3
  - Accept: Returns actionable errors without committing (FR-009).

## Track 2 — Mapping & transactions (P1)

- [ ] T2.1 LcmTransactionService (begin/commit/rollback; duration logging)
  - Est: S
  - Accept: Unit test simulates successful commit and rollback.
- [ ] T2.2 EntryMapper (DTO → LexEntry; forms, morph type, POS)
  - Est: M | Dep: T1.2, T2.1
  - Accept: Integration test creates LexEntry matching DTO (FR-004).
- [ ] T2.3 Sense/Pronunciation/Variant mappers
  - Est: M | Dep: T1.3, T2.1
  - Accept: Children attached; round-trip preview consistent.
- [ ] T2.4 Undo integration (single undo unit)
  - Est: M | Dep: T2.1–T2.3
  - Accept: One undo step reverts entire Save (FR-012).

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
  - Est: M | Dep: T1.*, T3.*
  - Accept: Per-property operations visible; context-sensitive.
- [ ] T4.2 Dynamic visibility rules (attributes + runtime filter)
  - Est: S | Dep: T4.1
  - Accept: “Show if non-empty” vs “always show” behavior configurable (FR-007/008 visibility rules support).

## Track 5 — Summary preview (P2)

- [ ] T5.1 EntrySummaryView (read-only mirror of Entry/Edit)
  - Est: M | Dep: T1.*, T4.1
  - Accept: Summarizes all populated fields prior to Save (FR-005).

## Track 6 — Observability & errors (P2)

- [ ] T6.1 Log validation failures and Save attempts/results
  - Est: S | Dep: T1.4, T2.*
  - Accept: Logs include counts, WS coverage, durations (FR-009).
- [ ] T6.2 Error UX: inline highlights + non-blocking panel
  - Est: S | Dep: T1.4, T4.1
  - Accept: Save blocked on required fields; user guided to fix (Edge Cases).

## Track 7 — Tests (P1/P2)

- [ ] T7.1 Unit tests: DTO validation (required, WS)
  - Est: S | Dep: T1.*
  - Accept: Green; covers edge inputs.
- [ ] T7.2 Unit tests: Mappers (forms, children)
  - Est: M | Dep: T2.*
  - Accept: DTO→LCM parity checks.
- [ ] T7.3 Integration tests: end-to-end Save (single transaction, undo)
  - Est: M | Dep: T2.4, T4.1
  - Accept: One entry created; one undo step removes it (FR-011/012).
- [ ] T7.4 i18n tests: RTL/combining in WsTextEditor
  - Est: S | Dep: T3.1
  - Accept: Correct rendering and round-trip.
- [ ] T7.5 Performance checks: Save ≤ 250 ms typical
  - Est: S | Dep: T2.*
  - Accept: Measured on baseline hardware; report in logs (SC-001/002 proxy).

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

- M1 (P1 scope): T0 + T1 + T2 + T3.1 + T4 + T7.1–T7.3 ⇒ MVP (create/save entry end-to-end)
- M2: T3.2–T3.4 + T5 + T6 + T8 + T7.4–T7.5 ⇒ Feature-complete advanced entry
- M3: T9 ⇒ Documentation and compliance

## References
- Spec: `specs/001-advanced-entry-view/spec.md`
- Plan: `specs/001-advanced-entry-view/plan.md`
- Analysis: `Avalonia Property copilot analysis.md`
- Legacy parity: `Src/LexText/LexTextControls/PatternView.cs`, `PatternVcBase.cs`, `PopupTreeManager.cs`, `POSPopupTreeManager.cs`, `FeatureStructureTreeView.cs`
- Avalonia.PropertyGrid: `PropertyGrid.axaml.cs`, `PropertyGrid.axaml`
