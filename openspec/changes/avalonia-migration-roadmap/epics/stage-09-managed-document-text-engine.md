# Stage 9 — Managed document/text rendering & editing engine (Views replacement) (Epic draft)

> JIRA-ready draft. Source of truth: `complete-migration-program.md` §6 Stage 9 + §11.1 (Graphite
> decision) + §7 (Definition of Done) + §10 (labels); `reviews/stage-09-managed-document-text-engine.md`;
> `reviews/00-cross-comparison-synthesis.md` §4/§6/§7. Stage 9 is the program's **long pole** and its
> **#1 correctness risk**. Decompose per synthesis §6 into one parent epic + five gated sub-epics
> (9.0 spike → 9.1 StText → 9.2 selection/caret → 9.3 layout/box → 9.4 embedded). **Scope = the DELTA
> over the landed field foundation (`FwMultiWsTextField`)** — single-paragraph multi-WS field editing is
> already done; do not re-litigate it. **Native `Src/views` deletion is Stage 13, not here** — Stage 9
> only severs the Avalonia path from native Views.

---

## Epic

**Summary:** Build the managed document/text rendering & editing engine that replaces the native C++
Views engine (`Src/views/`, `IVwRootBox`/`IVwSelection`/`IVwEnv`/box hierarchy) for FieldWorks'
**document / multi-paragraph / structured-text** surfaces — fully managed, no native Views, no Graphite.

**Type:** Epic (parent). Track III — long pole. Lead: **Senior** (with Claude).

**Labels:** `track-longpole`, `lead-senior`, `parity-blocked-by:views-engine`, `xl`.
(Sub-epics carry their own labels; see each below.)

**Description.**
Field-level multi-writing-system `ITsString` editing is **already managed and done** — `FwMultiWsTextField`
(stock Avalonia `TextBox`-per-WS with bolt-on bidi caret nav, grapheme-cluster selection, and `ITsString`
run write-back via `RegionRichTextEditAlgorithms`). That foundation **explicitly deferred** multi-paragraph
`StText` editing and embedded-object/ORC editing. Stage 9 is everything the field foundation is **not**:

- **Multi-paragraph `StText`** editing (paragraph model, split/merge, para-level properties).
- **Owned structured selection/caret model** replacing `IVwSelection`/`SelLevInfo`/`SelectionHelper`
  (~14.4K LOC in `VwSelection.cpp` + ~2K in `SelectionHelper`/`TextSelInfo`) — selection across nested
  objects and levels, the deepest correctness surface.
- **Owned `TextLayout`-based layout/box model** (HarfBuzz/Skia via Avalonia `TextLayout`) replacing the
  `VwParagraphBox`/`VwStringBox`/`VwGroupBox`/`VwTableBox`/`VwLazyBox` hierarchy where stock `TextBox`/
  `TextLayout` is insufficient for document surfaces (justified/complex multi-line, RTL paragraphs,
  drop-caps, tables, overlays).
- **Editable embedded objects/ORCs, footnotes, pictures, tables, overlays** (`VwTableBox`/`VwPictureBox`/
  ORC anchors/`IVwOverlay`).
- **The Stage 7A interlinear/sandbox constructs** the original scope text omitted: a secondary in-memory
  presentation cache/decorator editing model (replacing `CachePair`/`VwCacheDa`), icon/picture-anchored
  hit-test combo editing, and aligned multi-line interlinear grid layout. Stage 9 owns the **engine seams**;
  Stage 7A owns the interlinear-specific composition over them.

Fully managed only — **no Graphite, no native Views** on the Avalonia path. Complex-script shaping is
HarfBuzz/managed (Avalonia `TextLayout`). Editing routes through the existing seams (`IEditSession`,
`IUndoRedoCoordinator`) and the **one global LCModel undo stack** — never a parallel Avalonia history.
The forbidden-symbol audit (`EngineIsolationAuditTests`) stays green on every migrated surface — no
`IVwRootBox`/`IVwEnv`/`IVwGraphics`/`IRenderEngine`/`GraphiteEngineClass` in the Avalonia path.

