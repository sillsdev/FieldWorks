# Agent docs refresh workflow

FieldWorks uses a minimal AGENTS model. There is no hash/frontmatter pipeline and no AGENTS CI gate.

## Current model

- Keep only the core files: `AGENTS.md`, `.github/AGENTS.md`, `Src/AGENTS.md`, `FLExInstaller/AGENTS.md`, `openspec/AGENTS.md`.
- Keep content requirement-only and short.
- Use `.github/instructions/*.instructions.md` for strict rules.
- Use `Repository.Intelligence.Graph.json` for deterministic structure/build/test topology.
- Keep `Repository.Intelligence.Graph.json` committed (do not gitignore it).

## Refresh process

1. Update one of the core AGENTS files only when behavior or process changed.
2. Regenerate the graph:
   ```powershell
   .\Build\Agent\Generate-RepositoryIntelligenceGraph.ps1 -OutputPath Repository.Intelligence.Graph.json
   ```
3. Run relevant build/test validation for touched areas.
4. Commit docs + graph together when guidance or structure changed.

## What not to do

- Do not reintroduce per-folder AGENTS sprawl.
- Do not add hash metadata or stale-check scripts.
- Do not block CI on AGENTS freshness.
