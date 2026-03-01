## Context

DataTree.cs is 4,358 lines implementing a WinForms `UserControl` that fuses 8+ responsibilities into one class. It is the core detail-editing control in FieldWorks — every entry, sense, and record is displayed through DataTree slices. The Avalonia migration roadmap requires that both WinForms and Avalonia coexist in the same repo, meaning we need a UI-framework-agnostic model layer that either can consume.

**Current architecture:**
```
RecordEditView → DataTree (monolith: XML parsing + WinForms layout + navigation + messaging + persistence)
                    ↕
                 Slice (3,341 lines, also monolithic)
                    ↕
                 SliceFactory, ObjSeqHashMap, SliceFilter
```

**Key coupling points:**
- `Slice.ContainingDataTree` — every Slice holds a reference to the parent DataTree
- `SliceFactory.Create()` — returns WinForms `Slice` objects directly
- `StTextDataTree : DataTree` — the only subclass, in InfoPane.cs
- `DTMenuHandler.m_dataEntryForm` — holds a typed `DataTree` reference
- `RecordEditView.m_dataEntryForm` — the primary consumer

**Existing test coverage:** ~14 tests in DataTreeTests.cs, all focused on `ShowObject` + show-hidden logic. No tests for XML parsing internals, navigation, reuse, or layout.

## Goals / Non-Goals

**Goals:**
- Separate "what slices to show" (model) from "how to render them" (view) so the model layer can be shared between WinForms and Avalonia
- Make the XML-to-slice pipeline testable without WinForms infrastructure
- Reduce DataTree.cs to a manageable size where any developer can understand each file's purpose
- Preserve exact runtime behavior throughout — zero functional regressions
- Enable incremental delivery: each phase ships independently and is valuable on its own

**Non-Goals:**
- Building an Avalonia `DataTreeView` — that is a separate future change
- Refactoring Slice.cs internals beyond partial-class splitting
- Changing the XML layout/part format or Inventory loading
- Modifying DTMenuHandler or RecordEditView beyond minimal adaptation
- Performance optimization (the reuse map is complex enough; preserve it as-is)
- Replacing the XCore Mediator pattern

## Decisions

### Decision 1: Four-phase incremental delivery

**Choice:** Deliver in 4 sequential phases, each independently mergeable.

**Phases:**
1. **Characterization tests** (Phase 0) — add ~20 tests covering current behavior
2. **Partial-class split** (Phase 1) — mechanical file decomposition, zero logic changes
3. **Extract collaborators** (Phase 2) — `SliceLayoutBuilder`, `ShowHiddenFieldsManager`, `SliceCollection` as composition targets inside DataTree
4. **Model/View separation** (Phase 3) — introduce `DataTreeModel`, `SliceSpec`, `IDataTreeView`; DataTree becomes a thin WinForms view

**Rationale:** Each phase is valuable alone. Phase 0 provides a safety net. Phase 1 improves readability immediately. Phase 2 enables unit testing of the most complex logic. Phase 3 unlocks Avalonia. If the project stalls at any phase, the completed work still has value.

**Alternative considered:** Big-bang model/view extraction in one PR. Rejected because DataTree touches every detail view in the application — risk of subtle regressions is too high without phased safety nets.

### Decision 2: SliceSpec as intermediate representation