**Acceptance criteria (epic-level).**
1. Multi-paragraph `StText` + structured editing reach parity on managed surfaces; **no native Views on
   the Avalonia path** (forbidden-symbol audit green).
2. **9.0 spike exits with a written go/no-go and a build-vs-extend decision** before 9.1+ open; spike
   includes mixed bidi, CJK IME, custom WS, multi-paragraph StText, cross-structure selection, **and a
   Sandbox/interlinear scenario**.
3. The **G0–G3 LDML coverage scan** ships from 9.0 with the **exact dropped-script list + affected
   projects** (the document/notify obligation input per resolved §11.1).
4. Owned selection/caret model achieves cross-level/cross-object selection parity against real fixtures
   (the ~14.4K-LOC `IVwSelection` replacement).
5. Editing routes through `IEditSession` + the single global LCModel undo stack; per-surface Path-3 parity
   bundle (semantic + visual + workflow + performance) at **100% and 150% DPI**; typing latency budgets
   committed and met (extended from the field foundation to multi-paragraph).
6. Every sub-epic satisfies the §7 per-surface Definition of Done; `./build.ps1` + `./test.ps1` green;
   retrospective folds lessons into the skill set in the **same** PR.
7. The **Stage 9 ↔ 7A engine seam** is explicitly stated and frozen before the 9.2/9.3 API is finalized.

