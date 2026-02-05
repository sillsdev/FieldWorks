# Tasks: Render Performance Baseline & Optimization Plan

**Input**: Design documents from [/specs/001-render-speedup/](specs/001-render-speedup/)
**Prerequisites**: plan.md (required), spec.md (required for user stories), research.md, data-model.md, contracts/

**Tests**: Performance/timing tests are required by the specification (FR-002/FR-003/FR-010).

**Organization**: Tasks are grouped by user story to enable independent implementation and testing of each story.

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Project initialization and basic structure

- [x] T001 Create scenario definition file at Src/Common/RootSite/RootSiteTests/TestData/RenderBenchmarkScenarios.json
- [x] T002 [P] Update .gitignore to exclude Output/RenderBenchmarks/** artifacts
- [x] T002a [P] Add feature-flag config file at Src/Common/RootSite/RootSiteTests/TestData/RenderBenchmarkFlags.json (diagnostics on/off, capture mode)

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Core infrastructure that MUST be complete before ANY user story can be implemented

- [x] T003 [P] Create harness class in Src/Common/RootSite/RootSiteTests/RenderBenchmarkHarness.cs (build DummyBasicView, call MakeRoot/CallLayout, capture bitmap via DrawToBitmap)
- [x] T004 [P] Add bitmap diff utility in Src/Common/RootSite/RootSiteTests/RenderBitmapComparer.cs (pixel-perfect comparison + mismatch reporting)
- [x] T005 [P] Add benchmark models + JSON serialization in Src/Common/RootSite/RootSiteTests/RenderBenchmarkResults.cs
- [x] T006 [P] Add scenario data builder helpers in Src/Common/RootSite/RootSiteTests/RenderScenarioDataBuilder.cs
- [x] T007 [P] Add deterministic environment validator in Src/Common/RootSite/RootSiteTests/RenderEnvironmentValidator.cs (fonts/DPI/theme hash)
- [x] T008 [P] Add trace log parser in Src/Common/RootSite/RootSiteTests/RenderTraceParser.cs (parse stage durations)
- [x] T008a [P] Add diagnostics toggle helper in Src/Common/RootSite/RootSiteTests/RenderDiagnosticsToggle.cs (enable/disable trace output)
- [x] T008b [P] Add regression comparer in Src/Common/RootSite/RootSiteTests/RenderBenchmarkComparer.cs (compare runs, flag regressions)

**Checkpoint**: Foundation ready - user story implementation can now begin ✅

---

## Phase 3: User Story 1 - Pixel-Perfect Render Baseline (Priority: P1) 🎯 MVP

**Goal**: Provide a deterministic pixel-perfect baseline for a single entry render

**Independent Test**: Run RenderBaselineTests to render the simple scenario and compare to the approved snapshot

### Tests & Implementation for User Story 1

- [x] T009 [US1] Implement baseline test in Src/Common/RootSite/RootSiteTests/RenderBaselineTests.cs (uses RenderBenchmarkHarness + RenderBitmapComparer)
- [ ] T009a [US1] Pivot: Adopt native capture via VwDrawRootBuffered in RenderBenchmarkHarness.cs (fix ClearType/backgrounds)
- [ ] T009b [US1] Pivot: Replace DummyBasicView with production StVc in RenderBenchmarkTestsBase (enable rich text/styles)
- [x] T010 [US1] Add baseline snapshot for simple scenario at Src/Common/RootSite/RootSiteTests/TestData/RenderSnapshots/simple.png
- [x] T011 [US1] Wire environment hash validation into RenderBenchmarkHarness.cs (fail if environment mismatch)
- [x] T011a [US1] Document DrawToBitmap limitations and skip list in Src/Common/RootSite/RootSiteTests/RenderBenchmarkHarness.cs (ActiveX/RichTextBox handling)

**Checkpoint**: Pixel-perfect baseline for the simple scenario is green ✅

---

## Phase 4: User Story 2 - Rendering Timing Suite (Priority: P2)

**Goal**: Provide cold + warm timing for five scenarios with recorded results

**Independent Test**: Run RenderTimingSuiteTests and verify Output/RenderBenchmarks/results.json is produced with five scenarios and pass/fail pixel checks

### Tests & Implementation for User Story 2

- [x] T012 [US2] Populate RenderBenchmarkScenarios.json with five scenarios (simple, medium, complex, deep-nested, custom-field-heavy)
- [x] T013 [US2] Implement timing suite in Src/Common/RootSite/RootSiteTests/RenderTimingSuiteTests.cs (cold + warm timings per scenario)
- [x] T014 [US2] Add report writer in Src/Common/RootSite/RootSiteTests/RenderBenchmarkReportWriter.cs (summary + top contributors)
- [x] T015 [US2] Add baseline snapshots for remaining scenarios in Src/Common/RootSite/RootSiteTests/TestData/RenderSnapshots/{medium,complex,deep-nested,custom-field-heavy}.png
- [x] T016 [US2] Emit results to Output/RenderBenchmarks/results.json and summary to Output/RenderBenchmarks/summary.md from RenderTimingSuiteTests.cs
- [ ] T016a [US2] Implement run comparison in RenderBenchmarkReportWriter.cs using RenderBenchmarkComparer.cs (highlight regressions)
- [ ] T016b [US2] Add reproducible test data guidance in specs/001-render-speedup/quickstart.md (scenario creation steps)

**Checkpoint**: Five-scenario timing suite produces cold/warm metrics and summary output ✅

---

## Phase 5: User Story 3 - Rendering Trace Diagnostics (Priority: P3)

**Goal**: Add file-based trace timings for core Views rendering stages and integrate into summary

**Independent Test**: Enable diagnostics, render a scenario, confirm trace log entries for each stage and parsed summary output

### Implementation for User Story 3

- [x] T017 [US3] Add trace timing helper in Src/views/VwRenderTrace.h (stage start/stop + duration logging)
- [ ] T018 [US3] Instrument VwRootBox::Layout/PrepareToDraw/DrawRoot/PropChanged in Src/views/VwRootBox.cpp with trace timings (requires native build validation)
- [ ] T019 [US3] Instrument VwEnv render entry points in Src/views/VwEnv.cpp with trace timings (requires native build validation)
- [ ] T020 [US3] Instrument lazy expansion paths in Src/views/VwLazyBox.cpp with trace timings (requires native build validation)
- [x] T021 [US3] Add trace switch/config in Src/Common/FieldWorks/FieldWorks.Diagnostics.dev.config to enable Views render timing output
- [x] T022 [US3] Integrate trace parsing into RenderBenchmarkReportWriter.cs to include top contributors
- [ ] T022a [US3] Add trace validation test in Src/views/Test/TestVwRootBox.h or Src/views/Test/TestVwTextBoxes.h (ensure timing entries emitted)

**Checkpoint**: Trace infrastructure ready; native instrumentation pending build validation

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: Documentation and consistency updates

- [x] T023 [P] Update specs/001-render-speedup/quickstart.md with actual harness usage and output paths
- [x] T024 [P] Review and update Src/views/AGENTS.md for tracing changes
- [x] T025 [P] Review and update Src/Common/RootSite/AGENTS.md for new harness/tests
- [x] T026 [P] Add explicit edge case validations in RenderTimingSuiteTests.cs (no custom fields, no senses, deep nesting variance)

**Checkpoint**: Phase 6 complete - documentation updated, edge case tests added

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies
- **Foundational (Phase 2)**: Depends on Setup completion
- **User Story 1 (Phase 3)**: Depends on Foundational
- **User Story 2 (Phase 4)**: Depends on Foundational and completion of US1 baseline harness
- **User Story 3 (Phase 5)**: Depends on Foundational; can run parallel with US2 once harness exists
- **Polish (Phase 6)**: Depends on all user stories being complete

### User Story Dependencies

- **US1 (P1)**: No dependencies beyond Foundational
- **US2 (P2)**: Uses harness from Foundational + baseline strategy from US1
- **US3 (P3)**: Uses harness + report writer from Foundational/US2

### Parallel Opportunities

- Phase 2 tasks T003–T008 can run in parallel
- US2 scenario snapshots (T015) can run in parallel with report writer (T014)
- US3 instrumentation tasks (T017–T020) can run in parallel
- Doc updates (T023–T025) can run in parallel at the end

---

## Parallel Example: User Story 2

- Task: "Populate RenderBenchmarkScenarios.json" in Src/Common/RootSite/RootSiteTests/TestData/RenderBenchmarkScenarios.json
- Task: "Add report writer" in Src/Common/RootSite/RootSiteTests/RenderBenchmarkReportWriter.cs
- Task: "Add scenario snapshots" in Src/Common/RootSite/RootSiteTests/TestData/RenderSnapshots/{medium,complex,deep-nested,custom-field-heavy}.png

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1: Setup
2. Complete Phase 2: Foundational
3. Complete Phase 3: User Story 1
4. Validate pixel-perfect baseline for the simple scenario

### Incremental Delivery

1. US1 baseline → validate
2. US2 timing suite → validate results output
3. US3 trace diagnostics → validate trace parsing
4. Polish and documentation updates

---

## Notes

- [P] tasks = different files, no dependencies
- Each story should be independently completable and testable
- Keep trace logging file-based and opt-in to avoid measurement distortion

