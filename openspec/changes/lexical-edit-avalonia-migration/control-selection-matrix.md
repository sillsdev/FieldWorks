# Control Selection Matrix for Dense Tree/Table Surfaces (Task 7.6)

> **Status: RECOMMENDED, NOT DECIDED — pending owner sign-off.**
> This matrix records the evidence and the recommended direction per surface type. No control
> choice below is a committed product decision until the owning reviewer signs off and the
> corresponding region manifest names the control as an allowed dependency. Date: 2026-06-09.

## Scope

Lexical Edit needs three distinct dense surfaces (design.md decisions 3 and the §5 table/tree
slice in `architecture-diagrams.md`):

1. **Detail-view nested slices** — the `DataTree` replacement: a long, mixed-height, indented
   list of labeled editors (the legacy surface flattens the conceptual tree into a sequence of
   `Slice` rows with indent metadata; production-like fixtures run 68–253 slices, see task 2.13).
2. **Browse/table view** — the XMLViews `BrowseViewer` replacement: thousands to tens of
   thousands of rows, column headers, sort/filter, multi-writing-system cells (task 7.1).
3. **Tree-with-translations** — TreeView-heavy chooser/semantic-domain/sense surfaces with
   compact multi-writing-system node templates (task 6.4 spike).

Candidates compared: stock Avalonia `TreeView`, `TreeDataGrid` (separate
`Avalonia.Controls.TreeDataGrid` package), `ItemsRepeater` (separate
`Avalonia.Controls.ItemsRepeater` package), `ListBox` with virtualization
(`VirtualizingStackPanel` items panel), and a FieldWorks-owned virtualized control built on
Avalonia primitives.

Fixed constraints that shape every row (see `seam-recommendations.md` "Coexistence constraints"):

- **Avalonia is pinned to the 11.x line until WinForms is removed.** Anything that requires
  Avalonia 12 features, or a package version that only supports 12, is unavailable for the
  whole coexistence phase (~1 year+).
- **TreeDataGrid went commercial in October 2025** (per `architecture-diagrams.md` §8). The last
  permissively-licensed releases predate that and are effectively frozen; new fixes land on the
  commercial line.
- Multi-writing-system content means per-cell/per-run font, size, and direction differences,
  so every dense surface needs full data-template control inside rows/cells, and rows are
  **not uniform height**.

## Decision Matrix

Scale: **Good** / **Partial** / **Poor** / **N/A**, with the load-bearing detail in the cell.

