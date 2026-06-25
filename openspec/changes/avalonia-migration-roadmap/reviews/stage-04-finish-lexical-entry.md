# Stage 4 Review — Finish the Lexical / Advanced Entry Surface (Exemplar)

> Reviewer pass against the live branch `010-advanced-entry-view-phase-1-2`.
> Master plan: `openspec/changes/avalonia-migration-roadmap/complete-migration-program.md` §6 Stage 4
> (lines 252–258). Repo state read from `openspec/changes/lexical-edit-avalonia-migration/tasks.md`,
> `region-manifest.md`, `Src/xWorks/FullEntryRegionComposer.cs`, `Src/Common/FwAvalonia/`.

## 1. Scope assessment

Stage 4 (plan §6, lines 252–258) names five close-out work items:
1. Tables/browse on the Stage-3 control (lexical-edit 7.x).
2. Full P0/P1 field parity beyond the first slice (custom fields, rich references, media/pronunciation).
3. 150% DPI + scroll/expand/typing-latency budgets (tasks 2.13, 7.7).
4. JSON view-definition serialization + override migrator + runtime-XML-disable (9.x).
5. Path-3 bundle completeness; region manifest fully green.

Against the actual open checkboxes in `tasks.md`, this scope is **mostly accurate but partly stale
and partly over/under-stated.** The genuinely-open work, quoted from the file:

- **7.7** `- [ ] 7.7 Add large-fixture performance budgets for open time, scroll/expand latency, typing
  latency, realized control count, memory, and cache invalidation. (In progress … **remaining:**
  scroll/expand, typing latency, realized-count, memory, and cache-invalidation lanes — region-manifest §5.4.)`
- **2.13** is still `- [ ]` for the same reason: `**Still open:** scroll/expand and typing-latency
  scenarios, and 150% DPI numbers (need a non-headless scaled-display run)`.
- **9.2** `- [ ] 9.2 … **remaining:** the JSON serializer + override migrator from
  canonical-view-definition-design.md steps 1–3.`
- **9.3** `- [ ] 9.3 … **remaining:** user override fixtures per override-fixtures.md.`
- **9.4** `- [ ] 9.4 Disable runtime XML for a gated migrated surface while retaining import/audit fallback.`
- **10.4 / 10.5** Graphite/native-rendering and browser/PDF default-path validation — both `- [ ]`.
- **10.8 / 10.10 / 10.12** per-manifest evidence, Path-3 lane completeness, product wiring review — all `- [ ]`.
- **6.13** GATE (multi-WS text foundation) is `- [ ]` but the note says the code+automated lanes have
  LANDED; the only residual is the **foundation change's** 8.2 (realized-window manual RTL + Khmer) and
  10.3 (full CI). Those belong to `avalonia-multi-writing-system-text-foundation`, not this change.
- **18.10–18.13** PR-readiness / foundational follow-ups (IME end-to-end wiring, dual-projection
  unification, full build/test run, wiring checklist).

**Stale claim in Stage 4 scope — item 4 (JSON serializer).** The plan implies 9.x is unstarted. It is
not: `Src/Common/FwAvalonia/ViewDefinition/ViewDefinitionJsonSerializer.cs` already exists and is real
(canonical property order, `FormatVersion = 1`, `Serialize`/`Deserialize`), with
`BrowseAndCanonicalJsonTests.cs` exercising it. `architecture-patterns.md` §1 even cites it as canonical
code. So 9.2's *serializer core exists*; what is genuinely open is the **override migrator** (sparse
JSON patches keyed by `StableId`) and the **runtime-XML-disable switch** (9.4). Stage 4 should be
re-scoped to "finish 9.x" not "build 9.x".

**Overstated — item 1 (tables/browse).** `- [x] 7.1` is checked: `LexicalBrowseView.cs` exists and
`LexicalBrowseViewTests` proves a 10k-row source realizes <100 rows. The *detail* surface (the entry
view) does not actually contain a browse table in normal LexEntry layouts — browse/XMLViews is the
Stage-3/Stage-7 surface. Item 1 as written ("tables/browse in the entry view") risks importing
Stage-3 scope. What is truly left here is wiring the **already-built** browse control onto the shared
Stage-3 control once Stage 3 lands, and proving it at scale — a thin integration, not a build.