**Choice:** Introduce a `SliceSpec` class (plain C# object, no WinForms dependency) that captures everything needed to create a slice: label, abbreviation, indent, editor type, XML config, field ID, object, visibility, weight, tooltip, key path.

**Rationale:** The current code creates `Slice` WinForms controls directly inside the XML parsing loop (`AddSimpleNode` → `SliceFactory.Create`). This is the fundamental coupling that prevents model/view separation. `SliceSpec` breaks this dependency: the model produces specs, the view materializes them.

**Alternative considered:** Having the model produce abstract `ISlice` interfaces. Rejected because `ISlice` would still need to carry WinForms `Control` semantics and the interface would grow as large as the concrete class.

### Decision 3: SliceFactory stays in the view layer (Phase 3)

**Choice:** `SliceFactory.Create()` remains in the view layer and accepts `SliceSpec` instead of raw XML nodes. The model layer's `SliceLayoutBuilder` produces `SliceSpec` lists; the view layer calls `SliceFactory.Create(spec)` to materialize them.

**Rationale:** SliceFactory creates ~30 concrete WinForms slice types. Moving it to the model layer would require abstracting all those types. Keeping it in the view layer means only `SliceSpec` crosses the boundary, and each platform (WinForms, Avalonia) has its own factory.

**Alternative considered:** A fully abstract factory with platform adapters. Deferred to the Avalonia implementation phase since we don't yet know what Avalonia slice equivalents look like.

### Decision 4: Keep ObjSeqHashMap in the view layer

**Choice:** The slice reuse map (`ObjSeqHashMap`) stays in the view layer because it maps `SliceSpec.Key` → existing `Slice` instances. The model layer does not concern itself with reuse optimization.

**Rationale:** Reuse is a performance optimization that depends on having concrete control instances. The model layer simply produces a fresh `SliceSpec` list on each `ShowObject`/`RefreshList` call. The view layer diffs against existing slices using `ObjSeqHashMap`.

### Decision 5: StTextDataTree adaptation strategy

**Choice:** `StTextDataTree` currently overrides `ShowObject` to transform the root object (CmBaseAnnotation → owner Text) before calling `base.ShowObject`. In Phase 3, this becomes a model-layer hook: `DataTreeModel` gains a virtual `ResolveRootObject(ICmObject) → ICmObject` method that `StTextDataTree` overrides via a custom `DataTreeModel` subclass or a delegate injection.

**Rationale:** The override is purely about object resolution (model concern), not rendering. Moving it to the model layer is both natural and simple.

### Decision 6: Composition over inheritance for DataTreeModel

**Choice:** `DataTreeModel` uses composition — it holds `SliceLayoutBuilder`, `ShowHiddenFieldsManager`, and a `SliceCollection` (state tracker). These are injected via constructor or property.

**Rationale:** DataTree's current design suffers from being a single class that inherited too many responsibilities. Composition makes each piece independently testable and replaceable. The only inheritance left is `DataTree : UserControl` (required by WinForms) and the `StTextDataTree` subclass.

## Risks / Trade-offs

**[Risk] SliceSpec may not capture all slice creation context** → Mitigation: Phase 2 extracts `SliceLayoutBuilder` *before* introducing `SliceSpec`, so we learn exactly what information flows from XML parsing to slice creation. `SliceSpec` is designed after that extraction, informed by real data.

**[Risk] ObjSeqHashMap reuse breaks during model/view split** → Mitigation: Phase 0 characterization tests verify reuse behavior. Phase 3 preserves the exact key structure (`Slice.Key` → `SliceSpec.Key`).

**[Risk] Subtle behavior differences after partial-class split** → Mitigation: This is a zero-logic change; the compiler guarantees identical IL. Characterization tests from Phase 0 provide additional confidence.

**[Risk] DTMenuHandler and other consumers reference concrete DataTree** → Mitigation: Phase 3 keeps the concrete `DataTree` class; it just becomes thinner. Consumers don't need to change their references. `IDataTreeView` is used only by `DataTreeModel` internally.

**[Risk] Phase 3 model/view split is large** → Mitigation: Phase 2 has already extracted 60% of the logic into collaborators. Phase 3 is primarily about moving those collaborators under `DataTreeModel` and introducing `SliceSpec` as the boundary type.

**[Risk] Performance regression from SliceSpec indirection** → Mitigation: `SliceSpec` is a lightweight data object (no allocations beyond the object itself). The expensive work (XML parsing, cache queries) happens exactly once regardless of whether the result is a `Slice` or a `SliceSpec`.

## Phased Implementation Plan

### Phase 0: Characterization Tests (1-2 days, low risk)

- Add ~20 tests to `DataTreeTests.cs` covering: XML→slice mapping, show-hidden toggle, slice reuse, PropChanged routing, navigation, DummyObjectSlice expansion, SliceFilter interaction
- Extend `Test.fwlayout` and `TestParts.xml` with test layouts for sequence properties (>20 items), nested headers, visibility="never" parts
- No production code changes
- **Exit criterion:** All new tests pass; no existing tests broken

### Phase 1: Partial-Class Split (1 day, very low risk)

- Split `DataTree.cs` into 7 partial-class files per the spec
- Split `Slice.cs` into 4-5 partial-class files
- Update `.csproj` if needed (SDK-style projects auto-include; verify)
- **Exit criterion:** `build.ps1` succeeds; all tests pass; `git diff --stat` shows only file renames/splits

### Phase 2: Extract Collaborators (3-5 days, low-medium risk)

Order of extraction (most valuable first):
1. **`SliceLayoutBuilder`** — extract `CreateSlicesFor`, `ApplyLayout`, `ProcessSubpartNode`, `AddSimpleNode`, `AddSeqNode`, `AddAtomicNode`, `EnsureCustomFields`, label/weight helpers. DataTree delegates to it. ~1,000 lines moved.
2. **`ShowHiddenFieldsManager`** — extract `GetShowHiddenFieldsToolName`, `HandleShowHiddenFields`, key resolution. ~100 lines. Already well-tested from LT-22427.
3. **`DataTreeNavigator`** — extract `CurrentSlice` logic, goto methods, focus helpers. ~150 lines.

Each extraction is a separate commit with its own test run. At this point, `SliceLayoutBuilder` can be unit-tested by providing mock `Inventory`, `LcmCache`, and `IFwMetaDataCache` — no `Form` required.

- **Exit criterion:** All characterization tests pass; `SliceLayoutBuilder` has dedicated unit tests; DataTree.cs is under 2,000 lines.

### Phase 3: Model/View Separation (5-8 days, medium risk)

1. Define `SliceSpec` data class
2. Define `IDataTreeView` interface
3. Create `DataTreeModel` composing `SliceLayoutBuilder` + `ShowHiddenFieldsManager` + navigation state
4. Modify `SliceLayoutBuilder` to produce `SliceSpec[]` instead of calling `SliceFactory.Create` directly
5. Modify `DataTree` to implement `IDataTreeView`: receive `SliceSpec[]` from model, call `SliceFactory.Create(spec)` to materialize, manage `ObjSeqHashMap` for reuse
6. Adapt `StTextDataTree` to override model-layer hook
7. Verify `RecordEditView` and `DTMenuHandler` still function (should require no changes if DataTree facade API is preserved)

- **Exit criterion:** All characterization tests pass; `DataTreeModel` has no `System.Windows.Forms` reference; existing integration tests pass; manual smoke test of LexEdit entry display

## Open Questions

- Should `SliceSpec` be a record type (C# 9+) or a plain class? Depends on .NET Framework 4.8 language version constraints.
- Should the `IDataTreeView` interface live in the DetailControls assembly or a separate abstraction assembly? If Avalonia implementation will be in a different project, a shared abstractions package may be needed.
- How should `DummyObjectSlice` lazy expansion interact with `SliceSpec`? The model could produce placeholder specs, or the view could handle laziness entirely.