**Dependencies.**
- **Depends on:** Stage 1 (platform/enablement kit). Runs in parallel with Track II.
- **Gates / blocks:** **Stage 7A** (interlinear + sandbox — hard-blocks on extended Stage 9); **Stage 6b**
  (morphology/grammar document editors — `S9 → S6`, the plan's single biggest missing graph edge);
  **Stage 5 Tier C** (`FwFindReplaceDlg`/`FwStylesDlg`, which host `IVwRootSite`/`SimpleRootSite` —
  re-tiered out of junior Stage 5); **Stage 4** only for the one deferred `StText` field (lexicon
  comments/notes), which **9.1** unblocks (Stage 4 ships those read-only until then); **Stage 8a**
  (Views-coupled list editors); **Stage 10B** (Graphite managed-path removal is gated on 9.0's coverage
  proof). Coordinate **9.3 ↔ Stage 3** (`VwLazyBox` lazy/virtualized box realization overlaps Stage 3's
  virtualization primitive — share the realization-window approach).
- **Not in scope (Stage 13):** native `Src/views` deletion, `IVwRootBox`/`IVwGraphics`/`IVwEnv` render-COM
  retirement, `ViewsInterfaces` split (keep `IVwCacheDa`), native `GraphiteEngineClass` deletion.

**Rough size:** **XL.** Multi-quarter senior effort; each of 9.1–9.4 is itself a substantial sub-epic
and 9.2 alone replaces ~14.4K LOC of cross-level selection logic.

---

## Sub-epics / stories

### 9.0 — Spike + G0–G3 coverage scan (GATE)

**Summary:** De-risk the #1 framework gap; decide build-vs-extend; enumerate the dropped-script list.
Blocking gate — **do not open 9.1+ until 9.0 exits.**

**Type:** Sub-epic (gate). **Build-vs-extend:** this sub-epic *produces* that decision.

**Description.** Run the named spike scenarios, each producing a Path-3 bundle or a written go/no-go (not
"looks fine"): (1) mixed bidi (Arabic/Hebrew + Latin), (2) CJK IME, (3) custom writing systems,
(4) multi-paragraph `StText` editing, (5) selection across structured content, **(6) a Sandbox/interlinear
scenario** exercising the Stage 7A constructs — in-memory presentation cache/`CachePair`/`VwCacheDa`
replacement, icon/picture-anchored hit-test combo editing, and aligned multi-line interlinear grid layout.
Also run the **G0–G3 LDML coverage scan** (salvage the classifier + `km.ldml` fixture from
`graphite-transition-support` tasks 1.3/1.4/3.2): scan project LDML for `IsGraphiteEnabled` + Graphite-only
feature strings and classify each WS as **G0** (dual-engine, OT-safe), **G2** (dual-engine but renders
differently under OT), or **G3** (Graphite-only, no OT path — e.g. Awami Nastaliq). The expected/recorded
build-vs-extend outcome for the spike to validate: **extend** the field foundation for 9.1; **build** an
owned `TextLayout`-based selection/layout layer for 9.2+ (the open Avalonia bidi/caret `TextBox` defects do
not scale to documents). IME/RTL evidence requires realized-window lanes, not headless alone.

**Acceptance criteria.**
- All six spike scenarios produce a Path-3 bundle or a written go/no-go; the Sandbox/interlinear scenario
  is included and passes (or its risks are explicitly recorded).
- A **build-vs-extend decision is recorded** (extend 9.1 / build owned layer 9.2+), with the spike either
  validating or revising the predicted outcome.
- The **G0–G3 coverage scan output** lists the exact G3 (Graphite-only) projects/scripts and affected
  writing systems — this is the **document-and-notify obligation input** for §11.1 (consumed by Stage 10B/13).
- Forbidden-symbol audit green on all spike artifacts.

**Dependencies.** Depends on Stage 1. **Gates all of 9.1–9.4.** Its outputs gate **Stage 10B** (coverage
proof) and de-risk **Stage 7A** (interlinear/sandbox scenario). The Stage 9↔7A seam is drafted here.

**Build-vs-extend callout:** N/A — this sub-epic decides it.

**Labels:** `track-longpole`, `lead-senior`, `gate`, `parity-blocked-by:views-engine`, `spike`. **Size: L.**

---

### 9.1 — Multi-paragraph `StText` editing

**Summary:** Close the one deferred string blocker — multi-paragraph `StText` editing (paragraph model,
split/merge, para-level properties). The smallest document-model step.

**Type:** Sub-epic. **Build-vs-extend: EXTEND.** Model a paragraph as a stack of the existing per-WS
editors over `FwMultiWsTextField` + `RegionRichTextEditAlgorithms` + `RegionImeCompositionState`. This is
the only sub-epic where extending the field foundation is viable.

**Description.** Add a managed paragraph/`StText` model with paragraph splitting/merging and paragraph-level
properties, layered on the existing single-paragraph field editing. StText edits are multi-paragraph LCModel
mutations — keep them inside the fenced `IEditSession` and route through the single global undo stack. Unblocks
lexicon comment/note fields (the Stage 4 deferred `StText` field) and Notebook detail.

**Acceptance criteria.**
- Multi-paragraph `StText` create/edit/split/merge with para-level props at parity on a target surface.
- Edits route through `IEditSession` + the single global LCModel undo/redo stack (no parallel history).
- Path-3 parity bundle (semantic + visual + workflow + performance) at 100% + 150% DPI; typing latency
  budget met. Forbidden-symbol audit green.

**Dependencies.** Gated on **9.0** exit. Unblocks the Stage 4 deferred `StText` field and Notebook detail.

**Build-vs-extend callout:** **EXTEND** the field foundation.

**Labels:** `track-longpole`, `lead-senior`, `parity-blocked-by:views-engine`. **Size: L.**

---

### 9.2 — Owned structured selection/caret model

**Summary:** Build the FieldWorks-owned managed selection/caret model replacing `IVwSelection`/`SelLevInfo`/
`SelectionHelper` — selection across nested objects and structural levels. The ~14.4K-LOC core and the
deepest correctness surface in the program.

**Type:** Sub-epic. **Build-vs-extend: BUILD.** Stock `TextBox` owns selection/caret and cannot express
cross-object selection or FieldWorks box semantics; the open Avalonia bidi/caret defects (reversed-selection
caret, Shift+Arrow, Home/End, word-jump, caret-under-selection) do not scale to documents. Use the native
`VwSelection`/`SelLevInfo` model as the **reference spec, not the implementation** — a clean C# rewrite.

**Description.** Own a managed caret/selection model above the rendering layer: insertion points, ranges,
and structured selections that span paragraph and embedded-object boundaries and nested structural levels.
Hit-testing, anchor/end, normalization, and selection-driven commands. Exercise selection across structured
content with real fixtures (the 9.0 spike's cross-structure scenario is the prototype). IME/RTL caret
behavior requires realized-window evidence.

**Acceptance criteria.**
- Cross-level / cross-object selection parity against real fixtures; insertion-point and range semantics
  match the legacy `IVwSelection` behavior captured in baselines.
- No reliance on stock `TextBox` selection/caret for document surfaces.
- Realized-window evidence for IME/RTL caret behavior; Path-3 bundle at 100% + 150% DPI. Forbidden-symbol
  audit green.

**Dependencies.** Gated on **9.0** exit. Consumed by **9.3**, **9.4**, **Stage 7A**, **Stage 6b**.

**Build-vs-extend callout:** **BUILD** an owned selection/caret layer.

**Labels:** `track-longpole`, `lead-senior`, `parity-blocked-by:views-engine`. **Size: XL.**

---

### 9.3 — Owned `TextLayout`-based layout/box model

**Summary:** Build the FieldWorks-owned layout/box model over Avalonia `TextLayout` (HarfBuzz/Skia)
replacing the `VwParagraphBox`/`VwStringBox`/`VwGroupBox`/`VwTableBox`/`VwLazyBox` hierarchy — only the
editable/selection/box layer, **not** re-implementing shaping.

**Type:** Sub-epic. **Build-vs-extend: BUILD.** `TextLayout` supplies shaping + line-breaking + hit-testing
as a managed primitive; FieldWorks owns the box/line model and editable layer above it. The native `VwBox`
hierarchy is the **reference spec**.

**Description.** Implement an owned box/line model (paragraph/string/group/table/lazy/picture analogues)
over `TextLayout` for document surfaces where stock `TextBox`/`TextLayout` is insufficient — justified/complex
multi-line StText, RTL paragraphs, drop-caps, tables, overlays. `VwLazyBox` flags that **lazy/virtualized box
realization** is a document-engine concern: **coordinate with Stage 3** so the engine shares the
realization-window approach rather than reinventing virtualization (large StTexts/interlinear regress without it).

**Acceptance criteria.**
- Document layout parity (justification, complex line-breaking, RTL paragraphs, drop-caps) on target
  surfaces; visual lane permits the intentional Fluent restyle but asserts functional/density fidelity.
- Lazy/virtualized box realization for large documents; coordinated with the Stage 3 virtualization primitive
  (no duplicate realization engine). Performance ≤ legacy × 1.2 or accepted delta recorded.
- Path-3 bundle at 100% + 150% DPI. Forbidden-symbol audit green.

**Dependencies.** Gated on **9.0**; consumes **9.2**. **Coordinate with Stage 3** (`VwLazyBox` ↔
virtualization). Consumed by **Stage 7A** (aligned interlinear grid).

**Build-vs-extend callout:** **BUILD** an owned `TextLayout`-based box/layout layer.

**Labels:** `track-longpole`, `lead-senior`, `parity-blocked-by:views-engine`, `parity-blocked-by:stage-3-virtualization`. **Size: XL.**

---

### 9.4 — Embedded objects / tables / footnotes / overlays

**Summary:** Editable embedded objects/ORCs, footnotes, pictures, tables, and overlays — the
`VwTableBox`/`VwPictureBox`/ORC-anchor/`IVwOverlay` long tail. The difference between "lexicon notes work"
and "Scripture/structured documents work."

**Type:** Sub-epic (can trail). **Build-vs-extend: BUILD** owned managed equivalents.

**Description.** Editable embedded content: ORC anchors and editing, footnotes, pictures, tables, and
overlays. Owned managed equivalents — the Avalonia editor draws its own squiggles and queries `ISpellEngine`
directly; it must **not** call `SetSpellingRepository`/`IGetSpellChecker` (those exist only for `VwRootBox`).
Footnotes/overlays/ORC editing get owned managed equivalents, not native bridges. Easy to under-scope —
budget the long tail explicitly.

**Acceptance criteria.**
- Editable ORC/footnote/picture/table/overlay parity on target surfaces; spell-check via `ISpellEngine`
  with owned squiggle rendering (no `VwRootBox` spelling APIs).
- Path-3 bundle at 100% + 150% DPI. Forbidden-symbol audit green; edits through `IEditSession` + global undo.

**Dependencies.** Gated on **9.0**; consumes **9.2** and **9.3**. May trail 9.1–9.3.

**Build-vs-extend callout:** **BUILD** owned managed equivalents for embedded/overlay content.

**Labels:** `track-longpole`, `lead-senior`, `parity-blocked-by:views-engine`. **Size: L–XL.**

---

## Notes / open questions

- **Graphite / Awami Nastaliq doc-and-notify obligation (resolved §11.1).** Graphite is removed entirely —
  no escape-hatch, no gating on a Nastaliq solution. HarfBuzz covers the large majority of formerly-Graphite
  scripts (SIL dropped Graphite from Charis/Doulos v7 in 2025), **but Awami Nastaliq (Urdu/Arabic Nastaliq)
  is Graphite-ONLY by design** — OpenType lacks the collision avoidance Nastaliq needs and SIL has no
  OpenType replacement. These are exactly FieldWorks' minority-language users. The **9.0 G0–G3 scan is not a
  go/no-go gate** but is **required** to enumerate the exact dropped-script list + affected projects, so the
  program can **document the loss and notify affected users with migration guidance** before removal ships
  (a **Stage 10B / Stage 13** deliverable). Native `GraphiteEngineClass` deletion stays in **Stage 13**
  (legacy surfaces use it during coexistence). HarfBuzz does **not** implement Graphite — `hb-graphite2`
  only delegates to external `libgraphite2` and is off by default — so "managed only" genuinely means *no
  Graphite shaping at all*. **G3 has no in-app fidelity path after removal** (open product-comms risk, but
  the engineering decision is settled).

- **The explicit Stage 9 ↔ 7A engine seam.** Stage 9 owns the **engine seams** — selection, layout, box
  model, editable structured content, the in-memory presentation cache/`CachePair`/`VwCacheDa` replacement,
  hit-test combo editing, and aligned interlinear grid layout primitives. **Stage 7A owns the
  interlinear-specific composition** (`InterlinDocRootSiteBase`, `InterlinDocForAnalysis`, `SandboxBase`,
  `ConstChartBody`, `InterlinRibbon`) over those seams. State this split before freezing the 9.2/9.3 API so
  Stage 7A does not inherit unscoped engine work and Stage 9 does not absorb interlinear UI. **Action:**
  9.0's spike *must* include the Sandbox/interlinear scenario or Stage 7A inherits an unvalidated dependency
  and stalls late.

- **Open question — is the owned selection model (9.2) buildable to parity?** `VwSelection` is ~14.4K LOC of
  cross-level/cross-object logic; this is the single largest correctness surface and the deepest unknown.
  The 9.0 spike must exercise selection across structured content with real fixtures before committing the
  build. (High risk.)

- **Open question — does stock `TextLayout` give enough for FieldWorks document layout?** (justification,
  complex line-breaking, drop-caps, inverted/RTL paragraphs, overlays, tables). If not, 9.3 grows toward
  re-implementing more of the box engine than budgeted. (Med-High.)

- **Coordinate 9.3 with Stage 3.** `VwLazyBox` lazy realization is load-bearing for large StTexts and
  interlinear; the owned layout must virtualize box realization or large documents regress — share the
  realization-window approach with Stage 3 rather than reinventing it. (Med.)

- **IME at document scale** across structured boundaries (composition spanning paragraph/object edges)
  needs realized-window evidence — environment-sensitive, not headless-provable. (Med.)
