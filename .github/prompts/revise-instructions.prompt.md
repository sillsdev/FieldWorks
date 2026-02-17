# Instruction & COPILOT Refresh (Copilot coding agent prompt)

**Purpose**: Guide Copilot coding agents through a single, repeatable workflow that modernizes `.github/instructions/*.md` files and `Src/**/COPILOT.md` docs. The agent should leave every touched path with clear Purpose/Scope, actionable rules, examples, and up-to-date folder guidance.

**Inputs** (optional):
- `base_ref` — git ref to diff against (defaults to repo default)
- `status` — `draft` or `verified` for COPILOT frontmatter (default `draft`)

## Workflow
1. **Assess scope**
	- Run `python .github/detect_copilot_needed.py --strict [--base origin/<branch>]` to list folders whose code changed without COPILOT updates.
	- Scan `.github/instructions/manifest.json` to spot instruction files missing Purpose/Scope or exceeding 200 lines.
2. **Refresh `.github/instructions/*.md` files**
	- For each file, enforce the skeleton below with Purpose & Scope, Key Rules, and Examples.
	- Keep `applyTo`, `name`, and `description` accurate; omit unsupported keys like owners or excludeAgent.
	- Split oversized content into multiple files whose `applyTo` patterns map cleanly to repo paths.
3. **Update `Src/**/COPILOT.md` content**
	- If scaffolding is stale, run `python .github/scaffold_copilot_markdown.py --status <status> [--ref <commit>]` to restore headings and frontmatter.
	- Follow the detect → plan → validate workflow in `Docs/copilot-refresh.md` (Comprehension → Contracts → Synthesis guidance) and pull details directly from source files.
	- When a COPILOT exceeds ~200 lines, summarize it into a new `.github/instructions/<folder>.instructions.md` so Copilot reviews stay concise.
4. **Validate**
	- Execute `python scripts/tools/update_instructions.py` (inventory + manifest + validator).
	- Run `python .github/check_copilot_docs.py --only-changed --fail --verbose` to ensure COPILOT docs match the skeleton.
	- Fix any warnings before proceeding.
5. **Deliver**
	- Compose a draft PR summarizing updated instruction files and COPILOT folders touched, noting key rule changes and validation commands run.

## Instruction skeleton

```
---
applyTo: "<apply-to-pattern>"
name: "<short-file-id>"
description: "Short description"
---

# Title

## Purpose & Scope
- Brief description

## Key Rules
- concise rules

## Examples
- small code snippets that clarify the rule
```

Always preserve original intent, remove duplicate prose, and keep examples grounded in real files or commands from this repo.
