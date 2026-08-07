# Stage 9 Review — Managed Document/Text Rendering & Editing Engine (Views Replacement)

Reviewer: Claude (Opus 4.8, 1M). Date: 2026-06-15.
Scope under review: master plan §4 (stage row 9), §6 Stage 9, §11 open question (build-vs-extend),
risk register rows on rich-text/bidi/IME and HarfBuzz coverage. Grounded in `Src/views/`,
`Src/Common/SimpleRootSite/`, `Src/Common/FwAvalonia/Region/FwFieldControls.cs`
(`FwMultiWsTextField`), `openspec/changes/avalonia-multi-writing-system-text-foundation/`, and
`lexical-edit-avalonia-migration/native-views-audit.md`. This is the program's **long pole**; the
review treats it as such.

---

## 1. Scope assessment

**Verdict: NOT realistic as one stage. Decompose into a spike gate + four sequenced sub-epics.**
The native surface this stage replaces is far larger than a single epic can carry, and the stage
text silently bundles three different problems — *text layout/shaping*, *selection/caret model*,
and *structured document model* — each of which is itself a multi-quarter senior effort.

Repo-measured size of what is being replaced (Explore audit, this worktree):

- **Native C++ Views engine `Src/views/`: ~134K LOC, 136 files** (50 `.cpp` / 86 `.h`).
  `VwSelection.cpp` alone is **~14.4K LOC**; `VwTextBoxes.cpp` ~10.1K; `VwRootBox.cpp` ~5.3K.
- **Managed host `Src/Common/SimpleRootSite/`: ~25K LOC, 31 files.** `SimpleRootSite.cs` ~7.1K,
  `EditingHelper.cs` ~3.9K, `SelectionHelper.cs` ~2.1K.
- **~32 public COM interfaces** form the contract (`IVwRootBox`, `IVwSelection`, `IVwEnv`,
  `IVwGraphics`, `IVwRootSite`, `IRenderEngine`, `IVwViewConstructor`, `IVwStylesheet`,
  `IVwOverlay`, `IVwPrintContext`, `IVwSynchronizer`, `ILgSegment`, …).
- **~153 files reference `IVwRootBox`; ~80 reference `SimpleRootSite`/RootSite** — i.e. the
  consumer fan-out is wide, but per `native-views-audit.md` §8.6 only ~6–10 *surfaces* are
  strongly coupled (Interlinear, Discourse, XMLViews browse, xWorks doc views, DetailControls
  StText, parser sandbox).

**What `FwMultiWsTextField` actually is (the DELTA framing matters):** it is **not** a managed text
engine. It is a thin wrapper around stock Avalonia `TextBox` (one per WS row) with three bolt-ons:
(a) bidi-aware caret arrow navigation delegated to `RegionBidirectionalTextNavigation`, (b)
grapheme-cluster selection normalization, and (c) `ITsString` run write-back staging via
`RegionRichTextEditAlgorithms.ApplyPlainTextEdit` (`FwFieldControls.cs:144-291`). Shaping, layout,
line-breaking, hit-testing, and IME are **stock Avalonia `TextBox`/`TextLayout`** — FieldWorks owns
none of it. The multi-WS foundation (`avalonia-multi-writing-system-text-foundation`, done
2026-06-15) explicitly closed **only** the single-paragraph string-slice blockers
(`MultiStringSlice`/`StringSlice`/`GhostStringSlice`) and **explicitly deferred `StText`
multi-paragraph editing and embedded-object/ORC editing** (`native-views-audit.md` §8.3 "Deferred,"
design.md decision 4).

So the true Stage-9 DELTA over Stage 0 is everything the field foundation isn't:

