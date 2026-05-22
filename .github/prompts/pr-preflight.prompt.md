---
description: "Run FieldWorks PR preflight for the current branch: review readiness, author interview, validation evidence, .review/summary.md, and optional PR preparation."
agent: "agent"
argument-hint: "Optional branch purpose or PR goal"
---

Use the `pr-preflight` skill from `.github/skills/pr-preflight/SKILL.md` for the current branch.

Treat any text supplied with this prompt as the optional branch purpose or PR goal. If no purpose is available, ask for it during the skill setup. Load `CONTEXT.md`, `.github/context/codebase.context.md`, and `.github/instructions/review-analyzer.instructions.md`, then follow the skill workflow through `.review/summary.md` creation and, when posting or updating a PR, a Quick Summary-first PR description.
