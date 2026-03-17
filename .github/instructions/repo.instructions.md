---
applyTo: "**/*"
name: "repo.instructions"
description: "High-level repository rules that assist AI coding agents and automated code review."
---

# FieldWorks: Repo-wide Guidance (short)

## Purpose & Scope
Provide clear, concise, and enforceable rules that help AI coding agents and automated code review offer relevant suggestions and reviews.

## Rules (high-impact, short)
- Prefer the repository top-level build (`.\build.ps1`) and solution (`FieldWorks.sln`) for full builds.
- Keep localization consistent: use `.resx` and follow `crowdin.json` for crowdin integration.
- Before commit/push, run the existing VS Code task `CI: Full local check`.
- After rebase/merge/cherry-pick conflict resolution, run `CI: Whitespace check`; if it fixes files, restage them and rerun.
- When rewriting history or adding commits, run `CI: Commit messages` before pushing.
