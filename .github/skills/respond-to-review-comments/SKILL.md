---
name: respond-to-review-comments
description: "Respond to Copilot and human PR reviewer comments: evaluate feedback, fix sensible requests, ask about ambiguity, verify, commit, push, reply to threads, resolve addressed comments, and summarize results. Use when handling review comments, Copilot review feedback, requested changes, unresolved PR threads, or reviewer follow-up."
argument-hint: "Optional PR URL, reviewer name, comment ID, or focus area"
---

# Respond to Review Comments

Use this skill when the user wants a guided workflow for addressing Copilot or human reviewer comments on a FieldWorks pull request.

This workflow starts from technical evaluation, not automatic agreement. External feedback is input to check against the codebase and repository rules.

## Goals

- Collect unresolved Copilot and human reviewer comments from the active pull request, a supplied PR URL, or comments pasted in chat.
- Decide whether each comment needs a code change, a reply, a clarifying question, or technical pushback.
- Fix sensible, unambiguous comments with minimal scoped edits.
- Ask the user before guessing when feedback is ambiguous, risky, contradictory, or policy-heavy.
- Run appropriate FieldWorks validation through repo scripts or narrow editor diagnostics.
- Commit and push the resulting code when it is ready.
- Reply to review comments, resolve threads when possible, and report a concise summary.

## Required Context

Before working on comments:

1. Load `CONTEXT.md` and apply the shared language rules.
2. Load the repo instructions relevant to touched files.
3. Use the `grill-with-docs` discipline for ambiguous terminology: challenge fuzzy terms, ground terms in code, and update `CONTEXT.md` if the workflow exposes durable language decisions.
4. Apply the code-review reception rule: verify feedback before implementing it.

Useful repo anchors:

- `AGENTS.md`
- `.github/context/codebase.context.md`
- `.github/instructions/navigation.instructions.md`
- `.github/instructions/testing.instructions.md`
- `.github/instructions/fieldworks-ui-review.instructions.md`
- `.github/instructions/fieldworks-interop-review.instructions.md`
- `.github/instructions/fieldworks-build-installer-review.instructions.md`

## Comment Intake

Use the richest available source, in this order:

1. Active pull request tools in the editor or GitHub extension.
2. A PR URL, PR number, reviewer filter, or comment ID supplied by the user.
3. Review comments pasted into chat.

Collect:

- Unresolved inline review threads.
- Copilot review comments.
- Human reviewer comments and change requests.
- General PR comments that request action.

If no comments are available, ask the user for the PR, comment text, or review thread details.

## Classify Each Comment

Build a short working ledger in chat, grouped by file or thread. For each comment, classify it as one of:

- **Fix**: The request is technically sound, unambiguous, scoped, and compatible with FieldWorks conventions.
- **Clarify**: The request is ambiguous, too broad, conflicts with another requirement, or needs a product/architecture decision.
- **Reply only**: The request is already satisfied, incorrect for this codebase, a YAGNI expansion, or better handled by explanation.
- **Defer**: The request is valid but outside the current PR or requires a separate planned change.

Ask the user one focused question before changing code for any **Clarify** item. Do not partially implement unclear multi-item feedback while unresolved ambiguity remains.

## Fix Workflow

For each **Fix** item:

1. Read the comment, the referenced code, and enough nearby context to verify the issue.
2. Use symbol/reference navigation when the change crosses interfaces, inheritance, build graph, installer topology, native/managed boundaries, or parser/interlinear pipelines.
3. Apply the smallest change that satisfies the reviewer without unrelated refactoring.
4. Preserve FieldWorks rules:
   - Use `build.ps1` and `test.ps1` for build/test work.
   - Keep managed code compatible with .NET Framework 4.8, the repo default C# 8.0 language policy, and nullable reference types disabled unless a project explicitly opts in.
   - Keep user-visible strings in `.resx`.
   - Preserve native-before-managed build order.
   - Preserve registration-free COM behavior.
5. Record what changed for the eventual reply.

## Verification

Choose validation based on touched files and risk:

- Documentation, prompt, instruction, or skill-only changes: run editor diagnostics on touched files and consider `CI: Whitespace check` before committing.
- Managed code changes: run the narrowest reliable `./test.ps1 -TestProject` or `./test.ps1 -TestFilter`, and `./build.ps1` when project/resource/build inputs changed.
- Native or interop changes: run `./test.ps1 -Native` or the relevant native test project, plus build validation when needed.
- Installer or PowerShell changes: run the relevant `Build/Agent` validation script and avoid ad-hoc pipelines.

Do not claim manual validation unless it was directly performed or the user explicitly confirms it.

## Git Workflow

Before staging or committing:

1. Run `git status --short`.
2. Identify pre-existing user changes and changes made by this workflow.
3. Stage only the intended files explicitly. Do not use broad staging if unrelated or staged-but-deleted files are present.
4. If unrelated changes are already staged, ask whether to include them in the review-comment commit or leave them staged for a separate commit.

Commit after fixes are validated and no ambiguity remains. Use a concise message that describes the review-response work, for example:

```text
fix: address PR review comments
```

Push the current branch after committing. If push requires credentials or a force push, stop and ask the user; do not force-push without explicit approval.

## Reply and Resolve

For each thread or comment:

- **Fixed**: Reply with the concrete change and verification result.
- **Reply only**: Reply with concise technical reasoning.
- **Clarify**: Ask the specific question needed to proceed.
- **Deferred**: State why it belongs in follow-up work.

Resolve a review thread only when all of these are true:

- The thread is unresolved.
- The tool/API reports it can be resolved.
- The code change or reply fully addresses the comment.
- There is no open question for the user or reviewer.

Do not resolve threads that are ambiguous, disputed, blocked by missing verification, or intentionally deferred.

When using GitHub APIs directly, reply to inline review comments in their thread, not as unrelated top-level PR comments.

## PR Description Updates

If review-response work materially changes the PR's scope, validation state, or main reviewer focus, update the PR description's `## Quick Summary` before finishing.

Quick Summary maintenance rules:

- Keep the first section short and reviewer-focused: what changed, why it matters, where reviewers should focus, and what validation was run or skipped.
- For ordinary PRs, use one short paragraph or 2-3 bullets.
- For large, multi-faceted PRs, expand to at most 6 bullets or short items.
- If more than 6 bullets or short items would be needed to capture the meaningful changes, include this exact sentence in the Quick Summary: "This quick summary does not capture all meaningful changes from this PR - please review the full summary carefully."
- Preserve the collapsed preflight review details and `pr-preflight:summary` markers when they exist.
- Do not claim validation that was not performed or explicitly confirmed by the user.

## Final Report

End with:

- Comments fixed, grouped by reviewer/thread.
- Comments replied to without code changes and why.
- Comments left unresolved and the blocker.
- Commit SHA and pushed branch, if committed and pushed.
- Verification commands run and any gaps.

Keep the report concise, but include enough detail for the user to see which comments still need attention.
