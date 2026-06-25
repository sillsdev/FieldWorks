# Cross-comparison synthesis — 13-stage review

> Synthesis of the 13 per-stage reviews under `reviews/stage-NN-*.md` (all written 2026-06-15
> by independent subagents, each grounded in repo inspection + targeted web research). This doc
> cross-compares their findings, resolves conflicts, and drives the consolidated update to
> `complete-migration-program.md`. Read the per-stage files for the full evidence + citations.

## 1. Headline outcomes

1. **The 13 stages and their sequencing are sound; the sizing is not.** Eight stages
   (3, 6, 7, 8, 9, 10, 11, 13) are too coarse to be single JIRA epics and need decomposition into
   gated sub-epics. The *order* the plan chose — including the contested ones (dialogs before shell;
   runtime jump late) — is repeatedly confirmed as correct, often *more* correct than the plan argued.
2. **Much of the "to build" scope is already built.** Reviewers found the JSON view-def serializer,
   the read-only browse view (10k-row-proven), custom-field rendering, the host bridge, the command-bridge
   seam, the AutomationId convention, and the chooser virtualization already exist. Several stages are
   *finish/re-home/generalize*, not *build*. The plan's verbs overstate remaining work in 1, 2, 3, 4.
3. **One decision the user already made now collides with a hard reality** (Graphite → Awami Nastaliq;
   see §4) and must be re-opened. Everything else is plan-mechanics.

## 2. Recurring cross-cutting themes (appeared in ≥3 reviews)

- **Decompose into consumer-gated sub-milestones.** The most common recommendation. Stages should ship
  in slices that unblock specific downstream stages, not as monoliths (esp. 3→4/7/8, 9→6/7, 11→all).
- **Stage 9 is the gravity well.** Reviews of 6, 7, 9, 10 all pull work *into* Stage 9 (morphology
  document editors, interlinear/sandbox constructs, Graphite-coverage proof) and warn its scope text is
  too narrow for what depends on it. Stage 9 is the single biggest correctness risk in the plan.
- **The as-built stack is C# code-behind on net48, with zero `.axaml` and no CommunityToolkit.Mvvm.**
  Decision §11.3 (dialogs use MVVM + compiled bindings) introduces a *new toolchain* that is unscoped in
  Stage 1 and lands on juniors in Stage 5. Flagged by Stage 1, 5, and 6 reviews.
- **Stage 1 must deliver two kits, not one** — region/IR scaffolding *and* MVVM-dialog scaffolding — and
  its validation gate (migrate a trivial dialog) currently exercises only the missing one.
- **Modernized-Fluent look (§11.4) conflicts with `architecture-patterns.md` §12** ("mimic legacy
  density"). The skill reference needs a one-line correction (follow-up, not blocking).
- **"Av12-ready during coexistence" is not literally achievable** — code running on Avalonia 11/net48
  can't avoid 11-only APIs; the achievable posture is *confining 11-only APIs (clipboard, binding, focus,
  theming) to named seams*, enforced by a Stage 2/11 exit gate. (Stages 2, 11, 12.)

## 3. Conflicts & double-bookings found → resolutions

| Conflict | Reviews | Resolution |
| --- | --- | --- |
| "Generalize `LexicalEditSurfaceSelectionService` → app-wide registry" claimed by **both** Stage 1 and Stage 2 | 1, 2 | Owns **Stage 2**. Stage 1 consumes it. |
| Dictionary **preview** double-booked: Stage 8 ("config preview wiring") vs Stage 10 ("preview replacement") | 8, 10 | Preview *rendering replacement* = **Stage 10**; Stage 8 (8b) consumes the replaced preview. Remove "preview wiring" from Stage 8 scope. |
| **Find/Replace + Styles dialogs** assigned to junior Stage 5, but both host `IVwRootSite`/`SimpleRootSite` | 5, 9 | Re-tier to **Stage 9-dependent** (Tier C), not junior Stage 5. |
| Stage 13 "retire IVwRootBox/IVwGraphics/IVwEnv COM surface" too broad — `ViewsInterfaces.cs` also defines `IVwCacheDa` (data-access) used by 45+ projects | 13 | **Split ViewsInterfaces**: keep data-access (`IVwCacheDa`) behind a seam; delete only render interfaces. |
| Graphite branch deletion in **Stage 10** (`RenderEngineFactory`) breaks **legacy** WinForms+Views surfaces too | 9, 10, 13 | **Native Graphite deletion → Stage 13** (with native Views). Stage 10 only removes Graphite from the *Avalonia/managed* path + classifies coverage. |
| Stage 6 "Grammar/Morphology" mixes detail-fields, **Views document editors**, and **browse bulk-editors** | 6 | Split 6a (lexicon detail), 6b (morph/grammar doc editors → gated on **Stage 9**), 6c (FdoUi bulk → **Stage 8/browse**, gated on **Stage 3**). |
| Stage 9 scope omits the constructs **Stage 7** needs (in-memory presentation cache/`CachePair`, hit-test combo editing, aligned interlinear grid) | 7, 9 | **Expand Stage 9** scope + add a named Sandbox/interlinear sub-spike to 9.0. |

