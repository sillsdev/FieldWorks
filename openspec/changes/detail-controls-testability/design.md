## Context

The `Src/Common/Controls/DetailControls/` subsystem is the property-editing backbone of FLEx. It comprises three parallel class hierarchies — Slices (rows in the property editor), Launchers (chooser-button controls), and Views (value renderers) — all hosted inside a `DataTree` container. The architecture predates modern testability patterns: classes inherit from WinForms `UserControl`, there are no interfaces, and logic is deeply entangled with UI lifecycle.

Current state:
- **~65 production files, ~31 tests** (7 test files)
- **11 key classes with zero test coverage** (ButtonLauncher, ReferenceLauncher, all Slice subclasses, all Views, both Choosers)
- **Deep inheritance** — `MorphTypeAtomicLauncher` is 5 levels deep from `UserControl`
- **Bidirectional coupling** — Launchers → Slice → DataTree → Slices → Launchers
- **Ad-hoc refresh protocol** — `DoNotRefresh`, `RefreshListNeeded`, `m_postponePropChanged` are independent booleans with complex interactions
- **No interfaces** — everything depends on concrete classes

The class diagram is maintained separately in [detail-controls-class-diagram.mmd](detail-controls-class-diagram.mmd).

## Goals / Non-Goals

**Goals:**
- Make launcher business logic testable without WinForms
- Document the architecture so agents and developers understand the composition pattern
- Incrementally introduce testability seams without breaking existing code
- Cover the refresh protocol (source of LT-22414 and similar bugs) with tests

**Non-Goals:**
- Rewriting or replacing WinForms with another framework
- Achieving high test coverage for all 65 files in one pass
- Changing user-visible behavior
- Introducing new runtime dependencies

## Decisions

### 1. Incremental seams over big-bang rewrite

**Decision:** Use Legacy Seam (Feathers) and Branch By Abstraction (Fowler) patterns. Introduce `IDataTreeServices` interface implemented by `DataTree`. Existing code continues to use `DataTree` directly; new test code uses the interface.

**Rationale:** The codebase works. Rewriting carries regression risk with no user-visible benefit. Seams can be introduced one at a time, validated by builds, with zero behavior change.

**Alternatives considered:**
- Full MVP/MVVM rewrite — too risky, too much churn, no user-visible benefit
- Test only through UI automation — slow, flaky, doesn't catch logic bugs
- Leave untested — unacceptable given the LT-22414 class of bugs

### 2. Humble Object extraction for launcher logic

**Decision:** Extract a `MorphTypeSwapLogic` POCO class from `MorphTypeAtomicLauncher` containing `SwapValues`, `IsStemType`, `CheckForAffixDataLoss`, `ChangeAffixToStem`, `ChangeStemToAffix`. The launcher delegates to this class.

**Rationale:** These methods contain business-critical data-mutation logic that should never have lived in a `UserControl` subclass. Extraction follows the Humble Object pattern (Feathers/Meszaros) — the logic class needs only `LcmCache`, not Forms/Graphics/Mediator.

**Alternatives considered:**
- Make methods `internal` and test via `InternalsVisibleTo` — already done for `SwapValues` but doesn't reduce coupling
- Test through DataTree integration tests only — works (we did this for LT-22414) but is slow and can't cover edge cases without full UI setup

### 3. Strangler Fig for new slice types

**Decision:** Any new slice type or field editor written from this point forward SHALL use the testable architecture (interfaces, extracted logic). Existing slices are migrated only when they need bug fixes.

**Rationale:** Fowler's Strangler Fig pattern — new growth uses the better architecture, old code is migrated opportunistically. This avoids a big migration project while ensuring quality improves over time.

### 4. Diagram as standalone Mermaid file

**Decision:** Maintain the class diagram as a standalone `.mmd` file alongside the design doc, referenced from `AGENTS.md`. Not embedded inline.

**Rationale:** Standalone `.mmd` files render in GitHub, can be validated by Mermaid CLI, and are referenceable from multiple documents without duplication.

## Risks / Trade-offs

| Risk | Mitigation |
|------|------------|
| `IDataTreeServices` interface becomes a "god interface" | Keep it narrow — only the services that tests actually need. Split into finer interfaces as needed. |
| Humble Object extraction introduces delegation overhead | Negligible — these methods run once per user gesture, not in tight loops. |
| Architecture docs go stale | Reference from `AGENTS.md` which has automated change-log tracking. The Mermaid diagram is machine-parseable and can be validated. |
| Incremental approach leaves some classes permanently untested | Track coverage gaps in the architecture doc. Prioritize testing when classes are modified for bug fixes. |

## Open Questions

1. Should `IDataTreeServices` live in `DetailControls/` or in a shared interface assembly?
2. Should the `RefreshCoordinator` state machine be a separate class or a formalization within `DataTree`?
3. How much of the `ReferenceLauncher.HandleChooser` flow is worth extracting given the dialog dependencies?
