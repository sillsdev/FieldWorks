# Graphite Transition Support — Design

## Context

Graphite is SIL's smart-font shaping technology for scripts OpenType handles poorly or not at all.
In this repo it lives in the native Views render path (`GraphiteEngine.cpp`, `GraphiteSegment`,
`RenderEngineFactory` selection), in writing-system storage (`IsGraphiteEnabled`,
`DefaultFontFeatures`, `FontEngines.Graphite`), in writing-system setup UI, in Gecko preview/PDF
(`gfx.font_rendering.graphite.enabled`), and in shipped/test fonts.

Two font realities drive this design:

1. **Most current SIL fonts are dual-engine.** Charis SIL, Doulos SIL, Scheherazade New, Annapurna
   and others carry both OpenType (`GSUB`/`GPOS`) and Graphite (`Silf`/`Glat`/`Gloc`/`Feat`) tables.
   For these, OpenType shaping is usually correct; only projects using Graphite-specific *feature
   settings* see differences.
2. **Some fonts are Graphite-only.** Awami Nastaliq (Nastaliq-style Arabic) is the prominent
   shipped example; older custom minority-language fonts exist in the field. For these, OpenType
   shaping produces visibly wrong text.

The Avalonia text stack (Skia + HarfBuzzSharp) ships HarfBuzz **without** Graphite2 enabled
(HarfBuzz's own docs: `-Dgraphite=enabled` is off by default), so Avalonia cannot shape Graphite
fonts today, and pretending otherwise was never an option. The question is what users experience
during the ~1-year coexistence, and when Graphite support actually ends.

Timeline anchors used below (WinForms decommissioning milestones):

- **M0** — coexistence begins (UIMode switch shipped; where we are now).
- **M1** — first editable Avalonia slice is product-ready (lexical-edit 6.x complete).
- **M2** — *mid-decommissioning*: Avalonia is the default for Lexical Edit and the majority of
  `RecordEditView` consumers. **This is the Graphite sunset point.**
- **M3** — WinForms (and native Views, including `GraphiteEngine`) deleted.

## Goals / Non-Goals

**Goals:**
- Graphite-using projects keep a fully working editing path (legacy surfaces) until M2, and a
  clearly communicated, tooling-supported migration path from M2 to M3.
- Avalonia surfaces never silently change rendering: Graphite-enabled writing systems on Avalonia
  get OpenType shaping plus an actionable warning, graded by how much actually breaks.
- Graphite policy stops gating UI-region completion; the gate becomes "warning + classification
  coverage", which is testable per region.
- Keep the native-engine boundary intact: no native Graphite/Views shaping inside Avalonia.

**Non-Goals:**
- No Graphite shaping in Avalonia (recorded contingency only — Path B).
- No legacy editor islands inside Avalonia surfaces (rejected — Path C).
- No changes to Gecko/PDF/export Graphite behavior; that classification stays in the lexical-edit
  change.

## The three paths considered

### Path A — Legacy harbor + graded Avalonia warning (RECOMMENDED)

Graphite stays exactly as it is on legacy surfaces; the global UI mode is the support boundary.
When an Avalonia surface is about to render a writing system classified as Graphite-affected, it
shapes with OpenType and raises a graded, actionable warning (see classification below). The
warning offers: switch this project to Legacy UI mode (full Graphite, one click), keep going with
OpenType shaping, or open font-migration guidance. Sunset at M2: warnings escalate to deprecation
notices, migration tooling ships, new projects can no longer enable Graphite; removal at M3 with
WinForms.

- **Pros:** zero native work; no new packaging risk; honest UX with a real escape hatch (legacy
  mode is a supported product surface, not a fallback apology); preserves every existing audit
  gate (no native Graphite on the Avalonia path); decouples font migration from UI migration;
  testable headlessly (classification + warning are managed code).
- **Cons:** Graphite-only-font projects get degraded rendering whenever they choose the Avalonia
  surface before migrating fonts; per-WS warnings must be carefully rate-limited to avoid nagging;
  dual-engine fonts with Graphite feature strings may render subtly differently and users may not
  notice the warning's significance.
- **Cost/risk:** low/low.

### Path B — Graphite shaping in Avalonia (HarfBuzz + Graphite2)

Ship a HarfBuzzSharp/Skia native build with Graphite2 enabled (or sideload `graphite2` +
`hb-graphite2` and a custom Avalonia `ITextShaperImpl`), so Avalonia shapes Graphite fonts
natively. The warning becomes rare (only Graphite *feature-UI* gaps).

- **Pros:** best user continuity — Graphite projects see correct text on both surfaces; no forced
  font migration at M2 (sunset could slip to M3 or beyond); Graphite2 is a shaping library, not the
  Views render pipeline, so it does not violate the native-Views decommissioning gate per se.
- **Cons:** custom native builds of HarfBuzzSharp/Skia must be produced, packaged, and re-produced
  for every Avalonia/HarfBuzzSharp update while pinned to 11.x — a year of fork maintenance;
  Avalonia's text shaping internals are not a stable extension point on 11.x; adds exactly the
  kind of bespoke native dependency the migration is trying to shed; testing burden (shaping
  parity fixtures against native `GraphiteEngine` output).
- **Cost/risk:** high/medium-high.
- **Status:** recorded contingency. Pivot triggers (any one): the M1 fixture scan finds Graphite-only
  fonts in active use beyond an acceptable threshold (proposed: >10% of partner projects or any
  strategic-language project with no OpenType replacement font); SIL Language Technology commits to
  maintaining an hb-graphite2 build for another product (shared cost); or Avalonia 12 adoption
  (post-WinForms) makes a custom shaper sustainable.

### Path C — Per-field legacy bridge (Graphite islands in Avalonia)

When a field's writing system is Graphite-enabled, the Avalonia surface hosts the legacy
native-Views/RootSite editor for that field only; other fields use Avalonia editors.

- **Pros:** pixel-true Graphite editing inside the new surface; no warning needed.
- **Cons (disqualifying):** violates the active-host contract (3.10) that the migration just made
  an audited invariant; keeps native Views render/editor code alive *inside* "migrated" regions,
  which the region manifests forbid; fights the coarse-hosting constraint (Avalonia 11.x
  cross-boundary focus/tab bugs are documented and unfixable on the pinned line); per-field interop
  islands multiply the dialog-ownership, focus, and DPI problems the coexistence constraints
  explicitly avoid; and it builds throwaway plumbing at the worst possible granularity.
- **Cost/risk:** high/high.
- **Status:** rejected. If full-fidelity Graphite editing is ever required inside an Avalonia-mode
  app, the correct unit is the *whole surface* (the user switches the host to legacy mode — which
  is Path A's affordance), not a field island.

**Decision: Path A**, with Path B held as a contingency behind the recorded pivot triggers, and
Path C rejected.

## Writing-system / font classification (drives warning severity)

Classification runs per writing system at surface-resolution time, from immutable inputs
(`IsGraphiteEnabled`, `DefaultFontFeatures`, and font-table sniffing of the resolved font file):

| Tier | Condition | Avalonia behavior |
|---|---|---|
| **G0 — unaffected** | `IsGraphiteEnabled` false, or font has no Graphite tables | Normal OpenType rendering; no message. |
| **G1 — dual-engine, no feature strings** | Graphite-enabled + font has both `GSUB`/`GPOS` and `Silf` tables + no Graphite feature settings | OpenType rendering; **Info** diagnostic only (logged, visible in WS setup, not a popup). |
| **G2 — dual-engine with Graphite feature strings** | As G1 but `DefaultFontFeatures` (or per-WS feature overrides) carry Graphite feature IDs | OpenType rendering; **Warning**: "Graphite font features for ⟨WS⟩ do not apply in the new editor; rendering may differ." Once per project session, actionable. |
| **G3 — Graphite-only font** | Font has `Silf` but no functional OpenType shaping for the script (e.g. Awami Nastaliq) | OpenType/fallback rendering will be wrong; **prominent Warning** before first render: recommend Legacy UI mode or a replacement font. Never suppressed permanently while the condition holds. |

Best-practice notes baked into the tiers:

- **Sniff tables, don't trust flags alone.** `IsGraphiteEnabled` is a user preference; the `Silf` +
  `GSUB`/`GPOS` table check is what predicts actual rendering damage. Both inputs feed the tier.
- **Never block, never silently degrade.** G2/G3 still render (the user may be triaging data, not
  typography); the warning carries the "switch to Legacy UI" affordance that restores full
  fidelity in one action.
- **Warnings are per writing system, not per field**, rate-limited to once per project session,
  and recorded as diagnostics so support staff can see them after the fact.
- **The warning text names the font and the writing system**, links the font-migration guidance
  (e.g. Awami Nastaliq → no current replacement: stay on legacy; Padauk/Charis feature users →
  OpenType equivalents table), and is localized like any product string (lexical-edit 6.11 rules).

## Sunset schedule and best practices

| Milestone | Graphite state | Required before entering |
|---|---|---|
| M0 → M1 | Fully supported on legacy surfaces; G1–G3 warnings active on any Avalonia surface | Classification service + warning UX shipped with the first Avalonia surface that can render user text |
| M1 → M2 | Same, plus WS-setup UI shows per-WS Graphite status and migration guidance | LDML/project fixture scan quantifying G2/G3 prevalence (the Path B pivot input); font-replacement policy published |
| **M2 (sunset)** | Deprecated: new projects cannot enable Graphite; existing projects see deprecation notice with timeline; migration tooling (feature-string → OpenType mapping where possible, font-replacement assistant) ships | Tooling tested on the fixture corpus; at least one release of advance notice; partner sign-off for strategic languages |
| M2 → M3 | Legacy surfaces still render Graphite for existing projects (no functional removal), but it is support-frozen | — |
| **M3** | Removed with WinForms/native Views (`GraphiteEngine.*`, render-engine selection, Gecko prefs) | Every G3 project contacted/migrated or explicitly accepted as frozen-version users |

Transition best practices (applies to whichever path):

1. **Measure before sunsetting.** The M1 fixture scan is the evidence that M2 enforcement is
   humane. If the scan surfaces heavy G3 usage, the pivot triggers fire *before* anyone is harmed.
2. **One support boundary, not many.** Fidelity is per-surface (legacy vs Avalonia), selected by
   the existing UI mode — never per-field or per-control. This keeps the mental model and the test
   matrix small.
3. **Keep storage authoritative and untouched.** `IsGraphiteEnabled`/`DefaultFontFeatures` are
   user data; classification reads them. Migration tooling rewrites them only on explicit user
   action, with undo.
4. **Tie warnings to diagnostics, not just UI.** Every G1–G3 determination is a structured
   diagnostic (same channel as the view-definition diagnostics), so parity bundles and support
   can audit what a user was told.
5. **Don't couple the audit gates to the policy.** The region-manifest forbidden-symbol audit
   (`GraphiteEngineClass` etc. never on the Avalonia path) stays exactly as is under every path —
   it is about *engine* isolation, which is true even while Graphite is fully supported.

## Risks / Trade-offs

- G3 users who never read warnings discover wrong rendering late → mitigate with the prominent
  pre-render warning, WS-setup status surface, and the M1 scan proactively identifying affected
  projects for direct contact.
- Warning fatigue for G1/G2 → mitigate with the tier system (G1 is log-only) and per-session
  rate limiting.
- Path B pivot fires late, after users migrated fonts unnecessarily → mitigate by running the
  fixture scan at M1 (early), not at M2.
- Sunset slips because tooling isn't ready → M2 enforcement is gated on the tooling, not the
  calendar; the deprecation *notice* can ship without enforcement.

## Open Questions

1. What is the acceptable G3 prevalence threshold for the Path B pivot (proposed >10% of partner
   projects — needs product owner confirmation)?
2. Does the M2 "new projects cannot enable Graphite" rule need a partner exemption mechanism?
3. Where does the font classification service live — `FwAvalonia` (LCModel-free, table sniffing
   only) with the WS-flag input injected, or alongside the 6.x writing-system text service?
4. Should the deprecation notice be in-app only, or also release notes + partner communication
   channel?
