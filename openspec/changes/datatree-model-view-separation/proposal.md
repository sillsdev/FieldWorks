## Why

DataTree.cs (currently ~4.7k lines on this branch) is a God Class that fuses XML layout parsing, slice lifecycle management, WinForms layout/paint, focus navigation, mediator messaging, data-change notification, and persistence into a single `UserControl`. This makes it difficult to reason about in isolation and blocks straightforward reuse when the project migrates from WinForms to Avalonia. The same problem extends to Slice.cs. With the Avalonia migration on the roadmap, we need a UI-framework-agnostic model layer so both WinForms and Avalonia views can coexist during the transition period.

### Current implementation snapshot (2026-02-28)

- `IDataTreePainter` exists and is implemented by `DataTree`; painter seams are in place for offscreen/UI-adjacent tests.
- `DataTreeModel`, `IDataTreeView`, `SliceLayoutBuilder`, and `ShowHiddenFieldsManager` are **not** present yet in production code.
- `DataTree` remains a single concrete WinForms control file (`DataTree.cs`) rather than partial-class decomposition in production.

## What Changes

- **Extract `DataTreeModel`** — a new class (no WinForms dependency) owning XML layout interpretation, slice-spec construction, show-hidden-fields resolution, property-change routing, and navigation state. This is the "what to display" layer.
- **Extract `SliceLayoutBuilder`** — moves `CreateSlicesFor`, `ApplyLayout`, `ProcessSubpartNode`, `AddSimpleNode`, `AddSeqNode`, `AddAtomicNode`, `EnsureCustomFields` out of DataTree into a focused collaborator consumed by `DataTreeModel`.
- **Introduce `SliceSpec`** — a UI-framework-agnostic descriptor produced by `SliceLayoutBuilder`. Each `SliceSpec` captures label, indent, editor type, XML config, field ID, and object reference — everything needed for a view layer to materialize a concrete control.
- **Introduce `IDataTreeView`** — an interface that WinForms `DataTree` (and later Avalonia) implements, receiving `SliceSpec` lists from `DataTreeModel` and materializing them into platform controls.
- **Slim `DataTree`** to a WinForms `UserControl` that implements `IDataTreeView`: layout, paint, splitter management, control hosting. It delegates all "what" decisions to `DataTreeModel`.
- **Add characterization tests** (Phase 0) covering XML→slice-list mapping, show-hidden toggle, slice reuse, PropChanged→refresh, navigation, and DummyObjectSlice expansion — all *before* any structural changes.
- **Split DataTree.cs into partial-class files** (Phase 1) as a zero-risk mechanical step to isolate responsibilities before extraction.

These changes affect **managed C# code only** (Src/Common/Controls/DetailControls and Src/xWorks). No native C++ changes.

## Capabilities

### New Capabilities

- `datatree-model`: UI-framework-agnostic model layer that decides which slices to show, in what order, with what configuration — independent of WinForms or Avalonia.
- `datatree-characterization-tests`: Safety-net test suite covering DataTree's current behavior (XML parsing, show-hidden, slice reuse, navigation, lazy expansion) to reduce refactoring risk.
- `datatree-partial-split`: Mechanical decomposition of DataTree.cs and Slice.cs into partial-class files organized by responsibility, enabling targeted navigation and future extraction.

### Modified Capabilities

- `architecture/ui-framework/winforms-patterns`: DataTree transitions from monolithic UserControl to a thin view implementing `IDataTreeView`, delegating logic to `DataTreeModel`.
- `architecture/layers/layer-model`: Introduces a "detail-tree model" sublayer between UI shell and data access, formalizing the separation of "what fields to show" from "how to render them."

## Impact

- **Src/Common/Controls/DetailControls/**: DataTree.cs, Slice.cs, SliceFactory.cs gain new collaborators; file count increases but per-file complexity drops sharply.
- **Src/xWorks/RecordEditView.cs**: Primary DataTree consumer — must adapt to create `DataTreeModel` + `DataTree` (view). Public API (`ShowObject`, `Reset`, `CurrentSlice`) remains stable through facade methods.
- **Src/xWorks/DTMenuHandler.cs**: References `DataTree`; will need to accept `IDataTreeView` or continue referencing the concrete WinForms class during transition.
- **Src/LexText/Interlinear/InfoPane.cs**: Contains `StTextDataTree : DataTree` — the only subclass. Must be updated to override model-layer behavior rather than view-layer methods.
- **SliceFactory.cs**: Currently creates WinForms `Slice` objects directly. In the model/view split, it either produces `SliceSpec` descriptors (clean) or remains in the view layer (pragmatic). Decision deferred to design phase.
- **Test infrastructure**: New test fixtures in DetailControlsTests/ for characterization tests. Test XML layouts (Test.fwlayout, TestParts.xml) will be extended.
- **No breaking changes to external consumers** — all changes are internal to the DetailControls and xWorks assemblies.