| Already done (Stage 0 / foundation) | Stage 9 DELTA (not started) |
|---|---|
| Single-para, single-/multi-WS `ITsString` field editing over stock `TextBox` | **Multi-paragraph `StText`** editing (paragraph model, splitting/merging, para-level props) |
| Per-WS font/RTL/keyboard projection; bidi caret nav within a field | **Selection across structured content** (nested objects, levels — what `IVwSelection`/`SelLevInfo` model in 2.1K + 14.4K LOC) |
| Grapheme-cluster-safe edit/caret; IME *composition-state* modeling | **Owned text layout/shaping/line-break** (stop depending on stock `TextBox` once content exceeds what `TextBox` renders correctly) |
| Read-only ORC/embedded-object rendering | **Editable embedded objects/ORCs, footnotes, pictures, tables** (`VwTableBox`, `VwPictureBox`, ORC anchors) |
| Headless + typing-latency evidence for fields | **Interlinear/sandbox** structured layout (Stage 7's dependency) |

**Recommended decomposition (one parent epic, gated children):**

- **9.0 Spike (gate, blocking).** The five named spike scenarios + the HarfBuzz-coverage proof.
  Output decides build-vs-extend AND feeds the Stage-10B removal gate. Do not open 9.1+ until 9.0
  exits.
- **9.1 Multi-paragraph `StText` editing** (closes the one deferred string blocker; smallest
  document-model step; unblocks lexicon comment/note fields and Notebook detail).
- **9.2 Managed structured selection/caret model** replacing `IVwSelection`/`SelectionHelper`
  (the 14.4K-LOC core; selection across levels/nested objects; the hardest correctness surface).
- **9.3 Owned text layout/shaping surface** — only *if* the spike shows stock `TextLayout`/`TextBox`
  is insufficient for document surfaces (it almost certainly is for justified/complex multi-line
  StText, drop-caps, tables, overlays). HarfBuzz/Skia via Avalonia `TextLayout`, FieldWorks-owned
  box/line model replacing the `VwParagraphBox`/`VwStringBox`/`VwLazyBox` hierarchy.
- **9.4 Embedded objects / footnotes / pictures / tables / overlays** (the `VwTableBox` /
  `VwPictureBox` / ORC / `IVwOverlay` long tail; can trail).

This mirrors the Stage-10 reviewer's "split the bundled stage" pattern and matches the audit's own
deferral structure. **Interlinear-specific** layout (Stage 7) should stay in Stage 7 consuming 9.2/9.3
seams, not be pulled into 9 — but 9.0's spike *must* include an interlinear/sandbox scenario or
Stage 7 inherits an unvalidated dependency.

---

## 2. Feasibility (repo-grounded + web)

### 2a. The framework gap is real and is the program's #1 risk

The master plan's risk register already ranks "rich-text/bidi/IME gaps in Avalonia" High/High; the
web evidence confirms it is not theoretical. Avalonia 11 added IME and rich-text *inlines*, but the
**editable** complex-text surface still carries open caret/selection/RTL defects:

- RTL/Unicode `TextBox` rendering issues:
  [#5794](https://github.com/AvaloniaUI/Avalonia/issues/5794).
- Reversed-selection caret placement: [#12055](https://github.com/AvaloniaUI/Avalonia/issues/12055);
  Shift+Arrow caret: [#13648](https://github.com/AvaloniaUI/Avalonia/issues/13648);
  Home/End caret: [#12063](https://github.com/AvaloniaUI/Avalonia/issues/12063);
  word-jump caret: [#14726](https://github.com/AvaloniaUI/Avalonia/issues/14726);
  caret hidden under non-empty selection: [#16388](https://github.com/AvaloniaUI/Avalonia/issues/16388).
- The vendor rich-text editor is **Pro-tier** with RTL unverified
  ([avaloniaui.net/blog/rich-text-editor](https://avaloniaui.net/blog/rich-text-editor));
  AvaloniaEdit is a **code editor**, not rich text
  ([AvaloniaEdit](https://github.com/AvaloniaUI/AvaloniaEdit)).

**Implication:** the `FwMultiWsTextField`-over-stock-`TextBox` strategy that worked for *fields*
does **not** extend to document surfaces. Stock `TextBox` carries exactly the caret/selection bugs
above, and FieldWorks has been patching around them per-field (`RegionBidirectionalTextNavigation`,
hit-test normalization in `FwFieldControls.cs:203-222`). At document scale (multi-paragraph,
mixed-direction, structured selection across objects) that patch-the-stock-control approach does not
scale — Stage 9 will need a **FieldWorks-owned selection/layout model above the rendering layer**,
exactly as `avalonia-multi-writing-system-text-foundation/design.md` risk note #1 already warned for
fields. This is the strongest argument that 9.2 (owned selection) and 9.3 (owned layout) are
unavoidable, not optional.

### 2b. Build-vs-extend (the §11 open question)

Both paths are fully managed. Repo-grounded reading:

- **Extend the existing foundation** (`FwMultiWsTextField` + `RegionRichTextEditAlgorithms` +
  `RegionImeCompositionState`): viable for **9.1 StText** if a paragraph is modeled as a stack of
  the existing per-WS editors. Breaks down at 9.2/9.3 because stock `TextBox` owns selection/layout
  and cannot express cross-object selection or FieldWorks box semantics.
- **Build an owned document control** over Avalonia `TextLayout` (the read-only shaping/measure
  primitive) + an owned caret/selection/box model: necessary for 9.2–9.4. `TextLayout` is HarfBuzz/
  Skia-backed and gives shaping + line-breaking + hit-testing as a primitive, so "build" here is
  *owning the editable/selection/box layer over `TextLayout`*, **not** re-implementing shaping.

**Recommendation:** record the likely outcome now so the spike validates rather than discovers it —
**extend for 9.1, build an owned `TextLayout`-based control for 9.2+**. The native `VwBox`
hierarchy (`VwParagraphBox`/`VwStringBox`/`VwGroupBox`/`VwTableBox`/`VwLazyBox`) is the reference
spec for the owned box model; `VwLazyBox` in particular flags that **lazy/virtualized box
realization** (Stage 3's virtualization gap) is also a document-engine concern — coordinate 9.3 with
Stage 3.

### 2c. Graphite coverage — the verdict (this is Stage 9's gating deliverable for Stage 10B)

**Verdict: HarfBuzz/managed shaping covers the vast majority of formerly-Graphite scripts, but there
is a confirmed, irreducible gap — Awami Nastaliq (Urdu Nastaliq) is Graphite-ONLY by design and has
no OpenType equivalent. "HarfBuzz only" must be qualified, not asserted unconditionally.**

Evidence:

1. **HarfBuzz does not implement Graphite itself.** `hb-graphite2` *delegates* to the external
   `libgraphite2` (`gr_face`) and is **disabled by default** in HarfBuzz builds
   ([hb-graphite2](https://harfbuzz.github.io/harfbuzz-hb-graphite2.html),
   [graphite-shaping](https://harfbuzz.github.io/graphite-shaping.html)). So "HarfBuzz/managed only,
   Graphite removed" genuinely means *no Graphite shaping at all* — there is no hidden HarfBuzz
   Graphite fallback. The repo's own `graphite-decommissioning.md` §2 already recorded this
   correctly.
2. **SIL has itself moved its flagship fonts to OpenType-only.** Charis SIL v7 (2 Jun 2025) and
   Doulos SIL **removed Graphite** ("application/OS OpenType support has greatly improved")
   ([Charis news](https://software.sil.org/charis/news/),
   [Doulos news](https://software.sil.org/doulos/news/)). Most Latin/Cyrillic/IPA/diacritic-heavy
   scripts are therefore covered by OpenType today.
3. **The hard exception is real and named.** Awami Nastaliq **does not support OpenType and will not
   work in OpenType apps** — because Nastaliq requires Graphite **collision avoidance** that
   "OpenType engines" lack; SIL states they would add OpenType "should OpenType engines gain the
   necessary collision avoidance support" ([Awami FAQ](https://software.sil.org/awami/faq/)). Annapurna
   SIL (Devanagari) still ships both engines. This is exactly the population the
   `graphite-transition-support` change flagged: "Graphite-rendered writing systems are concentrated
   in exactly the minority-language projects FieldWorks exists to serve" (proposal.md, "Why").

**Consequence for the spike:** 9.0's HarfBuzz-coverage proof must be **data-driven, not a font-by-
font opinion** — scan project LDML for `IsGraphiteEnabled` + Graphite-only feature strings (the
repo already has the `km.ldml` Khmer fixture and the G0–G3 classifier salvageable from
`graphite-transition-support` tasks 1.3/1.4/3.2), and classify each as G0 (dual-engine, OT-safe),
G2 (dual-engine but renders *differently* under OT feature strings), or **G3 (Graphite-only, no OT
path — Awami Nastaliq)**. Stage 10B removal is safe only for G0/G2-with-accepted-delta; G3 has **no
in-app fidelity path after removal** and needs an explicit product decision (freeze those projects
on legacy, or accept degraded rendering). The Stage-10 review reached the same conclusion from the
removal side; this review supplies the coverage proof it depends on.

---

## 3. Best practices

- **Spike-first is correct and already in the plan — make its exits measurable.** Each of the five
  spike scenarios (mixed bidi, CJK IME, custom WS, multi-para StText, cross-structure selection) and
  the HarfBuzz-coverage scan must produce a Path-3 bundle or a written go/no-go, not a "looks fine."
  Add an **interlinear/sandbox** scenario so Stage 7's dependency is validated in the spike.
- **Own the selection/layout model above the rendering primitive** (foundation design risk #1).
  Use `TextLayout` for shaping/measure/hit-test; do **not** rely on stock `TextBox` selection/caret
  for document surfaces given the open bidi/caret defects (§2a).
- **Reuse the native `VwBox` hierarchy as the spec, not the implementation.** It is the proven box
  model (paragraph/string/group/table/lazy/picture); the managed model should map to it for parity
  fixtures while being a clean C# rewrite.
- **One global undo stack** (master principle 9 / `architecture-patterns.md` §8): route document
  edits through `IUndoRedoCoordinator` → LCModel action handler; never a parallel Avalonia history.
  StText edits are multi-paragraph LCModel mutations — keep them inside the fenced `IEditSession`.
- **Spell-check is a service, not a render concern.** Per `native-views-audit.md` §8.7/8.8, the
  Avalonia editor queries `ISpellEngine` directly and **draws its own squiggles** — it must NOT call
  `SetSpellingRepository`/`IGetSpellChecker` (those exist only for `VwRootBox`). Footnotes, overlays,
  and ORC editing similarly need owned managed equivalents.
- **IME and RTL need realized-window evidence, not headless** (foundation design decision 6; parity-
  evidence §2). Headless proves run fidelity/cluster/selection logic; IME composition and complex-
  script editing need UIA2/realized-window lanes.
- **Keep the forbidden-symbol audit green every PR** (`EngineIsolationAuditTests`) — no
  `IVwRootBox`/`IVwEnv`/`IVwGraphics`/`IRenderEngine`/`GraphiteEngineClass` in the Avalonia path.
- **Measure typing latency at 100% and 150% DPI** and commit budgets before any parity claim
  (extends the foundation's `typing-latency-evidence.md` to multi-paragraph).

---

## 4. Interactions & dependencies

- **Stage 7 (Interlinear/Discourse) hard-depends on Stage 9 — and the plan's §5 graph already wires
  `S9 → S7`.** But Stage 9's *current scope text* does not name interlinear layout, sandbox, or
  constituent-chart needs. `native-views-audit.md` §8.6 lists the Stage-7 consumers as
  `InterlinDocRootSiteBase`, `InterlinDocForAnalysis`, `SandboxBase`, `ConstChartBody`,
  `InterlinRibbon` — all `RootSite`-derived. **Action:** Stage 9 owns the *engine seams* (selection,
  layout, box model, editable structured content) and 9.0's spike must include an interlinear
  scenario; Stage 7 owns the interlinear-specific *composition* over those seams. State this split
  so Stage 7 does not inherit unscoped engine work and Stage 9 does not absorb interlinear UI.
- **Stage 10B (Graphite removal) is gated on Stage 9's coverage proof — correct, and the Stage-10
  reviewer already relies on it.** Tighten the gate: Stage 9's exit must include the **LDML/G0–G3
  fixture scan** identifying G3 (Graphite-only) projects, not just "spike says HarfBuzz is fine."
  The G3/Awami-Nastaliq finding (§2c) is a *blocking input* to whether 10B can remove Graphite at
  all vs. defer the native-engine deletion to Stage 13.
- **Stage 3 (virtualized grid/tree).** `VwLazyBox` shows document layout itself needs lazy/
  virtualized box realization. 9.3's owned layout and Stage 3's virtualization primitive should
  share the realization-window approach; coordinate so the document engine doesn't reinvent
  virtualization.
- **Stage 4 (finish Lexical/Advanced Entry).** Stage 4 closes the *field* surface that the
  foundation already powers; it does **not** depend on Stage 9 except for the one deferred `StText`
  field (lexicon comments/notes) — which 9.1 unblocks. Note this so Stage 4 can ship with `sttext`
  fields read-only (already the case per §8.3) and upgrade when 9.1 lands.
- **Stage 13 (native decommission).** The native `Src/views` deletion + RegFree/build cleanup lands
  here, not in Stage 9. Stage 9 only stops the *Avalonia path* from using native Views; `Src/views`
  stays alive for legacy-mode/parity baselines and all the §8.6 unplanned-area consumers until
  Stage 13.
- **Spell/ICU/parser services** stay behind seams (`ISpellEngine`, `Icu`/`CustomIcu`,
  `ParserConnection`) — unchanged by Stage 9 (§8.8).

---

## 5. Recommended plan changes

1. **Decompose Stage 9 into one parent epic + gated children 9.0 (spike), 9.1 (StText), 9.2
   (selection/caret model), 9.3 (owned layout/box model), 9.4 (embedded objects/tables/overlays).**
   Block 9.1+ on 9.0 exit. (§1.)
2. **Reframe the stage scope as the DELTA over the landed foundation** — explicitly state that
   single-paragraph field editing is *done* (`FwMultiWsTextField`) and Stage 9 is multi-paragraph +
   structured selection + owned layout + editable embedded content. Avoids re-litigating field work.
3. **Add a HarfBuzz-coverage / G0–G3 fixture-scan deliverable to 9.0's exit gate**, salvaging the
   classifier from `graphite-transition-support` (tasks 1.3/1.4/3.2). Make the **G3 (Graphite-only,
   e.g. Awami Nastaliq) finding an explicit blocking input to Stage 10B.** (§2c.)
4. **Record the build-vs-extend recommendation** (extend for 9.1; build owned `TextLayout`-based
   selection/layout for 9.2+) so the spike validates rather than re-opens it. (§2b.)
5. **Add an interlinear/sandbox scenario to the 9.0 spike** and state the Stage-9/Stage-7 seam split
   so Stage 7's dependency is de-risked. (§4.)
6. **Coordinate 9.3 owned layout with Stage 3 virtualization** (the `VwLazyBox` lazy-realization
   overlap). (§4.)
7. **Note that native `Src/views` deletion is Stage 13, not Stage 9** — Stage 9 only severs the
   Avalonia path. (§4.)

---

## 6. Open questions / risks

1. **G3 Graphite-only projects (Awami Nastaliq / Urdu Nastaliq) have no managed rendering path after
   removal.** OpenType cannot do Nastaliq collision avoidance ([Awami FAQ](https://software.sil.org/awami/faq/)).
   Needs a product-owner decision: freeze those projects on legacy, accept degraded OT rendering, or
   hold native Graphite until those users migrate. **This is a real, named, unresolved risk the
   roadmap's "Graphite fully removed" decision has not reconciled with FieldWorks' minority-language
   mission** — the `graphite-transition-support` change exists precisely because of it. (High.)
2. **Is the owned selection model (9.2) buildable to parity?** `VwSelection` is ~14.4K LOC of
   cross-level, cross-object selection logic; `SelectionHelper`/`TextSelInfo` add ~2K+. This is the
   single largest correctness surface and the deepest unknown. The spike must exercise selection
   across structured content with real fixtures before committing the build. (High.)
3. **Does stock Avalonia `TextLayout` give enough for FieldWorks document layout** (justification,
   complex line-breaking, drop-caps, inverted/RTL paragraphs, overlays, tables)? If not, 9.3 grows
   toward re-implementing more of the box engine than budgeted. (Med-High.)
4. **IME at document scale across structured boundaries** — composition spanning paragraph/object
   edges is the kind of state the foundation modeled only for single fields. Realized-window
   evidence required; environment-sensitive. (Med.)
5. **Performance/virtualization** — `VwLazyBox` laziness is load-bearing for large StTexts and
   interlinear; the owned layout must virtualize box realization or large documents regress.
   Coordinate with Stage 3. (Med.)
6. **Footnotes / ORC / embedded windows (`IVwEmbeddedWindow`) editing** — the long tail (9.4) is
   easy to under-scope; it is the difference between "lexicon notes work" and "Scripture/structured
   documents work." (Med.)

---

## 7. Confidence

**High** on the repo-grounded size and DELTA framing (native LOC/interface counts from the
`Src/views` + `SimpleRootSite` audit; `FwMultiWsTextField` is verifiably stock-`TextBox`-based;
`StText`/ORC explicitly deferred per `native-views-audit.md` §8.3 and foundation design.md).
**High** on the Graphite-coverage verdict (HarfBuzz delegates to external libgraphite2 and is off by
default; SIL dropped Graphite from Charis/Doulos; Awami Nastaliq is Graphite-only by design — all
web-confirmed and consistent with the repo's own `graphite-decommissioning.md`/`graphite-transition-
support`).
**Medium-high** on the decomposition shape (clear from the audit's deferral structure and the native
hierarchy, but exact epic boundaries are the owner's call).
**Medium** on build-vs-extend specifics and on whether `TextLayout` suffices for document layout —
these are genuinely what the 9.0 spike must decide; this review narrows the question and predicts the
likely answer but does not pre-empt the spike.