**Understated — item 2 (P0/P1 field parity).** This is *further along* than "beyond the first slice"
implies. `FullEntryRegionComposer.cs` (2524 lines) already walks the complete `LexEntry/Normal` layout:
custom fields render via plugin factory with explicit degradation (`RegionCustomFieldRenderingTests`),
reference vectors/atomic refs render (`EntryReferenceVectorTests`, `LexReferenceMultiSliceTests`,
`FullEntryRegionReferenceChooserTests`), ghost references (`GhostLexRefSliceTests`), pictures decode and
render (11.6), pronunciation writing systems are handled (`FullEntryRegionComposer.cs:2168`,
`:2336–2340`). Tasks 7.4 and 11.1–11.17 are all `- [x]`. The honest residual P0/P1 gaps are narrow and
already named in the manifest §6: **custom fields ✘ (9.x B1)**, **rich-TsString runs** (ride the landed
foundation), **StText multi-paragraph + ORC editing** (deferred to Stage 9 / task 8.11), and
**chooser write-back beyond morph type** (6.3 follow-on / 11.2). Stage 4 should cite the manifest §6
parity table as its acceptance scope rather than the vague "custom fields, rich references,
media/pronunciation," most of which are already green.

**Correctly scoped — items 3 & 5.** Perf budgets (scroll/expand/typing/150% DPI) and Path-3/manifest
greening are the real, accurately-described heart of Stage 4.

## 2. Feasibility (repo-grounded)

- **Perf budgets (7.7/2.13) — feasible, needs hardware.** The harness, baselines, and enforcement
  mechanism already exist: open-time + refresh-after-edit are measured and committed
  (`region-manifest.md` §5.1–5.3, `DataTreeTimingBaselines.json`, `DataTreeReshowTimingTests`). A
  `TypingLatencyHarnessTests.cs` already exists in `FwAvaloniaTests`. The blocker is purely operational:
  `region-manifest.md` §5.4 — *"150% DPI numbers need a non-headless run on a scaled display."* Headless
  Skia cannot produce these; this needs a real scaled-display machine in the loop. **Risk:** low
  technical, medium logistical (CI cannot self-serve 150% DPI).
- **JSON override migrator + runtime-XML-disable (9.2/9.4) — feasible, design exists.** The serializer
  is built; `canonical-view-definition-design.md` defines the 5-step sequence and the sparse-patch
  override model keyed by `ViewNode.StableId`. 9.5 (`- [x]`) already enumerated the 12-family XML blocker
  register with measured prevalence (161 `<generate>`, 48 ghost attrs, 230 `if`, 473 menu/hotlink). The
  open risk the blockers doc itself flags: *ghost/conditional/chooser schema must be reserved before the
  canonical Layer-1 JSON freezes* — i.e. freezing the format prematurely is the trap.
- **Manifest greening (item 5) — the hard gate.** `region-manifest.md` §6 verdict is **"Default stays
  Legacy"** with Layout/Validation/Accessibility/Performance rows **Partial**. Each Partial row has a
  named owner: layout = full-tree semantic comparison vs legacy DataTree not yet run; validation =
  severity/async lanes; accessibility = keyboard-traversal assistive smoke pends chooser-dialog work
  (3.16/6.3); performance = §5.4. These are all in-reach but are the long tail.
- **6.13 dependency is effectively satisfied** for editing behavior (the note documents the foundation's
  code landed); Stage 4 should not block on it except for the two foundation-owned residuals.

## 3. Best practices for an exemplar others copy

Stage 4's deliverable is not just "green" — it is the **template Stages 6/8 (mid-level) clone**. The
exemplar must therefore make the *reusable pattern* visible, not just the lexical-edit instance:

