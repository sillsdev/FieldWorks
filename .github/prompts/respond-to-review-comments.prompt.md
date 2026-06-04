---
description: "Respond to Copilot and human PR reviewer comments; fix sensible requests, ask about ambiguity, verify, commit, push, reply, resolve, and summarize."
agent: "agent"
argument-hint: "Optional PR URL, reviewer name, comment ID, or focus area"
---

Use the `respond-to-review-comments` skill from `.github/skills/respond-to-review-comments/SKILL.md`.

Treat any text supplied with this prompt as optional input: a PR URL, PR number, reviewer name, comment ID, pasted review comments, or a focus area.

Before changing files, load `CONTEXT.md` and use the `grill-with-docs` workflow to challenge ambiguous language in the review feedback. Load relevant staged docs and repo instructions before making decisions.

Work through review comments as follows:

1. Collect unresolved Copilot and human reviewer comments from the active PR, supplied PR details, or pasted comments.
2. Classify each comment as `Fix`, `Clarify`, `Reply only`, or `Defer`.
3. Fix sensible, unambiguous requests with minimal scoped changes.
4. Ask the user one focused question for ambiguous or risky requests.
5. Verify with the narrowest appropriate FieldWorks checks.
6. Commit and push only the intended changes, preserving unrelated user work.
7. Reply to each comment and resolve addressed threads where possible.
8. Report a summary of fixes, replies, unresolved comments, commit/push status, and verification.
