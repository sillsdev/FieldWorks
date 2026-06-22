# Tasks

> **⚠ SUPERSEDED (2026-06-15)** by the full-removal decision (program §11.1). Tasks below that
> implement *retention/warn/sunset* are historical. **Salvaged & still live:** 1.3 and 1.4 (the
> G0–G3 classifier + its font fixtures) — repurposed to enumerate dropped scripts for the
> document-and-notify obligation; and the `DefaultFontFeatures` keep-for-OpenType-export rule.

## 1. Policy and Inventory (M0 → M1)

- [ ] 1.1 Adopt the Path A decision and record the Path B pivot-trigger threshold with the product owner (design.md Open Question 1); confirm the M2 definition ("Avalonia default for Lexical Edit + majority of `RecordEditView` consumers") as the sunset milestone.
- [ ] 1.2 Re-home the Graphite inventory tasks from `lexical-edit-avalonia-migration` section 5 (5.1–5.4) under this change: native code/assets, writing-system settings persistence, feature UI/storage classification, and the font-replacement/fallback policy. The Gecko/browser/PDF tasks (5.6/5.7) and the default-path native-engine audits (5.5/5.8, re-read as engine-isolation, not support-removal) stay with the lexical-edit change.
- [ ] 1.3 Build the writing-system/font classification service (tiers G0–G3): inputs `IsGraphiteEnabled`, `DefaultFontFeatures`/per-WS feature overrides, and font-table sniffing (`Silf`/`Glat` vs `GSUB`/`GPOS`) of the resolved font file; deterministic, immutable-input, structured-diagnostic output (same diagnostics channel as the view-definition pipeline). Decide its home (design.md Open Question 3) so the LCModel-free table sniffer can be tested in `FwAvaloniaTests`.
- [ ] 1.4 Validate the classifier against known fonts: Charis SIL/Doulos SIL/Scheherazade New (G1), a dual-engine font with Graphite feature settings fixture (G2), Awami Nastaliq (G3), and a Graphite-flag-without-Graphite-font fixture (G0).

## 2. Warning UX on Avalonia Surfaces (lands with the first user-text-rendering Avalonia surface)

- [ ] 2.1 Implement the graded warning: G1 log/setup-visible only; G2 once-per-project-session actionable warning; G3 prominent pre-render warning naming WS + font. All warnings carry the switch-to-Legacy-UI affordance (whole surface via the existing UI mode, never per-field) and a font-migration-guidance link; all texts localized per lexical-edit 6.11 rules with stable nonlocalized AutomationIds.
- [ ] 2.2 Record every G1–G3 determination as a structured diagnostic visible to support and included in Path 3 parity bundles.
- [ ] 2.3 Add headless tests: tier → warning behavior mapping, per-session rate limiting, G3 never permanently suppressed while the condition holds, and the legacy-mode affordance wiring through `LexicalEditSurfaceSelectionService`.
- [ ] 2.4 Replace the "Avalonia default blocked by Graphite" gate in region manifests with the "classification + warning coverage" gate; keep the forbidden-symbol audit (`GraphiteEngineClass`, `FwGrEngine`, `GraphiteSegment` never on the Avalonia path) unchanged.

## 3. Visibility and Measurement (M1 → M2 entry criteria)

- [ ] 3.1 Surface per-WS Graphite status (tier, font, what differs on the new editor) in the writing-system setup UI with migration guidance.
- [ ] 3.2 Run the LDML/project fixture scan quantifying G2/G3 prevalence across partner/test corpora; publish the result as the Path B pivot input and the M2 humane-enforcement evidence.
- [ ] 3.3 Publish the font-replacement policy: OpenType equivalents table for common Graphite feature uses; explicit "no replacement — stay on legacy until M3" entries (e.g. Awami Nastaliq) with owner contact.

## 4. Sunset (M2) and Removal (M3)

- [ ] 4.1 Ship migration tooling before M2 enforcement: Graphite feature-string → OpenType feature mapping where equivalents exist, font-replacement assistant; rewrites `IsGraphiteEnabled`/`DefaultFontFeatures` only on explicit user action, with undo; tested on the 3.2 fixture corpus.
- [ ] 4.2 M2 deprecation sequence: in-app deprecation notice with timeline for existing Graphite projects; block new Graphite enablement (with the exemption decision from design.md Open Question 2); at least one release of advance notice before enforcement.
- [ ] 4.3 Direct outreach to G3-classified strategic-language projects identified by the 3.2 scan before enforcement.
- [ ] 4.4 M3 removal (with WinForms/native Views deletion): `Src/views/lib/GraphiteEngine.*`, `GraphiteSegment`, render-engine Graphite selection, Gecko Graphite preference, Graphite-specific tests/sample assets/build artifacts; settings storage handled per the lexical-edit data-migration rules (values preserved, semantics retired).

## 5. Validation

- [ ] 5.1 Verify no Avalonia-path reference to native Graphite symbols at every milestone (existing region-manifest symbol audit — unchanged by this policy).
- [ ] 5.2 Verify warning coverage: every G2/G3 writing system in the fixture corpus produces its warning on an Avalonia surface, exactly once per session, with working legacy-mode affordance.
- [ ] 5.3 Verify legacy surfaces' Graphite rendering is bit-identical to pre-migration baselines (existing render-verification harness) at M1 and M2.
- [ ] 5.4 Verify M2 enforcement is gated on shipped tooling + completed scan + advance notice, not on the calendar.
