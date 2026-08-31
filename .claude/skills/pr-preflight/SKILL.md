---
name: pr-preflight
description: "The required entrypoint whenever asked to write, make, open, create, update, or ship a PR for this repo -- do not post a PR body without running this first. Also use for pre-PR review, branch readiness, author interview, review summary generation, or validation evidence."
argument-hint: "Optional branch purpose or PR goal"
user-invocable: true
---

# PR Preflight

Interactive branch review before a PR is posted or updated. This is the
orchestration layer: review policy lives in
`.github/instructions/review-analyzer.instructions.md`, shared terminology in
`CONTEXT.md`, and the write-up belongs to `pr-pitch`.

Tell the author what will happen before starting: setup, analysis, interview,
then `.review/summary.md` and optionally the PR. During the interview they can
explain, dismiss a finding with a reason, ask for a fix, or say they are
unsure -- all of which get recorded.

## Setup

Name the review model for the summary header -- `GitHub Copilot` when running
in Copilot. **Never invent an AI co-author trailer.**

1. `git branch --show-current`. **Stop if it is `main`.**
2. `git status --porcelain`. If dirty, ask whether to commit first; in-review
   fixes will otherwise be committed alongside.
3. `git fetch origin --quiet`, then merge-base against `origin/main`. Record
   the file count and commit count.
4. If `.review` is not gitignored, ask to add it -- as its own question.
5. Ask the branch purpose in the author's own words, unless supplied.

Load `CONTEXT.md` and `.github/context/codebase.context.md`. If the purpose or
title uses an overloaded FieldWorks term -- `project`, `model`, `view`, `app`,
`context`, `review`, `validation` -- apply `grill-with-docs` before writing
the summary, and carry the clarified term into the findings and PR copy.

## Analysis

Run all four passes from `review-analyzer.instructions.md`:

1. Contracts, compatibility, correctness
2. Managed UI, C#, localization
3. Native, COM, boundary safety
4. Build, tests, CI, dependencies, installer

Use specialist read-only agents where the changed files justify one --
`FieldWorks C# Expert`, `WinForms Expert`, `C++ Expert`, `Avalonia UI Expert`
(Avalonia work only), `devils-advocate` for large scope or risk arguments. Run
the passes directly for a small diff. The synthesis is yours either way.

Per pass: compare against the merge base, **verify each finding against the
actual code before reporting it**, and grade Critical / Important / Minor.
Record positive observations and validation gaps too. Merge into one severity
-ordered list; deduplicate only when two passes flagged the same file for the
same concern.

## Interview

5-15 questions. One Critical or Important finding at a time, unless several
share a root cause.

Per finding: why is this safe or intentional, and what validation covers it?
One follow-up if the answer is vague; if still unclear, record it unresolved.

For large, cross-boundary or non-obvious changes, ask separately:

> "Can you walk me through the most complex or non-obvious part of these
> changes? I want to make sure I understand the reasoning."

**Record lack of understanding literally.** "The AI did it", "I'm not sure",
or an explanation that never describes the mechanism becomes
`Author does not understand: <area>`. Never soften it into acceptance.

Minor findings: print them all first. Three or fewer, ask whether to take them
together; more than three, go one at a time.

Close with: "Anything else to flag -- trade-offs, uncertainties, context a
reviewer should know?"

## In-review fixes

Keep them minimal and scoped to the finding. `git add`, do not commit yet.
Record each as `INTERVIEW_CHANGES`. **Do not delete a fixed finding** -- mark
it `[x]` with a fixed-during-review note.

Then run the repo scripts, never ad-hoc `msbuild` / `dotnet build` /
`vstest.console` / `nmake`:

| Changed | Run |
| --- | --- |
| Anything build-affecting | `./build.ps1 -CommentHygiene` |
| Managed behaviour | `./test.ps1` with the narrowest reliable `-TestProject` or `-TestFilter` |
| Native code or tests | `./test.ps1 -Native -TestProject <p>` |
| Installer, WiX, helper scripts | `./Build/Agent/Setup-InstallerBuild.ps1 -ValidateOnly` |
| Whitespace | VS Code task `CI: Whitespace check` |

A `-TestFilter` that matches nothing still exits 0 and prints PASS -- check
`Total tests: N` is above zero. **Never mark manual validation complete unless
you performed it or the author explicitly confirms it.** Report skipped checks
and why.

## Summary and PR

Write `.review/summary.md` per `references/summary-template.md`.

Then offer -- and only act on confirmation:

> "Summary written to `.review/summary.md`. Review it, make changes where
> appropriate, and re-run until you are ready. When you are, shall I commit,
> push and post the PR? I will update an existing one if there is one. The
> write-up runs through `pr-pitch`, which also triages the branch's research
> and working markdown into collapsed sections in the PR body and out of the
> tree -- you approve that triage before anything is deleted."

**This skill never composes the description itself.** Hand `pr-pitch` the
branch purpose, the findings and the summary. For a branch named
`lt-1234-anything`, prefix the PR title `LT-1234:` and write a sentence-case
title from the actual change, not the branch slug.

After reviewers comment, use `respond-to-review-comments`.
