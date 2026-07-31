# Retired Avalonia lesson cards

## Purpose

Preserve verified knowledge from retired Avalonia follow-up work without preserving its code, treating its implementation choices as current requirements, or turning it into an autonomous roadmap.

The immediate sources are PRs #965, #966, and #967, the removal and retirement work in PR #964, and the corresponding historical commits and OpenSpec material.

## Repository artifacts

Create a repository-wide lesson library under `Docs/lessons/`:

- `README.md`: the repository-wide index of lesson areas.
- `TEMPLATE.md`: the shared structure and human-review fields for every lesson area.
- `avalonia-migration/README.md`: a capability-oriented index for this migration.
- `avalonia-migration/interlinear-analysis.md`: lessons from retired PR #965.
- `avalonia-migration/rule-formula-editors.md`: lessons from retired PR #966.
- `avalonia-migration/browse-table-activation.md`: lessons from retired PR #967 and the later removal of the dormant browse implementation.
- `avalonia-migration/migration-pivot.md`: cross-cutting lessons from PR #964's scope correction and retirement work.

Each card records status, sources, human ownership, the question tested, observations, retired approaches, no more than five durable lessons, evidence required next time, its decision boundary, and explicit conclusions that must not be inferred.

Cards must be capability-first and code-free. Historical type names may appear only in source citations when needed for archaeology.

## Discovery

Future humans and agents must be able to find the cards without knowing an old PR number or branch name.

Discovery paths are:

1. `Docs/lessons/README.md`, which lets future lesson areas sit alongside Avalonia migration rather than treating one migration as the permanent top-level category.
2. `Docs/lessons/avalonia-migration/README.md`, indexed by problem and capability vocabulary.
3. One general link from the root `AGENTS.md` to `Docs/lessons/README.md`; repository guidance must not encode topic-specific lesson routing.
4. Avalonia migration skills linking directly to the Avalonia lesson index, framed as historical constraints rather than implementation authority.
5. A concise link from PR #964's main description, with expanded context in its existing sticky provenance comment.

## Git and pull-request workflow

The lesson framework and migration-skill references land directly on `phase1-base` as part of PR #964. This keeps the new skills and the lessons they depend on in one review and one merge boundary. The temporary `document-retired-avalonia-lessons` branch is not published as a separate PR.

After the lesson commits are pushed to PR #964:

1. Update PR #964's main description with a short lesson-index reference.
2. Update its existing sticky provenance comment in place; do not create another provenance comment.
3. Close PRs #965, #966, and #967 as superseded, linking PR #964 and the relevant lesson cards.
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