## 4. NEW product decision required — Graphite / Awami Nastaliq (raise to user)

Decision §11.1 ("Graphite fully removed; HarfBuzz/managed only") was made before the engine review.
The Stage 9 review establishes (with sources): HarfBuzz covers the large majority of formerly-Graphite
scripts (SIL even dropped Graphite from Charis/Doulos v7 in 2025), **but Awami Nastaliq (Urdu/Arabic
Nastaliq) is Graphite-only by design** — OpenType lacks the collision-avoidance Nastaliq needs and SIL
has *no* OpenType replacement planned. HarfBuzz does not implement Graphite (`hb-graphite2` only delegates
to external `libgraphite2`). These are exactly FieldWorks' minority-language users.

**Implication:** "Graphite fully removed" may strand a real user population. Options:
(a) accept the loss (document, notify, provide migration guidance);
(b) retain a narrow Graphite shaping escape-hatch behind a seam for G3 scripts only, contradicting
"fully managed only";
(c) gate the *final* removal on an external OpenType-Nastaliq solution existing.

This is a **product/values call, not an engineering one.**

**RESOLVED 2026-06-15 — accept the loss, document + notify.** Graphite is removed entirely; no escape-hatch,
no gating on a Nastaliq solution. The Stage 9.0 LDML **G0–G3 coverage scan** (salvaged from
`graphite-transition-support`) is still required — not as a go/no-go gate, but to **enumerate the exact
dropped-script list + affected projects** so the program can **document the loss and notify affected users
with migration guidance** before removal ships (a Stage 10B / Stage 13 deliverable). Native
`GraphiteEngineClass` deletion stays in Stage 13 (legacy surfaces use it during coexistence).

## 5. Per-stage verdicts (one line each)

| # | Verdict | Key action |
| --- | --- | --- |
| 1 | Feasible; under-scoped for the dialog/MVVM kit | Two kits; redefine gate to prove both; "version" not "freeze" the seam catalog; mark region template provisional until 4 closes |
| 2 | Feasible; mostly generalization of existing assets | Own the app-wide surface registry + surface-census; name the ownership ports as single source of truth for 11; dual-run = CI matrix |
| 3 | **Mis-sized** — tree mostly solved, editable **table** is the real work (read-only today) | Re-scope to editable table + row chrome; ship 3a (read@scale) → 3b (editable, unblocks 4) → 3c (bulk/filter, unblocks 8); promote custom AutomationPeer to first-class |
| 4 | Scope **stale** — much already built | Reword 1&4 "re-home/finish"; anchor to manifest §6 Partial rows; add exemplar-quality exit (unify dual projector, document copy-me contract) |
| 5 | Modal tension **resolved** (host-wrapped body in WinForms-owned Form); not blocked | Make host-wrapped-body the first rule; re-tier A/B/C (Find/Replace+Styles→C/Stage 9); add MVVM tooling to Stage 1 gate |
| 6 | **Mis-bundled** across 3 substrates | Split 6a/6b/6c; add edges S9→6, S3→6, S5→6; promote `FwDialogLauncherField` to a `RegionFieldKind` |
| 7 | **Mis-sized** — 5 surfaces, 1–2 orders of magnitude apart in Views coupling | Split 7A (interlinear+sandbox, hard-blocks on extended 9), 7B (chart: port logic as-is + Stage 3 grid), 7C (concordance/stats), 7D (import → Stage 5/MVVM) |
| 8 | **Grab-bag** | Split 8a (notebook/lists/bulk-edit, needs Stage 3) + 8b (dict-config dialogs, MVVM); move preview to Stage 10; bulk-edit is the real engineering item |
| 9 | **Long pole, not one stage**; scope too narrow | Decompose 9.0 spike→9.1 StText→9.2 selection/caret→9.3 layout/box→9.4 embedded; reframe as DELTA over field foundation; add interlinear/sandbox + G0–G3 scan to 9.0 |
| 10 | **Two stages in one**; legacy-breakage risk | Split 10A (Gecko/PDF/preview) + 10B (Graphite classify/managed-path removal); native Graphite deletion → Stage 13; keep `DefaultFontFeatures`; decouple XULRunner startup first |
| 11 | **Second program in one row** | Decompose 11a–11f; state Stage 2/11 split (2=ports/contracts, 11=implement+shell-scope+switch); add dialog-modality re-host task; 5→11 is a finishing edge (ordering correct) |
| 12 | Late sequencing **forced** (Av12 dropped net48) — correct | Don't split net10/Av12 but add intermediate "green net10 + Avalonia 11.3.17" checkpoint; fix stale "net48/net8 multi-target" wording (repo is uniformly net48); add NUnit 3→4; "Av12-delta-localized" |
| 13 | **Five workstreams in one** | Split 13a (flip+bake, reversible) / 13b (delete, irreversible, gated on 13a) / 13c (cross-platform+gates); split ViewsInterfaces; leaf-first deletion runbook; `retire-linux-era-view-shims` lands first |

