## Why

> **Status note (2026-06-09).** The original proposal planned `datatree-model-view-separation` as
> Phase 1. Execution diverged: Phase 1 was built directly as the region-model path
> (`ViewDefinitionModel`/`LexicalEditRegionModel`) inside `lexical-edit-avalonia-migration`, bypassing
> DataTree internals entirely. Gate 1 has passed. `datatree-model-view-separation` is formally
> superseded as a migration gate (task 1.13 done 2026-06-09). The active vocabulary and gate
> definitions are in `design.md`; this proposal is preserved as the historical rationale.

Two planning sets exist for moving FieldWorks to Avalonia: an older DataTree model/view separation
(`datatree-model-view-separation`) and a newer end-to-end program
(`lexical-edit-avalonia-migration` + `fieldworks-avalonia-shell-migration`). They overlap and need a
single, ordered roadmap so the team works one plan, not three. The goal is to move the **whole**
application to Avalonia, starting with the main Lexical Edit view after a small proof of concept,
preserving **functional fidelity and density** (not pixel-perfect), with the new path behind a
**feature flag** so the same build runs either Avalonia or the legacy WinForms controls.

This change is the **umbrella roadmap**. It does not introduce code. It sequences the existing
changes into one minimal-risk path and defines the gates between them.

## What Changed (as-built)

- **Phase 0 (entry-point spike):** completed and folded forward. In-process host bridge proven; product
  wiring evidence exists. See `lexical-edit-avalonia-migration/poc-retiring.md`.
- **Phase 1 (first migrated region):** completed via `lexical-edit-avalonia-migration` sections 3–4.
  The boundary is the region-model path (`ViewDefinitionModel` → `LexicalEditRegionModel`), not the
  originally-planned DataTree extraction. `DataTree` is frozen on the legacy side. Gate 1 passed.
  See `avalonia-migration-roadmap/design.md` for the updated Gate 1 definition and vocabulary.
- **`datatree-model-view-separation`** is superseded as a migration gate. Optional legacy maintenance
  only; does not gate Avalonia feature work.
- **Phases 2–6** (continued `lexical-edit-avalonia-migration`): in progress. This is **Phase 1 of the
  two-phase split — derisk all functional behavior** with WinForms still switchable, everything hosted
  through the WinForms shell, and no code deleted (its remaining functional burn-down is
  `lexical-edit-avalonia-migration/tasks.md` §19).
- **Phase 7+** (shell / cutover): now owned by **`avalonia-end-game`** (Phase 2 of the split — the
  cutover), which **absorbs and supersedes `fieldworks-avalonia-shell-migration`** (2026-06-20) and adds
  the net48 → .NET 10 retarget and true Windows/macOS/Linux cutover. Gated on the Phase-1 functional
  parity burn-down reaching zero.

## Non-goals

- Re-deriving or duplicating the detailed requirements already captured in the referenced changes.
- Changing any default runtime behavior in this change (the roadmap is planning only).
- Committing to shell migration timing before the regional Lexical Edit gates are proven.
- Reopening the frozen seam decisions; this roadmap consumes them.

## Capabilities

### New Capabilities

- `avalonia-migration-roadmap`: The ordered, gated sequence that governs how the proof-of-concept,
  first migrated region, lexical-edit migration, and shell migration proceed.

## Referenced changes (not duplicated here)

- `lexical-edit-avalonia-migration/poc-retiring.md` — the folded-forward record of the Phase 0
  entry-point spike and its retirement.
- `lexical-edit-avalonia-migration` — the regional program spine (Phase 1 complete, Phases 2–6 in progress).
- `datatree-model-view-separation` — **superseded as Phase 1**; optional legacy maintenance only.
  See `datatree-model-view-separation/hybrid-alignment.md`.
- `avalonia-end-game` — **Phase 2 (cutover): Phase 7+ / shell**, kill WinForms, retarget net48 → .NET 10,
  and go true cross-platform (Windows/macOS/Linux). Absorbs and supersedes
  `fieldworks-avalonia-shell-migration`. Gated on the Phase-1 functional parity burn-down reaching zero.
- `fieldworks-avalonia-shell-migration` — **superseded by `avalonia-end-game`** (2026-06-20); preserved as
  historical detail for the shell composition, lifetime, command bridge, and decommissioning seams that
  `avalonia-end-game` consumes.
- `detail-controls-testability`, `retire-linux-era-view-shims`, `render-speedup-benchmark` —
  supporting/companion work that reduces risk but does not gate the main sequence.

## Impact

- Planning/process only. No source code, native code, or packaging changes in this change.
- Subsequent code impact is described in the referenced changes' own proposals and specs.