1. **Resolve the dual-projection split (18.11) before declaring exemplar-complete.** Today there are two
   projectors: the thin `LexicalEditRegionMapper` (FwAvalonia) and the full `FullEntryRegionComposer`
   (xWorks, 2524 lines). `- [ ] 18.11` says unify them *"before a 2nd region reuses it, so header/indent/
   value-binding logic is not re-derived."* If Stage 6 copies a 2524-line composer, the exemplar has
   taught duplication. This is the single most important exemplar-quality item and it is currently a
   tracked-but-open follow-up. **Recommend pulling 18.11 into Stage 4's exit gate**, or explicitly into
   Stage 1 (platform kit) where the scaffolding template lives.
2. **Promote `RegionViewingServices` to the documented copy-me contract.** Task 8.3 built it as an
   as-built capability→owner map with a positive-assertion test (`RegionViewingServiceReplacementTests`).
   8.9 (`- [ ]`) defers turning it into an injected per-capability seam *"when a second region adopts it."*
   The exemplar should at minimum document the map as the thing Stage 6 reads first.
3. **Custom-slice plugin burn-down must be demonstrated, not just possible.** `RegionEditorPlugins.cs` +
   `LexemeEditorBurnDownTests.cs` exist. The exemplar should ship a worked "here is how you census a
   surface's custom slices and add a plugin" path, since that is exactly what Stages 6/8 face.
4. **Keep the evidence-language discipline.** Per `parity-evidence.md` §3, the manifest §6 honestly says
   "Partial" and lists which axes are unproven. The exemplar's value is partly that it *models honest
   gating*. Do not flip rows to Pass to "finish the stage"; close the underlying lane.

## 4. Interactions & dependencies

- **Hard dependency on Stage 3 (tables) — real but narrow.** Plan §4 lists Stage 4 `Depends on 2,3`.
  The browse control (`LexicalBrowseView`) is *already built standalone* on stock
  `VirtualizingStackPanel`. Stage 3 builds the *shared, reusable* owned grid/tree. The dependency is:
  Stage 4 must re-home its browse view onto the Stage-3 control and prove 10k-row/253-slice at 150% DPI
  on it. If Stage 4 ships its own browse view as "done," it pre-empts Stage 3's single-owner mandate.
  **Sequencing note:** the detail entry view itself does **not** need a browse table, so most of Stage 4
  (P0/P1 fields, perf, JSON, manifest) can proceed *in parallel with* Stage 3; only the
  browse-on-shared-control integration truly blocks on Stage 3.
- **Template for Stages 6 & 8.** Plan §5 graph: `S4 --> S6`, `S4 --> S8`; §8 staffing makes Stage 4 the
  "scale-up milestone" before Track-II head-count grows. What Stage 4 *must demonstrate* for them:
  (a) compile-from-live-inventory (done, 4.10), (b) composer walks IR → region model → fenced edit
  session (done), (c) plugin registry for custom slices (done, needs worked example), (d) Path-3 bundle
  per scenario (7.8 done for first slice; 10.10 open for "every scenario"), (e) the manifest as
  definition-of-done. The **unified projector (18.11)** is the missing piece that makes (a)+(b) copyable.
- **Stage 9 boundary — correct as drawn, with one caveat.** Rich-text-in-fields is correctly *not* in
  Stage 4: multi-WS `ITsString` field editing is the landed foundation (6.13), while **StText
  multi-paragraph and ORC rich-run editing are explicitly deferred to Stage 9 / task 8.11** (`- [ ]`).
  This boundary is clean. Caveat: Stage 4 item 2 says "rich references" — ensure that means reference
  *vectors/atomic* (done) and not rich-text, to avoid dragging Stage 9 scope in.
- **Stage 10 boundary (Graphite/PDF).** 10.4/10.5 (`- [ ]`) are default-path Graphite + browser/PDF
  validation. The entry surface is classified *outside* the Gecko/PDF boundary (5.7, `gecko-pdf-audit.md`)
  and engine isolation is enforced (`EngineIsolationAuditTests`). So 10.4/10.5 are **not** Stage 4
  blockers for the entry region per se — they block the *global default switch*, which is a
  cross-stage/cutover concern, not "finish the entry surface." **This is a scope-attribution risk:** if
  Stage 4's exit gate is read as "region enabled by default," it inherits Stage 10/13 work.

