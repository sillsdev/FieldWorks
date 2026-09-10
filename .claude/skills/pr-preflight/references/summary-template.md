# `.review/summary.md` template

Always write it fresh. Never merge into an existing summary.

```markdown
# Code Review Summary

**Branch**: <branch>
**Base**: <base branch>
**Date**: <today's date>
**Review model**: <model name>
**Files changed**: <count>

## Overview

[One or two paragraphs combining the author's stated purpose with what the
analysis found.]

## Contract/API Changes

[Factual. "None." if none.]

## Findings

### Critical - Must address before merge
### Important - Should address before merge
### Minor - Consider

## Required Validation / Evidence

[Commands run, commands still needed, manual gaps, or "None."]

## Positive Observations

## Interview Notes

[Author explanations, decisions, unresolved items, and explicit
lack-of-understanding notes.]

## In-Review Quality Check

[Only if in-review changes were made.]

## Suggested Review Focus

- [ ] [agenda item]
```

Finding states:

| Markup | Means |
| --- | --- |
| `- [ ] **Description**` | Open |
| `- [ ] ~~Description~~ _(author's explanation)_` | Dismissed, with the reason |
| `- [x] **Description** _(fixed during review: what changed)_` | Fixed |

A dismissed finding keeps its strikethrough and its reason. Deleting it hides
that the question was ever asked.

## Carrying the summary onto the PR

The preflight record belongs on the PR as a **collapsed section below the
pitch**, never as the opening content:

```markdown
<details>
<summary>Preflight review details</summary>

<!-- pr-preflight:summary:start -->
[summary]
<!-- pr-preflight:summary:end -->

</details>
```

Keep the markers so a re-run replaces the section instead of appending a
second copy. Do not duplicate findings, interview notes or validation logs
into the pitch -- the pitch states validation status in a sentence and leaves
the detail here.
