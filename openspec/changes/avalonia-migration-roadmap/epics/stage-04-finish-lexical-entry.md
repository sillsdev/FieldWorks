# Stage 4 — Finish the Lexical / Advanced Entry surface (exemplar) (Epic — **finished spec**)

> Status: **implementation-ready**, mapped to the open `lexical-edit-avalonia-migration/tasks.md` items.
> Grounded in `reviews/stage-04-finish-lexical-entry.md`. Most of the surface is built; this epic closes
> the named residuals so the region becomes the copy-me reference.

## Epic
- **Summary:** Close the open lexical-edit items so the Advanced Entry region is 100% green and becomes the
  reference implementation Stages 6/8 clone.
- **Type:** Epic · **Labels:** `track-surfaces`, `lead-senior` · **Size:** L
- **Description:** Scope is **finish / re-home**, not build — the JSON serializer, 10k-row browse,
  custom-field/reference/picture/pronunciation rendering, and 11.x parity all exist (`[x]`). The real heart
  is **latency + 150% DPI budgets**, the **exemplar-quality contract**, and flipping the region-manifest §6
  rows from **Partial** (today's verdict: "Default stays Legacy").
- **Acceptance criteria:** region-manifest §6 all green; **exit gate = "manifest green + enable-able"**,
  explicitly **not** "enabled by default" (the latter inherits Stage 10/13 default-path validation).
- **Dependencies:** **3b (editable cells)** for the table re-home; foundation change owns 6.13 residuals.

## Sub-epics / stories (mapped to tasks.md)

### 4.1 Re-home entry tables onto the Stage-3 control  · Story · S — *task 7.x*
- `LexicalBrowseView` is built + 10k-row-proven (7.1 `[x]`); **remaining = re-home onto the Stage-3 owned
  control and prove at 150% DPI**. Blocks on **3b**; the rest of Stage 4 runs in parallel.

### 4.2 Latency + 150% DPI budgets  · Story · M — *tasks 2.13, 7.7* ⚠ hardware-gated
- Remaining 7.7 lanes: **scroll/expand, typing latency, realized-count, memory, cache-invalidation**
  (region-manifest §5.4). **Note:** 150% DPI + some latency lanes need **real scaled-display hardware**
  (manifest §5.4) — schedule against that constraint; not purely a code task.

### 4.3 Override migrator + user-override fixtures  · Story · M — *tasks 9.2, 9.3*
- JSON serializer **exists** (`ViewDefinitionJsonSerializer.cs`); **remaining = the override migrator**
  (sparse `StableId` patches, `canonical-view-definition-design.md` steps 1–3) and **user-override fixtures**
  (`override-fixtures.md`). Shipped layouts already proven (136 import).

### 4.4 Runtime-XML-disable for the gated surface  · Story · S — *task 9.4*
- Disable runtime XML for the gated migrated surface while retaining import/audit fallback.

### 4.5 Exemplar-quality contract  · Story · M — *task 18.11 (+ pairs with 8.9)*
- **Unify the dual projector** (`LexicalEditRegionMapper` thin path vs `FullEntryRegionComposer` 2524-line
  full path) into one shared structural projector, and **document `RegionViewingServices` + the plugin
  burn-down as the copy-me contract** — or Stages 6/8 clone a 2524-line composer. This is the single most
  important exemplar deliverable.

### 4.6 End-to-end IME wiring  · Story · S — *task 18.10*
- Wire `RegionImeCompositionState` into the owned editor end-to-end (explicit compose/cancel/commit on a
  realized surface, not just Avalonia's native TextBox IME).

### 4.7 Manifest green + evidence + full build/test  · Story · M — *tasks 10.8/10.10/18.12/18.13*
- Flip region-manifest §6 (Layout/Validation/Accessibility/Performance) from Partial; complete Path-3
  bundles per scenario; attach `wiring-review-checklist.md`; **run the full `./build.ps1` + `./test.ps1`
  native+managed traversal (18.12)** — the verification gate not yet run this pass.

## Notes / open questions
- "Rich references" in scope = **reference vectors, not rich text**; StText multi-paragraph + ORC editing
  are **Stage 9** (tasks 8.11, and the `RegionViewingServices` deferred-concerns list).
- Graphite/PDF default-path validation (10.4/10.5) blocks the **global default**, not this region (it's
  engine-isolated) — do **not** treat as a Stage 4 blocker.
- `architecture-patterns.md` cited JSON/browse code as done while a couple of tasks read open — reconciled
  here (serializer done; migrator/fixtures open).
