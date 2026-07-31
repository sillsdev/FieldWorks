# Retired Avalonia lesson cards

## Purpose

Preserve verified knowledge from retired Avalonia follow-up work without preserving its code, treating its implementation choices as current requirements, or turning it into an autonomous roadmap.

The immediate sources are PRs #965, #966, and #967, the removal and retirement work in PR #964, and the corresponding historical commits and OpenSpec material.

## Repository artifacts

Create `Docs/migration/lessons/` with:

- `README.md`: a capability-oriented discovery index.
- `TEMPLATE.md`: the required structure and human-review fields.
- `interlinear-analysis.md`: lessons from retired PR #965.
- `rule-formula-editors.md`: lessons from retired PR #966.
- `browse-table-activation.md`: lessons from retired PR #967 and the later removal of the dormant browse implementation.
- `avalonia-migration-pivot.md`: cross-cutting lessons from PR #964's scope correction and retirement work.

Each card records status, sources, human ownership, the question tested, observations, retired approaches, no more than five durable lessons, evidence required next time, its decision boundary, and explicit conclusions that must not be inferred.

Cards must be capability-first and code-free. Historical type names may appear only in source citations when needed for archaeology.

## Discovery

Future humans and agents must be able to find the cards without knowing an old PR number or branch name.

Discovery paths are:

1. `Docs/migration/lessons/README.md`, indexed by problem and capability vocabulary.
2. A prominent link from `Docs/migration/README.md`.
3. Guidance in `Src/AGENTS.md` requiring consultation before Avalonia migration planning.
4. A link from the FieldWorks WinForms-to-Avalonia migration skill, framed as historical constraints rather than implementation authority.
5. A concise link from PR #964's main description, with expanded context in its existing sticky provenance comment.

## Git and pull-request workflow

The work lives on `document-retired-avalonia-lessons`, created from the current `phase1-base`, and becomes a focused documentation PR targeting `phase1-base`.

After the documentation PR is pushed and has a stable URL:

1. Update PR #964's main description with a short lesson-index reference.
2. Update its existing sticky provenance comment in place; do not create another provenance comment.
3. Close PRs #965, #966, and #967 as superseded, linking the new documentation PR and the relevant lesson cards.
4. Leave the three remote branches intact as temporary archaeological references.

No product code, old tests, archived task lists, or branch commits are copied into this branch.

## Jira boundary

Lesson cards are durable institutional memory. Jira issues are execution records created only after a human approves a product outcome or a bounded discovery spike.

The lesson-card PR does not create implementation stories for the three retired PRs. A future Jira issue may cite a lesson card for constraints and evidence, but the issue must independently state its desired outcome and must not treat the historical implementation as authorized.

## Validation

Before publishing:

- Check every source commit and PR reference.
- Search the current tree to avoid claiming retired types or routes are present.
- Ensure each card distinguishes observation, rejected approach, durable lesson, and unresolved hypothesis.
- Ensure no card contains copied source code or an implementation checklist.
- Verify all repository links resolve.
- Inspect the final diff for scope and encoding damage.
- Use repository build/test scripts only if a changed validation surface requires them; documentation-only changes do not require a product build.

## Non-goals

- Do not restore, rebase, cherry-pick, or rewrite code from PRs #965-#967.
- Do not delete their branches.
- Do not endorse their UI architecture, class layout, activation scope, or completion claims.
- Do not recreate the removed OpenSpec changes or their task checklists.
- Do not create Jira implementation work without a separate human product decision.
- Do not use cards as a substitute for current-tree discovery, legacy characterization, domain-owner decisions, or real product validation.
