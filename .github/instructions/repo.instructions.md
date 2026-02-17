---
applyTo: "**/*"
name: "repo.instructions"
description: "High-level repository rules that assist Copilot coding agent and Copilot code review."
---

# FieldWorks: Repo-wide Guidance (short)

## Purpose & Scope
Provide clear, concise, and enforceable rules that help Copilot code review and coding agents offer relevant suggestions and reviews.

## Rules (high-impact, short)
- Prefer the repository top-level build (`.\build.ps1`) and solution (`FieldWorks.sln`) for full builds.
- Keep localization consistent: use `.resx` and follow `crowdin.json` for crowdin integration.

## Examples (Quick)
- When adding a new project, update `FieldWorks.proj` and verify that `Build/Orchestrator.proj` phases remain valid.
