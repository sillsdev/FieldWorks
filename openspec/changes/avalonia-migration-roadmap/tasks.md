# Tasks

> This change is planning/sequencing only. "Done" means the roadmap, gates, and overlap resolution
> are recorded and the referenced changes are aligned to them. No runtime code here.

## 1. Establish the roadmap and gates

- [x] 1.1 Record the Hybrid decision and rationale (see `design.md` above; the original plan-comparison
      analysis this was recorded from has since been removed as a superseded decision record).
- [x] 1.2 Define the ordered sequence phase-0 spike → DataTree region → Lexical Edit → Shell with gates 0–2
      (`design.md`).
- [x] 1.3 Define the overlap resolution (SliceSpec ⊂ typed IR; IDataTreeView ⊂ two-adapter flag)
      (`design.md`, and `datatree-model-view-separation/hybrid-alignment.md`).

## 2. Stand up the entry-point change

- [x] 2.1 Record the Phase 0 entry-point spike artifacts and fold their surviving guidance into
      `lexical-edit-avalonia-migration/poc-retiring.md`.
- [ ] 2.2 Confirm the phase-0 spike flag, host-bridge, slice, and parity tasks match Gate 0 before the spike
      starts.

## 3. Align the referenced changes to the sequence

- [ ] 3.1 Add the hybrid-alignment note to `datatree-model-view-separation` marking it as Phase 1 (the
      first migrated region) and pointing `IDataTreeView`/`SliceSpec` at the lexical-edit seams.
- [ ] 3.2 Confirm `lexical-edit-avalonia-migration` Phase 3+ tasks consume the DataTree region output
      rather than redefining a parallel boundary.
- [ ] 3.3 Confirm `fieldworks-avalonia-shell-migration` remains gated on Gate 2 and does not start
      before the regional gates pass.

## 4. Validation

- [ ] 4.1 Verify each referenced change's gate definitions are consistent with gates 0–2 here.
- [ ] 4.2 Keep this roadmap updated as the phase-0 spike evidence lands and estimates firm up.
- [ ] 4.3 Run `CI: Full local check` before commit/push of roadmap and aligned docs.
