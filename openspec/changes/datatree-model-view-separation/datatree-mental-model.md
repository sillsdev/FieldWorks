# DataTree Mental Model

## Scope and status

This document explains the **current** runtime architecture around `DataTree` in FieldWorks and how the proposed model/view split would change it.

Current-state summary:

- `DataTree` is the central WinForms detail editor engine (`Src/Common/Controls/DetailControls/DataTree.cs`).
- `RecordEditView` is the primary host/orchestrator (`Src/xWorks/RecordEditView.cs`).
- `DTMenuHandler` drives many edit commands using the current slice/object (`Src/xWorks/DTMenuHandler.cs`).
- `SliceFactory` converts XML editor tokens into concrete slice/control types (`Src/Common/Controls/DetailControls/SliceFactory.cs`).
- `ObjSeqHashMap` handles strict and type-based slice reuse (`Src/Common/Controls/DetailControls/ObjSeqHashMap.cs`).
- `IDataTreePainter` exists as a view seam for line painting.
- Proposed `DataTreeModel` / `IDataTreeView` / `SliceSpec` are **not implemented yet** in production code on this branch.

---

## Big picture: what problem this subsystem solves

`DataTree` takes:

1. LCM domain data (`ICmObject`, `LcmCache`, metadata/flids),
2. XML layout/part definitions (`Inventory` lookups), and
3. runtime UI state (`PropertyTable`, current selection, show-hidden toggles),

and produces a **live editable tree of WinForms controls** (“slices”) that users can navigate and edit.

So this is a transformation pipeline from:

`Domain object graph + XML config + UI state` → `slice list + controls + interaction state`

---

## Main data pathways and conversions

## 1) View initialization pathway (host wiring)

Entry point: `RecordEditView.SetupDataContext()`.

What happens:

1. Creates persistence context key (per vector/view).
2. Assigns `PersistenceProvider` into `DataTree`.
3. Loads layout and part inventories via `Inventory.GetInventory("layouts", db)` and `Inventory.GetInventory("parts", db)`.
4. Calls `DataTree.Initialize(cache, layouts, parts)` and `DataTree.Init(mediator, propertyTable, config)`.
5. Installs slice filter and context-menu handler.
6. Hosts DataTree in WinForms control tree.

Conversions:

- XML inventory files → `Inventory` in-memory indexes.
- View config XML (`m_configurationParameters`) → runtime context (`layout`, `layoutChoiceField`, `filterPath`, persistence key).

---

## 2) Object-to-slices pathway (core render pipeline)

Entry point: `DataTree.ShowObject(root, layoutName, layoutChoiceField, descendant, suppressFocusChange)`.

What happens:

1. Loads show-hidden flags from `PropertyTable` (keyed by current tool).
2. Restores/snapshots current-slice identity metadata.
3. Decides create vs refresh:
   - root changed → `CreateSlices(true)`
   - same root, descendant changed → adjust target slice
   - otherwise → `RefreshList(false)`
4. Schedules focus restoration via mediator idle queue.

`CreateSlices(...)` then:

1. Builds reuse map from current slices (`ObjSeqHashMap`).
2. Calls `CreateSlicesFor(...)` recursively to materialize new/updated slices.
3. Removes unused slices, reapplies tooltip state, resets tab indices.

Recursive layout pipeline:

- `CreateSlicesFor` chooses template via `GetTemplateForObjLayout`.
- `ApplyLayout` iterates layout child nodes.
- `ProcessPartRefNode` resolves `<part>` / `<sublayout>`.
- `ProcessSubpartNode` dispatches to:
  - `AddSimpleNode` (`<slice>`)
  - `AddSeqNode` (`<seq>`)
  - `AddAtomicNode` (`<obj>`)

Key conversions in this pipeline:

- `ICmObject` + field name strings → FLIDs (`GetFieldId2`) and HVOs.
- XML nodes (`layout`, `part`, `slice`, `seq`, `obj`) → slice construction decisions.
- Sequence properties (`VecProp`) → managed `int[]` HVO arrays (`SetupContents`).
- Editor token string (`editor="..."`) → concrete slice class via `SliceFactory.Create`.
- Path stack (`XmlNode` + HVO sequence) → `slice.Key` object[] (identity/reuse key).

---

## 3) Editing pathway (user interaction → domain update)

The right-side control in each `Slice` edits LCM-backed properties.

Typical flow:

1. User edits a field in a slice control.
2. Control writes through LCM interfaces (`DomainDataByFlid` / object API).
3. `DataTree` receives property change notifications (for monitored `(hvo, flid)` pairs).
4. `PropChanged` decides refresh strategy:
   - monitored property: `RefreshListAndFocus` (possibly deferred via `BeginInvoke`)
   - undo/redo structural changes: force broader refresh and focus update
5. `RefreshList(...)` reuses slices where possible and rebinds current slice.

Important state machine pieces:

- `DoNotRefresh` + `RefreshListNeeded` + `m_fPostponedClearAllSlices`
- `m_postponePropChanged` (defer refresh to avoid re-entrancy crashes)
- current-slice snapshot fields (`m_currentSlicePartName`, `m_currentSliceObjGuid`, etc.)

---

## 4) Menu-command pathway (DTMenuHandler)

`DTMenuHandler` is tightly coupled to `DataTree`/`CurrentSlice`.

What it does:

- Reads `CurrentSlice`, `Root`, `Slices`, and sometimes `FieldAt(...)`.
- Runs commands like insert/copy/move/delete/edit/add-reference.
- Uses `FindMatchingSlices` and current object/slice identity to keep selection stable after mutations.

This is a major coupling seam that the future split must preserve via stable facade behavior.

