---
name: openspec-beads-import
description: >
  Import tasks from an OpenSpec change into Beads with full context.
  Each Beads issue is self-contained so agents can work on it without
  re-reading OpenSpec files. Per the Reddit workflow pattern.
license: MIT
compatibility: Requires openspec CLI and bd (beads) CLI.
metadata:
  author: FieldWorks team
  version: "1.0"
---

# OpenSpec to Beads Import

Import OpenSpec tasks into Beads with **full context** so each issue is self-contained and survives conversation compaction.

## Prerequisites

- OpenSpec change exists with completed `tasks.md` artifact
- Beads initialized in repo (`bd prime` works)
- Change has been **validated** (don't import unvalidated specs!)

## Input

Specify change name, or omit to auto-select from active changes.

## Steps

### 1. Identify the Change

If change name provided, use it. Otherwise:
```bash
openspec list --json
```

Select the change and announce: "Importing tasks from change: `<name>`"

### 2. Read OpenSpec Artifacts

Read all available artifacts to build context:
```bash
openspec status --change "<name>" --json
```

Read these files:
- `openspec/changes/<name>/proposal.md` - High-level description
- `openspec/changes/<name>/specs/*.md` - Detailed specifications
- `openspec/changes/<name>/design/*.md` - Implementation approach
- `openspec/changes/<name>/tasks.md` - Task list to import

### 3. Create Epic for the Change

```bash
bd create "<change-name>: <short description>" -t epic -p 1 \
  -l "openspec:<change-name>" \
  -d "## OpenSpec Change: <change-name>

**Proposal:** openspec/changes/<change-name>/proposal.md

## Summary
<one paragraph from proposal>

## Key Specs
<list spec files with one-line descriptions>

## Design Approach
<one paragraph from design>

## Tasks
See child issues labeled \`openspec:<change-name>\`

## Files Affected
<list primary files/folders from specs>"
```

Record the epic ID for use as parent reference.

### 4. Import Each Task

For EACH task in `tasks.md`:

**Parse the task** to extract:
- Task description
- Priority (default 2 for features, 1 for blockers)
- Which spec/design sections apply
- File paths mentioned or implied

**Create with FULL context:**
```bash
bd create "<task title>" -t task -p <priority> \
  -l "openspec:<change-name>" \
  -d "## Spec Reference
openspec/changes/<change-name>/tasks.md - Task N of M

## Parent Epic
<epic-id>: <epic-title>

## Requirements
<copy relevant requirements from specs>
<be specific - include file paths, method names, patterns>

## Acceptance Criteria
<from spec or design>
- [ ] <specific testable criteria>
- [ ] <another criteria>

## Technical Context
<from design and domain knowledge>
- Files: <specific paths>
- Dependencies: <what must be done first>
- Testing: <what tests to write/run>

## FieldWorks Notes
<relevant domain context>
- Build phase: <native|managed|both>
- Related AGENTS.md: <paths if applicable>"
```

### 5. Establish Dependencies

If tasks have order dependencies:
```bash
bd dep add <later-task-id> --blocks <earlier-task-id>
```

Common dependency patterns:
- Native (C++) tasks before managed (C#) tasks that depend on them
- Interface definitions before implementations
- Core utilities before consumers

### 6. Summary

Display:
```
## Import Complete: <change-name>

**Epic:** <epic-id>
**Tasks Created:** N issues

### Task Overview
| ID | Title | Priority | Dependencies |
|----|-------|----------|--------------|
| <id> | <title> | <priority> | <deps or "none"> |

### Next Steps
1. Run `bd ready` to see unblocked tasks
2. Pick a task and run `bd show <id>` for full context
3. When implementing: `bd update <id> --status in_progress`
4. Keep tasks.md in sync: mark `[x]` when completing bd issues

### Quick Commands
\`\`\`bash
bd ready                          # What can I work on?
bd show <id>                      # Full task context
bd update <id> --status in_progress  # Start work
bd close <id> --reason "done"     # Complete task
\`\`\`
```

## Guardrails

- **NEVER create issues without full context** - Each issue must be independently workable
- **NEVER skip the acceptance criteria** - Copy from spec, don't summarize
- **Include file paths** - Agents need to know WHERE to look
- **Mark build phase** - Native vs managed matters in FieldWorks
- **Ask for clarification** if a task is too vague to create proper context

## The Self-Contained Test

Before creating each issue, ask: "Could an agent implement this correctly with ONLY the bd description and access to the codebase, even after conversation compaction?"

If NO → add more context.

## Example Output

```
## Import Complete: add-retry-logic

**Epic:** bd-47
**Tasks Created:** 4 issues

| ID | Title | Priority | Dependencies |
|----|-------|----------|--------------|
| bd-48 | Add Retrier utility class | 2 | none |
| bd-49 | Wrap FLExBridge calls with retry | 2 | bd-48 |
| bd-50 | Add retry tests | 2 | bd-49 |
| bd-51 | Update logging for retry events | 3 | bd-48 |

Run `bd ready` to start working!
```
