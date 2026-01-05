# Copilot Instructions Modernization Checklist

This checklist operationalizes the multi-phase plan to align FieldWorks guidance with the latest Copilot instructions best practices.

See `.github/AI_GOVERNANCE.md` for the current documentation taxonomy and source-of-truth rules.

## Phase 1 — Inventory & Metrics
- [x] Script to inventory `Src/**/COPILOT.md` files (path, size, structure).
- [ ] (Removed) Instruction inventories/manifests are intentionally not maintained; `.github/instructions/` is a curated minimal set.

## Phase 2 — Repo-wide & Path-specific Refresh
- [x] Restructure `.github/copilot-instructions.md` using Purpose/Scope + concise sections by adding `repo.instructions.md` for agents and retaining long human doc.
- [x] Add missing instruction files from awesome-copilot templates and generate concise `*.instructions.md` for large modules (PowerShell, security, spec workflow, .NET).
- [x] Normalize existing `*.instructions.md` files to the recommended heading structure with sample code.
- [x] Keep each instruction file ≤ 200 lines by splitting topics as necessary (many generated files created).

## Phase 3 — COPILOT.md Modernization
- [ ] Introduce per-folder `copilot.instructions.md` (or equivalent) with `applyTo` for targeted guidance while retaining narrative `COPILOT.md`.
- [ ] Extend `Docs/copilot-refresh.md` workflow to enforce required sections and length caps.
- [ ] Add VS Code tasks / scripts to scaffold new folder instruction files from templates.

## Phase 4 — Discoverability & Linting
- [ ] Keep COPILOT.md validation and link checks in CI.
- [ ] Keep `.github/instructions/` small and focused on prescriptive constraints.

## Phase 5 — Adoption & Governance
- [x] Update README/CONTRIBUTING to describe instruction file taxonomy and contribution expectations (short section added).