## 6. Revised stage/sub-epic map (for JIRA)

```
1  Platform & enablement kit        → 1-region-kit, 1-dialog-mvvm-kit, 1-shared-evidence-base, 1-runbook
2  Coexistence spine & contracts     → 2-generalize-host, 2-surface-registry+census, 2-command-bridge, 2-theming, 2-ownership-ports
3  Editable table + row chrome       → 3a-read@scale, 3b-editable-cells, 3c-bulk/checkbox/filter, 3-automationpeer
4  Finish Lexical entry (exemplar)   → 4-rehome-table(3b), 4-latency+150dpi, 4-override-migrator/xml-disable, 4-exemplar-contract
5  Dialogs & choosers                → 5A-junior(small,Views-free), 5B-mid(wizards/WS/props), 5C→Stage9(Find/Replace,Styles)
6  Lexicon + grammar/morphology      → 6a-lexicon-detail, 6b-morph-doc-editors→S9, 6c-fdoui-bulk→S8
7  Texts & Words / Interlinear       → 7A-interlinear+sandbox(S9), 7B-chart(S3+port), 7C-concordance/stats, 7D-import(S5)
8  Notebook/Lists/Dict-config        → 8a-notebook/lists/bulk(S3), 8b-dictconfig-dialogs(MVVM)
9  Managed document/text engine      → 9.0-spike+G0–G3, 9.1-StText, 9.2-selection/caret, 9.3-layout/box, 9.4-embedded
10 Browser/PDF + Graphite path       → 10A-gecko/pdf/preview, 10B-graphite-classify+managed-path
11 Application shell                 → 11a-lifetime/windowing, 11b-mainxml-compiler, 11c-command/state, 11d-nav/panes, 11e-screen-registry, 11f-startup/installer/switch
12 Runtime modernization             → 12-net10-port → (green net10+Av11.3.17 checkpoint) → 12-av12-bump → 12-nunit4
13 Cutover + decommission + xplat     → 13a-flip+bake, 13b-delete(leaf-first,+ViewsInterfaces split,+native Graphite), 13c-xplat+final-gates
```

## 7. Sequencing corrections (edges to add)

- `S9 → S6` (morphology document editors) — **missing, biggest graph error.**
- `S3 → S6` (FdoUi bulk editors), `S5 → S6` (launcher dialogs).
- `S3 → S8`, `S9 → S8` (bulk-edit + any Views-coupled list editors).
- `S9 (extended) → S7A` make explicit; `S3 → S7B/7C` (chart + concordance grids).
- Native Graphite + native Views deletion both **→ S13**, not S10/S9.
- `retire-linux-era-view-shims → S13` (narrow prerequisite, preserves VwTextStore/IViewInputMgr/ManagedVwDrawRootBuffered).

## 8. Required follow-ups outside the plan doc

- Correct `architecture-patterns.md` §12 wording (legacy-mimic → modernized-look-allowed) to match §11.4.
- Add superseded banners to all four files of `graphite-transition-support`; salvage its G0–G3 classifier
  + font-outreach obligation into Stage 9.0 / Stage 10B.
- `fieldworks-managed-netfx-review` skill premise (net48-vs-net8, C#7.3) is stale (repo defaults C#8,
  uniformly net48) — refresh when Stage 12 starts.
