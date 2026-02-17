# Agent docs refresh workflow

This note summarizes the detect → plan → draft flow for keeping `Src/**/AGENTS.md` aligned with source changes. It is optimized for tool-agnostic agent documentation workflows.

## Prerequisites
- Clean git workspace (commit code changes first).
- Python 3.11 available on PATH.
- Build outputs cached via `.cache/copilot/` (generated automatically).

## Commands
1. **Detect** stale folders:
   ```powershell
   python .github/detect_copilot_needed.py --base origin/release/9.3 --json .cache/copilot/detect.json --strict
   ```
2. **Plan** diffs + prompts:
   ```powershell
   python .github/plan_copilot_updates.py --detect-json .cache/copilot/detect.json --out .cache/copilot/diff-plan.json
   ```
3. **Inject auto change-log blocks** (optional, before manual edits):
   ```powershell
   python .github/copilot_apply_updates.py --plan .cache/copilot/diff-plan.json --folders Src/Foo Src/Bar
   ```
4. **Run the folder review prompt** (post-update): feed the relevant JSON slice and `AGENTS.md` path into `.github/prompts/copilot-folder-review.prompt.md` via your preferred agent.
5. **Validate** docs before committing:
   ```powershell
   python .github/check_copilot_docs.py --only-changed --fail
   ```

## Required frontmatter
Every `Src/**/AGENTS.md` needs deterministic review metadata so agents know when it was last validated:

```yaml
---
last-reviewed: YYYY-MM-DD
last-reviewed-tree: <tree-hash>
status: draft|verified
---
```

- `last-reviewed` updates when you complete a substantive review.
- `last-reviewed-tree` is the git tree hash for the folder (excluding `AGENTS.md`). Use the planner cache or `git rev-parse $(git rev-parse HEAD:Src/Foo)` if you need to compute it manually.
- `status` stays `draft` until an SME confirms accuracy; keep it honest.
- Optional keys such as `related-folders` or `tags` are fine but keep them sparse.

The scaffolder will populate these fields, but you are still responsible for keeping the values correct whenever the folder’s behavior changes.

## Accuracy & update thresholds
- Prefer `FIXME(<topic>)` over speculation; only remove TODO/FIXME notes once the statement is verified against source files or authoritative assets.
- Update agent docs whenever there is a **substantive change**: new public interfaces, architectural shifts, dependency adjustments, XML/XSLT contract updates, threading/perf model changes, or notable build/test infrastructure work.
- Skip doc churn for cosmetic diffs, comment-only edits, or bug fixes that do not affect public behavior—noise makes it harder to detect real regressions.
- Keep sections grounded in specific files (call them out again under `## References`) so reviewers can trace each claim.
- When folders contain only stubs or archived artifacts, say so plainly and cite the files so the next reviewer can confirm quickly.

## Cache layout
- `.cache/copilot/detect.json`: detect script output (optional).
- `.cache/copilot/diff-plan.json`: aggregated planner output.
- `.cache/copilot/diffs/<folder>.json`: per-folder cached diff (sharded for concurrent agents).
- Planner JSON now includes `project_refs` so you no longer need to embed large “Auto-Generated Project References” sections in AGENTS.md. Link to the JSON when reviewers need the exhaustive list.

Use `--refresh-cache` on the planner if the repo history rebased or caches look stale.

## Best practices
- Keep AGENTS.md free of ownership/team references—describe behaviors, not people.
- Let auto sections (`<!-- copilot:auto-change-log ... -->`) capture deterministic data; keep narrative sections human-curated.
- Run the folder-review prompt during CI or PR review to ensure docs and code stay aligned.
- For organizational folders, scaffold from `.github/templates/organizational-copilot.template.md` and keep content focused on navigation + checklists.
- Follow the AGENTS.md guidance from tool-agnostic editor rules (for example, Cursor’s AGENTS.md support) and keep instructions concise, scoped, and testable.
