# FwFeatureStructureEditor — Design Note (Phase-1 §19b Stage 1)

A reusable, **LCModel-free** Avalonia control that edits a feature structure
(`FsFeatStruc`) over a feature system — the foundation for the inflection-feature
editor (MSA), the gloss/MGA editor (sense), and the phonological/inflection
feature dialogs. This stage ships **one control plus its tests**. It does **not**
wire LCModel or the consuming dialogs (later stages), but the seam is designed so
those stages drop in.

## 1. The WinForms truth (what this replaces)

| WinForms file | Role |
| --- | --- |
| `FeatureStructureTreeView.cs` | The hierarchical tree control (radio-image TreeView) used by the inflection-feature editor. The node model is the canonical truth. |
| `MsaInflectionFeatureListDlg.cs` | Hosts the tree for an MSA/POS; `BuildFeatureStructure` walks the chosen nodes into an `IFsFeatStruc` (recursive ascent: complex → nested FS, closed → `IFsClosedValue`, value → `ValueRA`). |
| `PhonologicalFeatureChooserDlg.cs` | A *flat* closed-feature list with a per-row value dropdown (BrowseViewer + floating combo). Same closed-feature/value semantics, no nesting. |
| `MasterInflectionFeatureListDlg.cs` | The "create a new feature definition" catalog (closed vs complex). The create-new affordance. |

### Node model (from `FeatureStructureTreeView.AddNode` + `FeatureTreeNodeInfo.NodeKind`)

- **Complex** (`IFsComplexFeature`): an expandable parent. Children come from
  `complex.TypeRA.FeaturesRS` — more closed and/or complex features (nests
  arbitrarily). Maps to `IFsComplexValue` whose `ValueOA` is a nested
  `IFsFeatStruc`.
- **Closed** (`IFsClosedFeature`): an expandable parent. Children are its
  symbolic values (`closed.ValuesSorted`) rendered as radio nodes, **plus a
  trailing "None of the above" radio** (the unspecified pick). Maps to
  `IFsClosedValue` whose `ValueRA` is the chosen `IFsSymFeatVal` (or none).
- **SymFeatValue** (`IFsSymFeatVal`): a terminal **radio**. Picking one
  **deselects its siblings** (`HandleCheckBoxNodes`) — *exactly one value per
  closed feature*. "None of the above" is a sibling radio that means
  *unspecified* (no spec emitted for that feature).

### Behaviors / workflows / edge cases observed

- Tree auto-sorts alphabetically and appends "None of the above" to every
  terminal (closed-feature) group; if nothing chosen, "None of the above" is
  pre-selected (`Sort` → `HandleCheckBoxNodes(null, noneOfTheAboveNode)`).
- Loading an existing FS marks the matching value nodes `Chosen` (radioSelected
  image) and recurses into complex values (`PopulateTreeFromFeatureStructure`).
- Output is depth-first recursive ascent: walk up the node's parent chain
  creating/locating the nested FS, then set the leaf value. An **empty** result
  (no value chosen anywhere) is valid → the FS is deleted / treated as null.
- Cardinality: a closed feature holds **0 or 1** value; a complex feature holds
  **0 or 1** nested structure. No multi-value lists.
- Duplicate / `AlreadyInTree` guard prevents infinite recursion when a complex
  feature's type references an ancestor feature.
- "Create new feature" is a separate catalog dialog
  (`MasterInflectionFeatureListDlg`), reached via a link, returning
  `DialogResult.Yes`; creating new *values* is done in the feature-system editor
  (not inline). → In the new control this is the **deferred create hook**.

The phonological dialog is the *flat* degenerate case (all closed, no nesting,
value picked from a dropdown rather than expanded radios). The same seam covers
it: a feature system with only top-level closed features.

## 2. The Avalonia control: `FwFeatureStructureEditor`

Built in pure C# (no XAML), a `Border` like `FwPosChooser`/`FwMsaGroupBox`,
LCModel-free, using `FwAvaloniaDensity` tokens and stable AutomationIds. Lives in
`Src/Common/FwAvaloniaDialogs` next to `FwMsaGroupBox` (the first stage-2/3
consumer is the MSA editor there). It is rendered as a **`TreeView`** whose:

