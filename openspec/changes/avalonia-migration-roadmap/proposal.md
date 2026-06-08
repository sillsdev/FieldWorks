## Why

Two planning sets exist for moving FieldWorks to Avalonia: an older DataTree model/view separation
(`datatree-model-view-separation`) and a newer end-to-end program
(`lexical-edit-avalonia-migration` + `fieldworks-avalonia-shell-migration`). They overlap and need a
single, ordered roadmap so the team works one plan, not three. The goal is to move the **whole**
application to Avalonia, starting with the main Lexical Edit view after a small proof of concept,
preserving **functional fidelity and density** (not pixel-perfect), with the new path behind a
**feature flag** so the same build runs either Avalonia or the legacy WinForms controls.

This change is the **umbrella roadmap**. It does not introduce code. It sequences the existing
changes into one minimal-risk path, defines the gates between them, and resolves the overlap between
the two plans (the DataTree split becomes the concrete first migrated region inside the lexical-edit
program).

## What Changes

- Adopt the **Hybrid** approach: the lexical-edit program is the spine (typed view definition,
  seams, parity automation, Graphite/native decommissioning, two-adapter flag, then the shell); the
  DataTree model/view split is executed as the **first concrete migrated region** inside it.
- Add a **proof-of-concept spike** (`lexical-edit-avalonia-poc-spike`) as the entry point, before the
  regional migration, to de-risk the host bridge, fidelity/density, and the dual-run flag.
- Define the **ordered sequence and gates** across all changes so no phase starts before its
  predecessor's evidence exists.
- Reconcile vocabulary between the two plans: Plan A's `SliceSpec` is a concrete realization of Plan
  B's typed view-definition node; Plan A's `IDataTreeView` is selected by Plan B's two-adapter flag.
- Keep the comparison analysis (`Docs/avalonia-migration-approach-comparison.md`) as the rationale of
  record for choosing the Hybrid.

## Non-goals

- Re-deriving or duplicating the detailed requirements already captured in the referenced changes.
- Changing any default runtime behavior in this change (the roadmap is planning only).
- Committing to shell migration timing before the regional Lexical Edit gates are proven.
- Reopening the frozen seam decisions; this roadmap consumes them.

## Capabilities

### New Capabilities

- `avalonia-migration-roadmap`: The ordered, gated sequence and overlap resolution that governs how
  the proof-of-concept, DataTree region split, lexical-edit migration, and shell migration proceed.

## Referenced changes (not duplicated here)

- `lexical-edit-avalonia-poc-spike` — the entry-point proof of concept (this roadmap's Phase 0).
- `datatree-model-view-separation` — the first concrete migrated region (this roadmap's Phase 1).
- `lexical-edit-avalonia-migration` — the regional program spine (Phases 2–6).
- `fieldworks-avalonia-shell-migration` — the application-wide shell migration (Phase 7+), gated.
- `detail-controls-testability`, `retire-linux-era-view-shims`, `render-speedup-benchmark` —
  supporting/companion work that reduces risk but does not gate the main sequence.

## Impact

- Planning/process only. No source code, native code, or packaging changes in this change.
- Subsequent code impact is described in the referenced changes' own proposals and specs.
