---
name: pr-preflight
description: "Use when preparing a FieldWorks branch or pull request for review: pre-PR review, branch readiness, author interview, review summary generation, validation evidence, or PR description preparation."
argument-hint: "Optional branch purpose or PR goal"
user-invocable: true
---

# PR Preflight

Use this skill when the user wants an interactive FieldWorks branch review before posting or updating a PR.

This is the orchestration layer. The review policy lives in `.github/instructions/review-analyzer.instructions.md`, shared terminology lives in `CONTEXT.md`, and specialized reviewer agents may be used for independent read-only passes.

## Goals

- Analyze the branch diff from `origin/main` using FieldWorks review policy.
- Use specialist agents for deep read-only review where they fit the changed files.
- Challenge the author on risks, assumptions, validation gaps, and design understanding.
- Record author explanations, dismissed findings, unresolved concerns, and in-review fixes.
- Write a fresh `.review/summary.md` that a reviewer can use as a meeting agenda.
- Optionally commit, push, and create or update a PR with a Quick Summary-first description only after the author confirms readiness.

## Start Here

First tell the author what will happen:

> **Here's what this preflight will do:**
>
> 1. **Setup** - Check your branch and ask a couple of quick questions
> 2. **Analysis** - Review contracts/correctness, managed UI/localization, native interop/COM, build/test/installer risk, and validation evidence
> 3. **Interview** - Walk through findings and challenge the reasoning
> 4. **Output** - Write `.review/summary.md` and optionally create or update a PR
>
> During the interview, you can explain changes, dismiss findings with reasons, ask me to fix something, or say you are unsure. I will record all of that for the reviewer.

## Setup

1. Determine the review model name. Use `GitHub Copilot` when running in Copilot. Do not invent AI co-author trailers.
2. Validate the current branch:
   - Run `git branch --show-current`.
   - If the branch is `main`, stop and tell the author to run this from a feature branch.
3. Check working tree state with `git status --porcelain`.
   - If there are uncommitted changes, explain that any fixes made during the review will be staged and may be committed with existing changes at the end.
   - Ask whether the author wants to commit existing work first or continue with it included.
4. Compute the diff range against `origin/main`.
   - Run `git fetch origin --quiet`.
   - Compute `MERGE_BASE` with `git merge-base origin/main HEAD`.
   - Compute `HEAD_SHA`, `FILE_COUNT`, and `INITIAL_COMMIT_COUNT`.
   - List changed files with `git diff --name-only <merge-base>`.
   - If this fails, ask the author which base branch to use.
5. Check whether `.review` is ignored.
   - If `.review` is missing from `.gitignore`, ask whether to add it now.
   - Do not combine this question with the purpose question.
6. Determine the branch purpose.
   - Use any purpose supplied in the prompt invocation.
   - If none was supplied, ask: "What is the overall purpose of these changes? Please describe it in your own words."

## Context And Language Check

Before analysis, load `CONTEXT.md` and `.github/context/codebase.context.md`.

If the branch purpose, PR title, plan, or spec uses overloaded FieldWorks terms such as `project`, `model`, `view`, `app`, `context`, `review`, or `validation`, apply the `grill-with-docs` discipline before writing the summary:

- Clarify the term with the author.
- Ground the term in code, docs, tests, or build files.
- Update `CONTEXT.md` only for durable shared language decisions.
- Carry the clarified terms into findings, interview notes, and PR copy.

## Analysis

Load `.github/instructions/review-analyzer.instructions.md` and run all four required passes:

- Contracts, compatibility, and correctness.
- Managed UI, C#, and localization.
- Native, COM, and boundary safety.
- Build, tests, CI, dependencies, and installer.

Use specialist agents as independent read-only reviewers when the changed files justify them and the agent tooling is available. Keep them scoped; the final synthesis remains your responsibility.

Recommended agents:

- `FieldWorks C# Expert` for managed `*.cs`, `.csproj`, config, resources, or net48 behavior.
- `FieldWorks WinForms Expert` for WinForms UI, designer, layout, event-handler, resource, or localization changes.
- `FieldWorks C++ Expert` for native, C++/CLI-adjacent, COM, Views, FwKernel, ViewsInterfaces, or ABI-sensitive changes.
- `FieldWorks Avalonia UI Expert` only for Avalonia/XAML work; do not use it for existing WinForms UI.
- `devils-advocate` for large architecture, scope, or risk arguments where a skeptical pass would sharpen the interview.

If specialist agents are unavailable or would add friction for a small diff, run the passes directly using the review policy.

For each pass:

- Compare against `MERGE_BASE`.
- Verify findings against actual code before reporting.
- Record findings as Critical, Important, or Minor.
- Record positive observations.
- Record required validation and evidence gaps.

After all passes:

- Merge findings into one list ordered by severity.
- Deduplicate only when two passes flagged the same file for the same concern.
- Keep distinct concerns about the same file as separate findings.
- Keep a factual Contract/API Changes summary.
- Keep Required Validation separate from findings.

## Author Interview

Aim for 5-15 questions total. Ask one Critical or Important finding at a time unless multiple findings share one root cause.

For each Critical and Important finding:

- Ask directly why the change is safe or intentional.
- Ask what validation covers it.
- If the answer is vague, ask one follow-up.
- If it remains unclear after the follow-up, record it as unresolved.

If the changes are large, cross native/managed/build/installer boundaries, involve non-obvious design decisions, or the author's answers reveal uncertainty, ask separately:

