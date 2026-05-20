---
description: "Challenge a FieldWorks PR author before posting a PR; runs interactive branch review, writes .review/summary.md, and optionally prepares the PR."
agent: "agent"
argument-hint: "Optional branch purpose or PR goal"
---

Use the `challenge-pr-author` skill from `.github/skills/challenge-pr-author/SKILL.md` for the current branch.

Treat any text supplied with this prompt as the optional branch purpose or PR goal. If no purpose is available, ask for it during the skill setup. Load `.github/instructions/review-analyzer.instructions.md` for the analysis policy, then follow the skill workflow through `.review/summary.md` creation.
