# Copilot and AI guidance governance

## Purpose
This repo uses a **Copilot-first** documentation strategy:
- Component knowledge lives with the component (`Src/**/COPILOT.md`).
- A small set of scoped instruction files in `.github/instructions/` provides **prescriptive, enforceable constraints**.
- `.github/copilot-instructions.md` is the short “front door” that links to the right places.
- Agent definitions in `.github/agents/` and role chatmodes in `.github/chatmodes/` describe **behavior/persona**, not system architecture.

## Source of truth
- **Component architecture & entry points**: `Src/<Component>/COPILOT.md`
- **Repo-wide workflow** (how to build/test, safety constraints): `.github/copilot-instructions.md`
- **Non-negotiable rules** (security, terminal restrictions, installer rules, etc.): `.github/instructions/*.instructions.md`

## No duplication rule
- Do not copy component descriptions into `.github/instructions/`.
- Do not restate rules in multiple places. Prefer linking.
- If a rule must be enforced by Copilot for a subtree, add a scoped `.instructions.md`; otherwise document it in the relevant `COPILOT.md`.

## What goes where

### `.github/copilot-instructions.md`
Use for:
- One-page onboarding for Copilot: build/test commands, repo constraints, and links.
- Pointers to the curated instruction set and the component docs.

### `.github/instructions/*.instructions.md`
Use for:
- Prescriptive constraints that must be applied during editing/review.
- Cross-cutting rules that prevent expensive mistakes (security, terminal command restrictions, installer rules, managed/native boundary rules).

**Curated keep set (intentionally small):**
- `build.instructions.md`
- `debugging.instructions.md`
- `installer.instructions.md`
- `managed.instructions.md`
- `native.instructions.md`
- `powershell.instructions.md`
- `repo.instructions.md`
- `security.instructions.md`
- `terminal.instructions.md`
- `testing.instructions.md`

### `Src/**/COPILOT.md`
Use for:
- Where to start (entry points, key projects, typical workflows).
- Dependencies and cross-component links.
- Tests (where they live, how to run them).

Baseline expectations for a component COPILOT doc:
- **Where to start** (projects, primary entry points)
- **Dependencies** (other components/layers)
- **Tests** (test projects and the recommended `./test.ps1` invocation)

### `.github/agents/` and `.github/chatmodes/`
Use for:
- Role definitions, boundaries, and tool preferences.
- Do not put component architecture here; link to the component `COPILOT.md`.

## Adding a new scoped instruction file
Add a new `.github/instructions/<name>.instructions.md` only when:
- The guidance is prescriptive (MUST/DO NOT), and
- It applies broadly or to a subtree, and
- It would be harmful if Copilot ignored it.

Otherwise, update the appropriate `Src/**/COPILOT.md`.