- **Complex** rows render as expandable headers (a feature glyph + name).
- **Closed** rows render as expandable headers; their value children render as
  **radio buttons** (one logical group per closed feature). The trailing
  **"<None>"** radio is the unspecified pick.
- **SymFeatValue** rows render as radio buttons; checking one unchecks the
  group's siblings and commits the closed feature's value; the change event
  fires.

### 2a. The seam (LCModel-free input)

Mirrors the `FwPosNode` depth-tagged flat list, enriched with a node kind and a
value reference. The host (a later stage) builds these from the live feature
system in document order; the control folds them into the tree exactly like
`FwPosChooser` / `ChooserTreeBuilder`.

```csharp
public enum FwFeatureNodeKind { Complex, Closed, Value }

public sealed class FwFeatureNode
{
    public FwFeatureNode(string id, string name, FwFeatureNodeKind kind, int depth = 0);
    public string Id { get; }              // opaque stable id (guid string); round-tripped verbatim
    public string Name { get; }            // display text
    public FwFeatureNodeKind Kind { get; } // Complex | Closed | Value
    public int Depth { get; }              // 0 top-level; +1 per nesting (document order)
}
```

- A `Value` node attaches under the nearest shallower `Closed` node (its feature).
- A `Closed`/`Complex` node attaches under the nearest shallower `Complex`.
- The control **auto-appends a "<None>" Value** to each closed feature (the
  "None of the above" radio), so the host need not supply it.

### 2b. The current assignments (input) and the chosen set (output)

A feature assignment is a `(closedFeatureId → chosenValueId)` mapping; an
*unspecified* feature simply has no entry (or maps to null). The nesting is
implicit in the tree (a value's closed-feature parent, and that feature's complex
ancestors), so the assignment set itself is **flat** — the host reconstructs the
`IFsFeatStruc` nesting from the feature system, exactly as
`BuildFeatureStructure` does (recursive ascent). This keeps the control
LCModel-free and the seam tiny.

```csharp
public sealed class FwFeatureValueAssignment
{
    public FwFeatureValueAssignment(string closedFeatureId, string valueId);
    public string ClosedFeatureId { get; } // the IFsClosedFeature id
    public string ValueId { get; }         // the chosen IFsSymFeatVal id (never the <None> sentinel)
}
```

- **Input** (seed, silent): `void SetAssignments(IReadOnlyList<FwFeatureValueAssignment>)`
  marks the matching value radios and expands their ancestors. Does **not** raise.
- **Output**: `IReadOnlyList<FwFeatureValueAssignment> Assignments { get; }` — the
  current chosen set, one per closed feature that has a non-`<None>` value.
- **Empty / unspecified is valid**: a closed feature whose `<None>` radio is
  selected (or never touched) contributes **no** assignment. An entirely empty
  set is legal (the WinForms "delete the FS" case).

### 2c. API surface

```csharp
public sealed class FwFeatureStructureEditor : Border
{
    public FwFeatureStructureEditor(string automationId);

    // ---- seam ----
    public void SetNodes(IReadOnlyList<FwFeatureNode> nodes);          // feeds the feature system
    public void SetAssignments(IReadOnlyList<FwFeatureValueAssignment> assignments); // seeds (silent)
    public IReadOnlyList<FwFeatureValueAssignment> Assignments { get; } // output

    // ---- change + create-hook events ----
    public event Action<IReadOnlyList<FwFeatureValueAssignment>> AssignmentsChanged; // user pick
    public event Action CreateNewFeatureRequested;   // deferred wiring (Stage 2+)
    public event Action<string> CreateNewValueRequested; // arg = closed-feature id; deferred wiring

    // ---- create-hook acceptance (host calls back after its create flow) ----
    public void AcceptCreatedFeature(FwFeatureNode created, IReadOnlyList<FwFeatureNode> valueChildren = null);
    public void AcceptCreatedValue(string closedFeatureId, FwFeatureNode createdValue);

    // ---- programmatic gestures + test accessors ----
    public void RaiseCreateNewFeature();
    public void RaiseCreateNewValue(string closedFeatureId);
    public TreeView Tree { get; }
    public TextBox FilterBox { get; }      // type-ahead filter over feature/value names
    public Control FilterList { get; }     // flat results while filtering
    public Control CreateFeatureRow { get; }
}
```