---

## 5) Persistence/customization pathway

Two persistence layers are active:

1. **UI/session properties** (`PropertyTable` + `PersistenceProvider`)
   - show-hidden toggles
   - splitter base distance (`PersistPreferences`/`RestorePreferences`)
   - current-slice recall keys

2. **Layout overrides** (`Inventory.PersistOverrideElement`)
   - custom field placeholder corrections
   - generated/modified layout nodes persisted to override storage

So user-visible structure is partly from shipped XML and partly from persisted overrides.

---

## 6) Painting/layout pathway (WinForms-specific view mechanics)

`DataTree` remains a WinForms view container:

- performs layout and scroll behavior for slices,
- manages splitter/indent positioning,
- paints separator lines (`Painter.PaintLinesBetweenSlices(...)`),
- coordinates focus and tab ordering.

This view work is intentionally platform-specific and a good candidate to remain in a view class after split.

---

## 7) Sequence handling and lazy materialization

In large sequences (`kInstantSliceMax = 20`), `AddSeqNode` may create `DummyObjectSlice` placeholders instead of eagerly creating full slice subtrees.

Why:

- startup/render cost control,
- preserving responsiveness,
- deferring materialization until needed.

This behavior is subtle and test-sensitive; it should be preserved or intentionally redesigned with explicit contracts.

---

## Module-by-module responsibilities and held data

| Module | Performs | Holds key data/state |
|---|---|---|
| `RecordEditView` | Host/orchestrator for DataTree lifecycle; wires inventories, mediator, filters, menu handler | `m_dataEntryForm`, layout names, config XML, persistence context |
| `DataTree` | End-to-end pipeline from object/layout to editable slice controls; refresh, selection, messaging, paint/layout | cache, metadata cache, inventories, current root/descendant/slice, monitored props set, show-hidden flag, refresh deferral flags |
| `Slice` (+ subclasses) | Row abstraction: left label/tree node + right editor control; local UX behavior | object context, flid/config nodes, key path, parent slice, mediator/property table/cache refs |
| `SliceFactory` | Maps editor tokens and context into concrete slice classes | conversion logic from XML attributes (`editor`, `ws`, etc.) to constructors |
| `ObjSeqHashMap` | Reuse index for slices by strict path key and by type name | hashtable keyed by path lists, type-name reusable lists |
| `Inventory` (XCore) | Lookup/unify/persist XML layouts/parts | in-memory index of XML elements and persisted override handling |
| `DTMenuHandler` | Command and context-menu logic tied to current DataTree selection | `m_dataEntryForm`, command context, temporary move/copy state |
| `StTextDataTree` (`InfoPane`) | Specialized DataTree behavior for interlinear text roots/selection | root transformation logic before base `ShowObject` |

---

## Where native `Src/views` / “Views.cpp” fits

I found **no direct references** between `DataTree`/`RecordEditView`/`DTMenuHandler` and the native `Src/views` C++ layer in this branch (symbol search in `Src/views/**` for those types is empty).

Interpretation:

- This specific DataTree pipeline is primarily managed WinForms + LCM + XML inventory logic.
- Native `Src/views` still underpins rendering/editor infrastructure elsewhere in FieldWorks, but this proposed split should not require direct C++ `views` API changes unless a managed contract currently hiding that dependency is altered.

Practical impact expectation: **minimal/no direct `Src/views` code impact** for the planned DataTree model/view split.

---

## Is the split plan sensible?

Short answer: **yes**, with the right boundary.

Why it is sensible:

1. Current class mixes three concerns: decision logic, UI materialization, and UI rendering.
2. XML + domain traversal logic is deterministic and testable without WinForms.
3. Existing painter seam (`IDataTreePainter`) already demonstrates successful decoupling in one area.
4. Host and menu consumers can keep using a stable DataTree facade while internals move.

Main risk:

- preserving behavior around slice reuse keys, deferred refresh semantics, and lazy dummy-slice expansion.

Mitigation:

- characterization tests around those exact behaviors before/through extraction.

---

## How it would look after split (target shape)

## Target architecture

- **DataTreeModel (no WinForms):**
  - owns `ShowObject` decision state, monitored properties, and XML/layout traversal outputs
  - emits framework-agnostic slice descriptors (`SliceSpec`)
- **SliceLayoutBuilder (pure logic collaborator):**
  - owns `CreateSlicesFor` / `ApplyLayout` / `ProcessSubpartNode` style traversal logic
  - converts `(ICmObject + XmlNode + metadata)` → `SliceSpec` graph/list
- **DataTreeView (WinForms DataTree facade):**
  - materializes specs into concrete `Slice` controls via factory
  - owns WinForms layout, paint, scroll, splitter, focus plumbing
- **Reuse manager (existing ObjSeqHashMap semantics):**
  - preserved in view/materialization layer, keyed by spec path identity

## Data flow then

1. Host asks model to build specs.
2. Model reads cache + inventory + property state, returns ordered `SliceSpec` list.
3. View diffs specs against existing controls using reuse map.
4. View creates/rebinds/positions controls and restores focus/scroll.

This keeps “what to show” independent of UI toolkit while preserving existing behavior contracts for consumers.

---

## Suggested incremental extraction order

1. Extract `SliceLayoutBuilder` methods first (highest complexity, purest logic value).
2. Keep DataTree as façade that delegates into builder but still materializes controls.
3. Introduce `SliceSpec` output mode and adapter in DataTree.
4. Introduce `DataTreeModel` orchestrating selection/refresh decisions.
5. Keep `DTMenuHandler` and `RecordEditView` APIs stable until final phase.

This sequence reduces risk while gradually introducing the end-state boundaries.