| Criterion | Avalonia `TreeView` | `TreeDataGrid` | `ItemsRepeater` | `ListBox` + virtualization | FieldWorks-owned virtualized control |
|---|---|---|---|---|---|
| **Information density control** | Partial — templated items, but per-level container chrome (expander margins, item padding) is themed for general UI, needs heavy restyling to hit FLEx density | Good — designed for dense rows; row height and cell padding controllable | Good — no chrome at all; layout is whatever the template says | Partial/Good — item container chrome must be restyled (padding, selection visuals), then density is template-controlled | Good — density is a first-class design input (DPI density measurement is already an open spike item) |
| **Virtualization** | **Poor — `TreeView` does NOT virtualize.** Expanded hierarchies realize every container; unusable for unbounded trees and a known perf cliff on the 11.x line | Good — row virtualization for both flat and hierarchical sources is its core feature | Partial — virtualizing layouts exist, but recycling with mixed-height items and bring-into-view behavior need per-surface validation | Good — `VirtualizingStackPanel` is the stock, maintained virtualization path in 11.x; flat lists only (hierarchy must be flattened in the model) | Good (by construction) — built over `VirtualizingStackPanel`/`ItemsControl` primitives or an owned realization window; cost is ours |
| **Cell/row templating for multi-WS content** | Good — full `TreeDataTemplate` per node; compact multi-WS node templates are possible (the 6.4 spike) | Partial — `TemplateColumn` allows arbitrary cell content, but the column model assumes per-column uniformity; per-row WS-driven structure variance fights the model; editing templates are weak | Good — arbitrary templates, nothing imposed | Good — arbitrary `DataTemplate` per item; a slice row template can host any FieldWorks editor control | Good — templates and the editor registry (`ILexicalEditorRegistry`) are the design center |
| **Selection model** | Partial — single selection solid; multi-select and programmatic selection of unrealized nodes are weak spots | Good — flat/hierarchical selection models including multi-select and cell selection | **Poor — none.** No selection concept; everything hand-built | Good — stock single/multiple selection, `SelectionModel`, programmatic selection works with virtualization | Partial — must be built, but can implement exactly the legacy semantics (current-slice highlight, browse-row check/multi-select) over a managed selection service (diagram §5 "Managed selection") |
| **Accessibility / automation peers** | Good — stock peers expose tree item pattern, expand/collapse, names; works with `AutomationProperties.AutomationId` stamping (6.9) | Partial/Poor — automation peer coverage is a known weak area (noted in `architecture-diagrams.md` §8: "weak editing/accessibility"); gaps would have to be patched on a commercial codebase | **Poor — no items-control peers**; every UIA pattern hand-written | Good — stock `ListBox`/`ListBoxItem` peers, selection patterns, scroll patterns work today and are what task 7.11 UIA parity evidence would exercise | Partial — peers must be authored, but FieldWorks controls already stamp stable automation metadata (`LexicalEditPreviewTests` and the shared-region suites), and owning the peers means the 7.11 automation-tree parity gate is satisfiable by design rather than by upstream goodwill |
| **Keyboard navigation** | Good — arrows/expand/collapse/typeahead stock | Partial — grid navigation present; editing-mode keyboard flow (F2/Enter/Tab-through-cells) is immature | **Poor — none** | Good — stock up/down/home/end/page/typeahead; left/right and expand/collapse for a flattened tree must be added at the surface level | Partial — built to match legacy DataTree/Browse keyboard contracts exactly (which stock controls would also need overriding to achieve) |
| **Licensing / version availability on pinned 11.x** | Good — in-box, MIT, every 11.x release | **Poor — commercial since Oct 2025.** Staying free means pinning a frozen pre-commercial MIT version with no fixes for the entire coexistence phase; paying adds procurement + redistribution questions for an open-source FLEx | Partial — MIT, but **upstream is retiring/deprecating it** (diagram §8); it ships separately from core and gets minimal attention on 11.x | Good — in-box, MIT, the most-exercised virtualization path in the framework | Good — our license, our timeline; only depends on in-box primitives that survive the 11.x→12 jump |
| **Maintenance risk** | Low (control) / High (consequence) — control is maintained, but the no-virtualization design forces item-count caps forever | High — single-vendor commercial dependency, or frozen MIT fork we patch ourselves (a hard fork in all but name, contrary to proposal.md dependency guidance) | High — deprecated upstream; building on it guarantees a forced rewrite | Low — core control, used by everything, survives framework upgrades | Medium — we own the code (cost), but it is small code over stable primitives, and FieldWorks-owned dense controls are already the stated direction (design.md decision 3) |

### Notes on candidates not in the matrix

- **Avalonia `DataGrid`** (separate `Avalonia.Controls.DataGrid` package) was considered for the
  browse surface but is excluded from the matrix: it is a port with long-standing virtualization
  and editing bugs, is in maintenance mode upstream, and offers nothing over
  `ListBox`-with-owned-headers for our template-heavy cells. It can be revisited if upstream
  investment resumes.
- **`TreeView` + manual flattening** is not a distinct option: once the hierarchy is flattened
  into the items source, the control *is* a `ListBox` with indent templating — which is the
  `ListBox` column.

## Recommendation per Surface Type

> All three recommendations follow one principle: **flatten in the model, virtualize with stock
> primitives, own the row.** The typed IR already supports this — `ViewNode` carries `Indented`,
> `Expansion`, and `StableId`, and the legacy `DataTree` itself renders a *flattened* slice
> sequence, so a flat virtualized list with indent metadata is the faithful projection of the
> legacy surface, not a compromise.

### 1. Detail-view nested slices (DataTree replacement)

**Recommended: FieldWorks-owned slice list control built over `ListBox`/`ItemsControl` +
`VirtualizingStackPanel`** (i.e., the "ListBox with virtualization" column hardened into an
owned control). The region model already projects the IR tree to a flat sequence; expansion
state lives in the model (`ViewExpansion`), indent is template data, and each row's editor
comes from the editor registry. Mixed row heights (multi-WS multistring slices) are the main
virtualization risk — validate `VirtualizingStackPanel` estimated-size/bring-into-view behavior
against the 253-slice fixture before committing (feeds the 7.7 budgets; legacy baseline:
2483 ms open / 253 slices, budget rule in `region-manifest.md` §5).