### 2d. Create-hook deferral (mirrors `FwPosChooser.CreateNewPosRequested`)

The control performs **no** create. An inline "Create a new feature…" row at the
bottom of the tree raises `CreateNewFeatureRequested`; a per-closed-feature
"Add a value…" affordance raises `CreateNewValueRequested(closedFeatureId)`.
Stage 2+ wires these to the Avalonia replacements of `MasterInflectionFeatureListDlg`
/ the feature-system value editor, then calls `AcceptCreatedFeature` /
`AcceptCreatedValue` so the control adds + selects the new item. The control stays
LCModel-free (host supplies the already-built `FwFeatureNode`).

### 2e. Interaction details (parity with `FeatureStructureTreeView`)

- **Radio semantics per closed feature**: checking a value radio commits that
  feature's value and unchecks the group's siblings (incl. `<None>`); checking
  `<None>` clears the feature's assignment.
- **Expand/collapse**: complex and closed features expand to reveal children;
  seeding an assignment expands the chain to the chosen value (like
  `ExpandAncestors` in `FwPosChooser`).
- **Keyboard**: Up/Down move the tree highlight (TreeView native), Space/Enter
  toggles the highlighted value radio, type-ahead filter focuses on a flat
  result list (Up/Down/Enter there), Escape clears the filter.
- **Filtering**: a contains-match over node names; while filtering the tree
  hides and a flat depth-indented list shows (same pattern as `FwPosChooser`).

### 2f. Density / styling / AutomationIds

- All padding/indent/colors from `FwAvaloniaDensity` (`OptionItemPadding`,
  `TreeIndentPerLevel`, `RadioBoxSize`, `PickerBackgroundBrush`, etc.).
- AutomationId stem `"{automationId}.…"`: `.FeatureEditor` (root), `.Tree`,
  `.Search`, `.Filtered`, `.Node`, `.Value`, `.CreateFeature`,
  `.CreateValue` — nonlocalized constants, matching the kit convention.
- New localized strings (append-only) in `FwAvaloniaDialogsStrings`:
  `FeatureEditorNone` ("<None>", the unspecified radio, seeded from the WinForms
  "None of the above"/AddNotSureItem wording), `FeatureEditorCreateFeature`
  ("Create a new feature..."), `FeatureEditorCreateValueFormat`
  ("Add a value to {0}..."), `FeatureEditorName` (accessible name).

## 3. How stages 2+ wire it in

- **MSA inflection features**: host builds `FwFeatureNode`s from the POS's
  `InflectableFeatsRC` (complex/closed/values, depth-tagged, document order),
  seeds current `MsFeatures`, reads `Assignments` back, reconstructs the
  `IFsFeatStruc` via the existing `GetOrCreateValue` recursive-ascent logic, and
  wires `CreateNewFeatureRequested` to the Avalonia `MasterInflectionFeatureListDlg`
  replacement. Lives alongside `FwMsaGroupBox`.
- **Sense gloss / MGA**: same seam fed from the MGA feature set.
- **Phonological / inflection feature dialogs**: the flat case — a feature system
  of only top-level closed features; the tree degenerates to a flat radio list
  (or the host can present each closed feature's value via the same radios).

## 4. Test plan (the §19.0 rubric — see `feature-structure-test-research.md`)

T0 research matrix (traceability) → T1 unit (render, expand/collapse, pick +
emit, add/remove, empty/unspecified, create-requested, keyboard + filter) → T2
integration (alongside an `FwPosChooser` in one host panel — selections compose,
events don't cross-talk) → T3 edge (empty system, deep nesting, no value chosen,
rapid expand/collapse + pick, RTL/complex-script names, large system) → T4
workflow (open → expand complex → pick closed value → add a second feature → read
back) → T5 visual PNGs (`FwFeatureStructureEditor-01-initial`, `-02-expanded`,
`-03-value-picked`, `-04-multi-feature`) before `AssertNoCrowding`.

## 5. File ownership

Create `FwFeatureStructureEditor.cs` + `FwFeatureStructureEditorTests.cs` + the
two research/design notes; append-only strings. Do **not** modify
`FwMsaGroupBox.cs` / `FwPosChooser.cs` / `FwOptionPicker.cs` / `ChooserDialog*` /
`EntryGoDialog*` / style files — consume their seams; later stages wire.
