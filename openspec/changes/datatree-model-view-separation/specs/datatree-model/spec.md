## Status Note (Current vs Target)

This document defines the **target-state requirements** for the model/view separation.
On the current branch snapshot (2026-02-28), `DataTreeModel`, `IDataTreeView`, `SliceLayoutBuilder`, and `ShowHiddenFieldsManager` are not yet implemented in production code; `IDataTreePainter` is implemented.

## ADDED Requirements

### Requirement: DataTreeModel owns slice-specification logic

A new class `DataTreeModel` SHALL exist with no dependency on `System.Windows.Forms`. It SHALL own the decision of which slices to display, in what order, with what configuration — given a root `ICmObject`, a layout name, and a layout-choice field.

#### Scenario: DataTreeModel produces slice specifications without WinForms
- **WHEN** `DataTreeModel.BuildSliceSpecs(root, layoutName, layoutChoiceField)` is called
- **THEN** it returns an ordered list of `SliceSpec` descriptors without creating any WinForms controls

#### Scenario: DataTreeModel is testable without a Form
- **WHEN** a unit test constructs a `DataTreeModel` with cache, layout inventory, and part inventory
- **THEN** no `Form`, `UserControl`, or WinForms host is required for the test to run

### Requirement: SliceSpec descriptor captures all slice metadata

A new class `SliceSpec` SHALL capture the information needed to materialize a slice in any UI framework: label, abbreviation, indent level, editor type, XML configuration node, field ID, object reference, visibility, weight, tooltip, and key path.

#### Scenario: SliceSpec contains editor type
- **WHEN** a `SliceSpec` is produced from a `<part>` with `editor="multistring"`
- **THEN** `SliceSpec.EditorType` equals `"multistring"`

#### Scenario: SliceSpec contains label and indent
- **WHEN** a `SliceSpec` is produced from an indented part with `label="Citation Form"`
- **THEN** `SliceSpec.Label` equals `"Citation Form"` and `SliceSpec.Indent` reflects the nesting depth

### Requirement: SliceLayoutBuilder performs XML layout interpretation

A new class `SliceLayoutBuilder` SHALL encapsulate the XML layout parsing logic currently in `DataTree.CreateSlicesFor`, `ApplyLayout`, `ProcessSubpartNode`, `AddSimpleNode`, `AddSeqNode`, and `AddAtomicNode`. It SHALL produce `SliceSpec` lists rather than concrete `Slice` WinForms controls.

#### Scenario: SliceLayoutBuilder interprets sequence nodes
- **WHEN** a layout contains `<seq field="Senses">` and the entry has 3 senses
- **THEN** `SliceLayoutBuilder` produces `SliceSpec` entries for each sense's sub-layout

#### Scenario: SliceLayoutBuilder handles ifData visibility
- **WHEN** a part has `visibility="ifdata"` and the field is empty
- **THEN** `SliceLayoutBuilder` excludes that `SliceSpec` from the output (unless show-hidden is on)

### Requirement: ShowHiddenFieldsManager encapsulates show-hidden key resolution

A new class `ShowHiddenFieldsManager` SHALL own the logic for resolving the tool-specific property key used for the "Show Hidden Fields" toggle. It SHALL be consumed by both `DataTreeModel` and `IDataTreeView` implementations.

#### Scenario: Resolves key from currentContentControl
- **WHEN** `currentContentControl` is `"lexiconEdit"` and the root is `ILexEntry`
- **THEN** `ShowHiddenFieldsManager.GetKey()` returns `"ShowHiddenFields-lexiconEdit"`

#### Scenario: Falls back to lexiconEdit for LexEntry roots
- **WHEN** `currentContentControl` is not set and the root is `ILexEntry`
- **THEN** `ShowHiddenFieldsManager.GetKey()` returns `"ShowHiddenFields-lexiconEdit"`

### Requirement: IDataTreeView interface for platform-specific rendering

An interface `IDataTreeView` SHALL define the contract between `DataTreeModel` and platform-specific view implementations. It SHALL include methods for materializing `SliceSpec` lists into visible controls, managing focus, and reporting user interactions.

#### Scenario: WinForms DataTree implements IDataTreeView
- **WHEN** the existing `DataTree : UserControl` is adapted
- **THEN** it implements `IDataTreeView` and delegates "what to show" decisions to `DataTreeModel`

#### Scenario: IDataTreeView does not expose WinForms types
- **WHEN** `IDataTreeView` is defined
- **THEN** it references only framework-agnostic types (`SliceSpec`, `ICmObject`, etc.) — no `Control`, `Form`, or `UserControl` in the interface

### Requirement: DataTree delegates to DataTreeModel

The existing `DataTree` WinForms class SHALL delegate `ShowObject`, show-hidden resolution, XML layout interpretation, and navigation state to `DataTreeModel`. It SHALL retain only WinForms-specific responsibilities: `OnLayout`, `OnPaint`, splitter management, `Control` hosting, and `SplitContainer` configuration.

#### Scenario: ShowObject delegates to model
- **WHEN** `DataTree.ShowObject(root, layout, ...)` is called
- **THEN** it calls `DataTreeModel.BuildSliceSpecs(...)` and materializes the resulting `SliceSpec` list into WinForms `Slice` controls

#### Scenario: DataTree retains WinForms layout
- **WHEN** the WinForms layout engine calls `OnLayout`
- **THEN** `DataTree` positions its child `Slice` controls using WinForms-specific APIs without involving `DataTreeModel`

### Requirement: StTextDataTree subclass compatibility

The `StTextDataTree` subclass in `InfoPane.cs` SHALL continue to function. Its overrides of `ShowObject` and `SetDefaultCurrentSlice` SHALL be adapted to work with the model/view split.

#### Scenario: StTextDataTree overrides model behavior
- **WHEN** `StTextDataTree` needs to transform the root object before display
- **THEN** it overrides a model-layer method (or hook) rather than a view-layer `ShowObject`

### Requirement: Slice reuse remains functional

The `ObjSeqHashMap`-based slice reuse mechanism SHALL continue to work during `RefreshList`. `SliceSpec` keys SHALL be compatible with the existing `Slice.Key` array structure.

#### Scenario: Refresh reuses existing slices
- **WHEN** `RefreshList` is called on the same object
- **THEN** slices whose `SliceSpec.Key` matches an existing slice are reused, not recreated

## MODIFIED Requirements

### Requirement: WinForms patterns — DataTree composition

*(Modified from `architecture/ui-framework/winforms-patterns`)*

DataTree SHALL follow a model/view composition pattern: `DataTreeModel` (UI-agnostic) decides what to display; `DataTree` (WinForms `UserControl`) renders it. This replaces the current monolithic pattern where a single `UserControl` owns both logic and rendering.

#### Scenario: DataTree is a thin view
- **WHEN** a developer reads `DataTree.cs` (the WinForms view)
- **THEN** they see only WinForms layout, paint, and control hosting — no XML parsing or show-hidden logic

## MODIFIED Requirements

### Requirement: Layer model — detail-tree model sublayer

*(Modified from `architecture/layers/layer-model`)*

A "detail-tree model" sublayer SHALL exist between the UI shell and data access layers. `DataTreeModel` and `SliceLayoutBuilder` reside in this sublayer. They depend downward on `LcmCache` and `Inventory` (data access / configuration) and are consumed upward by `IDataTreeView` implementations (UI shell).

#### Scenario: Model layer has no UI dependency
- **WHEN** the `DataTreeModel` class is compiled
- **THEN** it does not reference `System.Windows.Forms`, `Avalonia`, or any UI framework assembly