> "Can you walk me through the most complex or non-obvious part of these changes? I want to make sure I understand the reasoning."

Watch for lack-of-understanding signals:

- "The AI did it" or similar deferrals.
- "I'm not sure" or "I don't know".
- Vague explanations that do not describe the mechanism.
- Inability to explain a changed section.

Record those explicitly as `Author does not understand: <area>`. Do not soften them into acceptance or satisfaction.

For Minor findings:

- Print the full list first.
- If there are 3 or fewer, ask whether the author wants to respond to all at once or one at a time.
- If there are more than 3, go one at a time.

End the interview by asking:

> "Is there anything else you want to flag or discuss before I write up the summary? Any tradeoffs you made, things you're uncertain about, or context a reviewer should know?"

## In-Review Fixes

If the author asks you to fix a finding, implement the fix unless it is ambiguous. Ask one clarifying question only when needed.

Rules:

- Keep fixes minimal and scoped to the finding.
- Stage fixes with `git add`; do not commit yet.
- Record each fix as `INTERVIEW_CHANGES` with the finding and what changed.
- Do not remove fixed findings from the summary; mark them `[x]` with a fixed-during-review note.

After in-review fixes, run relevant FieldWorks checks:

- Use repository scripts and tasks, not ad-hoc `msbuild`, `dotnet build`, `vstest.console`, or `nmake`.
- Prefer the VS Code task `CI: Whitespace check` for whitespace.
- Run `./build.ps1` when build-affecting, managed, native, resource, or project files changed.
- Run `./test.ps1` with the narrowest reliable `-TestProject` or `-TestFilter` for managed behavior.
- Run `./test.ps1 -Native` with `-TestProject` when native code or tests changed.
- Run `./Build/Agent/Setup-InstallerBuild.ps1 -ValidateOnly` for installer/WiX/helper-script changes.
- Do not mark manual validation complete unless you directly performed it or the author explicitly confirms it.

Report checks that were skipped and why.

## Summary File

Always write a fresh `.review/summary.md`. Do not merge with an existing summary.

Use this structure:

```markdown
# Code Review Summary

**Branch**: <branch>

**Base**: <base branch>

**Date**: <today's date>

**Review model**: <model name>

**Files changed**: <file count>

## Overview

[One or two paragraphs combining the author's purpose with the analysis result.]

## Contract/API Changes

[Factual Contract/API Changes summary. Write "None." if none.]

## Findings

Finding states:
- `- [ ] **Description**` - open
- `- [ ] ~~Description~~ _(author's explanation)_` - dismissed
- `- [x] **Description** _(fixed during review: what changed)_` - fixed

### Critical - Must address before merge

[All Critical findings, or "None."]

### Important - Should address before merge

[All Important findings, or "None."]

### Minor - Consider

[All Minor findings, or "None."]

## Required Validation / Evidence

[Commands run, commands still needed, manual validation gaps, or "None."]

## Positive Observations

[Positive observations, or "None."]

## Interview Notes

[Author explanations, decisions, unresolved items, and explicit lack-of-understanding notes.]

## In-Review Quality Check

[Only include if in-review changes were made.]

## Suggested Review Focus

- [ ] [High-priority review meeting agenda item]
- [ ] [High-priority review meeting agenda item]
```

## PR Offer

After writing the summary, tell the author:

> "Review summary written to `.review/summary.md`.
>
> Please review it, make changes where appropriate, and run `/pr-preflight` again until you are ready to post the PR.
>
> If you do not want to make any changes and are ready for review, would you like me to commit any uncommitted changes, push, and post the PR? I will check whether one already exists for this branch and update it, or create a new one if not, using a Quick Summary-first PR description based on this summary."

Only create or update a PR after the author confirms.

## PR Description

When creating or updating a PR, do not paste `.review/summary.md` verbatim as the first content. Compose a reviewer-first PR description that starts with a short `Quick Summary`, then keeps the full preflight record available in a collapsed section.

Use this structure:

```markdown
## Quick Summary

[One short paragraph or 2-3 bullets covering what changed, why, review focus, and validation status.]

<details>
<summary>Preflight review details</summary>

<!-- pr-preflight:summary:start -->
[summary]
<!-- pr-preflight:summary:end -->

</details>
```

Quick Summary rules:

- Keep it short and reviewer-focused: what changed, why it matters, where reviewers should focus, and what validation was run or skipped.
- For ordinary PRs, use one short paragraph or 2-3 bullets.
- For large, multi-faceted PRs, expand to at most 6 bullets or short items.
- If more than 6 bullets or short items would be needed to capture the meaningful changes, include this exact sentence in the Quick Summary: "This quick summary does not capture all meaningful changes from this PR - please review the full summary carefully."
- Do not duplicate full findings, interview notes, or detailed validation logs in the Quick Summary. Put detailed material in the collapsed preflight review details.
- If validation was skipped, incomplete, or manual-only, say that plainly in the Quick Summary.

When creating or updating a PR, wrap the full preflight summary inside the collapsed details section using these markers:

```markdown
<!-- pr-preflight:summary:start -->
[summary]
<!-- pr-preflight:summary:end -->
```

For branch names like `lt-1234-anything`, prefix the PR title with `LT-1234:`. Use a sentence-case title based on the actual change, not just the branch slug.

After Copilot or human reviewers leave comments, use `.github/prompts/respond-to-review-comments.prompt.md` to work through the review response loop: evaluate comments, fix sensible requests, ask about ambiguity, verify, commit, push, reply, resolve, and summarize.