- `TreeView`: rejected — no virtualization, and the surface is not visually a tree anyway.
- `TreeDataGrid`: rejected — no columns here, licensing cost buys nothing.
- Raw `ItemsRepeater`: rejected — deprecated upstream; no selection/peers.

### 2. Browse/table view (XMLViews replacement, task 7.1)

**Recommended: FieldWorks-owned virtualized table — flattened row list over
`VirtualizingStackPanel` with an owned shared-scope column header bar and owned cell layout
(uniform column grid via a lightweight panel), stock `ListBox` selection.** Row counts (10k+)
make virtualization non-negotiable; multi-WS cells make template control non-negotiable; the
filter bar/header reachability already baselined by `WinFormsUiaSmokeTests` must be
reproducible, which favors owned peers over `TreeDataGrid`'s weak ones.

- `TreeDataGrid` is the *technically* closest stock fit (it is the only candidate with columns
  + virtualization out of the box) and remains the named **pivot option** below — its rejection
  here is driven by the Oct 2025 commercial licensing on the pinned 11.x line plus weak
  editing/accessibility, not by capability.
- `ListBox` alone: insufficient — no column infrastructure; that is exactly the part we own.

### 3. Tree-with-translations (choosers, semantic domains, sense trees — task 6.4)

**Split recommendation by boundedness:**

- **Bounded popup trees** (morph-type chooser, small possibility lists; tens to a few hundred
  nodes): **stock `TreeView` with compact multi-WS `TreeDataTemplate`s**, with an explicit,
  manifest-recorded item-count ceiling (proposed: ≤ 500 realized nodes) because `TreeView` does
  not virtualize. This is the cheapest path to chooser parity (6.3) and keeps stock
  accessibility/keyboard behavior.
- **Unbounded or large trees** (full semantic domain list ~1800+ nodes with translations,
  reversal trees): **the owned flattened virtualized list from surface 1, plus
  expander/indent row chrome** — same control, tree-shaped projection, model-owned expansion.

The 6.4 spike should explicitly measure where the `TreeView` ceiling really is on 11.x at
100%/150% DPI before the ceiling number is frozen.

## Pivot Triggers

Revisit this matrix (and re-run the spike evidence) if any of the following occurs:

1. **TreeDataGrid licensing changes** — relicensed permissively, a maintained community fork
   emerges, or SIL accepts the commercial license **and** upstream closes the editing +
   automation-peer gaps: re-evaluate it for the browse/table surface before building owned
   column infrastructure (or to replace it if ours underperforms).
2. **`VirtualizingStackPanel` fails the mixed-height fixtures** — scroll/expand or open-time
   budgets (7.7) miss on the 253-slice or 10k-row fixtures, or bring-into-view/focus-restore
   misbehaves with variable row heights: escalate to a fully owned realization-window
   virtualizer (the "owned control" column without stock panel reuse).
3. **`TreeView` gains virtualization on a consumable 11.x release** (or the post-WinForms
   Avalonia 12 upgrade lands with it): raise/remove the bounded-tree ceiling and reconsider
   `TreeView` for large trees.
4. **`ItemsRepeater` is un-deprecated** with a maintained virtualization story: reconsider as
   the substrate for the owned controls (it would reduce owned layout code).
5. **Owned-control cost overruns** — if the owned table's selection/peers/keyboard work exceeds
   the spike budget materially, re-open the TreeDataGrid commercial option with the actual
   measured cost as the comparison baseline rather than an estimate.
6. **Accessibility gate failure** (task 7.11) on any stock control we adopted: owning the
   surface's peers becomes mandatory, which collapses the choice back to the owned column.

## Relationship to other artifacts

- Task 6.4 (tree-with-translations spike) and 7.1 (virtualized table path) consume this
  matrix's recommendation and produce the evidence that confirms or pivots it.
- Region manifests (design decision 8) must name the chosen control per surface as an allowed
  dependency once the owner signs off.
- Performance budgets in `region-manifest.md` §5 (and 2.13 measured baselines) are the
  acceptance bar for whichever control is chosen — the control choice never relaxes the budget.