## 5. Recommended plan changes

1. **Re-word item 1** from "Tables/browse *in the entry view*" to "Re-home the already-built
   `LexicalBrowseView` onto the Stage-3 shared control and prove 10k-row / 253-slice at 150% DPI" — and
   mark it explicitly blocked-on-Stage-3, while letting the rest of Stage 4 run in parallel.
2. **Re-word item 4** from "JSON serialization + …" to "**Finish** 9.x: override migrator (sparse
   `StableId` patches) + runtime-XML-disable (9.4) + override fixtures (9.3); serializer core already
   exists." Add the `canonical-view-definition-design.md` caveat: reserve ghost/conditional/chooser
   schema before freezing Layer-1 JSON.
3. **Re-anchor item 2** to the manifest §6 parity table: the open P0/P1 gaps are *custom fields (9.x B1),
   chooser write-back beyond morph type (6.3 follow-on), and StText/ORC (→Stage 9)* — most field types
   are already green. Drop the implication that references/pictures/pronunciation are unbuilt.
4. **Add an explicit exemplar-quality exit criterion:** unify the dual projector (18.11) and document
   `RegionViewingServices` + plugin burn-down as the copy-me contract. Without this, Stages 6/8 inherit a
   2524-line composer to clone. Consider moving 18.11 to Stage 1 (kit) if it should precede *all* surface
   hand-off.
5. **Disambiguate the exit gate.** State clearly whether Stage 4 done = "region manifest gates green +
   ready to enable" vs. "enabled by default." The latter pulls in 10.4/10.5/10.8 (global default-path
   validation) which are genuinely cross-stage. Recommend: Stage 4 = manifest §6 rows flip Partial→Pass
   and the region is *enable-able*; the global default flip stays in cutover (Stage 13).
6. **Note the 6.13 cross-change residual:** Stage 4 cannot be "fully green" until the *foundation
   change's* 8.2 (manual RTL + Khmer realized-window evidence) and 10.3 (full CI) land. Add that as an
   explicit cross-change dependency, not a Stage-4 task.

## 6. Open questions / risks

- **150% DPI / scroll / typing latency need real hardware** — headless Skia cannot produce them
  (`region-manifest.md` §5.4). Who owns the scaled-display run, and is it in CI or manual? This gates
  three manifest rows and is the most likely schedule slip.
- **Full-tree semantic comparison vs legacy DataTree (layout gate, manifest §6 "Partial")** has not been
  run for the composed view — only deterministic typed snapshots. This is the AI-hallucinated-parity
  risk the program flags as High/High (plan §9). It must be a real legacy-vs-Avalonia diff, not a
  self-snapshot.
- **Override migrator format-freeze risk:** freezing canonical Layer-1 JSON before ghost/conditional/
  chooser schema is reserved (per `xml-retirement-blockers.md`) bakes in a migration the customer
  override fixtures (9.3) will then break against.
- **Dual-projection debt (18.11)** is the quiet exemplar killer — easy to defer, expensive once two more
  surfaces have copied it.
- **Architecture-patterns.md drift:** the reference doc cites `ViewDefinitionJsonSerializer.cs` and
  `LexicalBrowseView.cs` as canonical/done while tasks 9.2/9.4 read as open. Reconcile the doc and the
  task list so the exemplar's own provenance is not self-contradictory.

## 7. Confidence

**High** on the open-task inventory and the boundary findings — these are quoted directly from
`tasks.md` and confirmed against real files (`ViewDefinitionJsonSerializer.cs`, `LexicalBrowseView.cs`,
`FullEntryRegionComposer.cs`, `region-manifest.md` §6). **Medium** on the exemplar-debt severity
(18.11) — I read the projector split from task text and file structure, not a line-by-line diff of both
projectors. **Medium** on the perf-hardware logistics — the gap is stated in the manifest but the
ownership/CI answer is not in-repo.
