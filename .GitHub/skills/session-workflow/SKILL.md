---
name: session-workflow
description: >
  Session management workflows: start, checkpoint, and land sessions.
  Ensures work is properly tracked, synced, and handed off between sessions.
license: MIT
compatibility: Requires bd (beads) CLI.
metadata:
  author: FieldWorks team
  version: "1.0"
---

# Session Workflow Management

Structured workflows for starting, checkpointing, and completing coding sessions. Ensures context survives compaction and work is never stranded.

## Session Start

### 1. Orient

```bash
bd prime              # Load workflow context
bd ready --json       # Find unblocked work
```

### 2. Understand Current State

If continuing existing work:
```bash
bd list --status in_progress   # What was I working on?
bd show <id>                   # Get full context
```

If starting fresh:
```bash
bd ready                       # Pick highest priority unblocked issue
openspec list                  # Check for active OpenSpec changes
```

### 3. Announce Intent

```bash
bd update <id> --status in_progress
```

## Session Checkpoint

Run periodically, especially before:
- Asking user for input
- Context getting large (>70% tokens)
- Switching to a different task
- Taking a break

### Quick Checkpoint

```bash
# Capture current state
bd update <id> --add-note "CHECKPOINT: <what was done>, <what's next>"

# Sync to git
bd sync
```

### Full Checkpoint

```bash
# Update issue notes with full context
bd update <id> --add-note "CHECKPOINT $(date):
COMPLETED: <list of completed items>
IN PROGRESS: <current item and state>
DECISIONS: <any key decisions made>
BLOCKERS: <any blockers discovered>
NEXT: <what to do next>"

# If code changed, verify build
.\build.ps1

# Sync tracking
bd sync

# Commit work-in-progress (optional but recommended)
git add -A
git commit -m "wip: checkpoint - <brief description>"
git push
```

## Landing the Plane (Session End)

**MANDATORY workflow.** Work is NOT complete until `git push` succeeds.

### Step 1: File Issues for Discovered Work

```bash
# Bugs found
bd create "Bug: <description>" -t bug -p 1 -d "<full context>"

# TODO items
bd create "TODO: <description>" -t task -p 2 -d "<full context>"

# Technical debt
bd create "Tech debt: <description>" -t tech-debt -p 3 -d "<full context>"
```

### Step 2: Run Quality Gates

```powershell
# Build
.\build.ps1

# Test (if applicable)
.\test.ps1

# Whitespace check
.\Build\Agent\check-and-fix-whitespace.ps1

# Commit message validation
.\Build\Agent\commit-messages.ps1
```

File P0 issues if builds/tests are broken.

### Step 3: Update All Tracking

**Beads:**
```bash
# Completed work
bd close <id> --reason "Completed: <summary>"

# Partial progress
bd update <id> --status in_progress --add-note "SESSION END: <what remains>"

# Add context for future sessions
bd update <id> --add-note "HANDOFF: <key context for next session>"
```

**OpenSpec tasks.md** (if applicable):
- Mark completed tasks: `- [x] Task description`
- Add notes for partial progress

**Agent Mail** (if multi-agent):
```
send_message(
  thread_id="bd-<id>",
  subject="[bd-<id>] Session end",
  body_md="## Session Summary\n<what was done>\n\n## Remaining\n<what's left>"
)
release_file_reservations()
```

### Step 4: Sync and Push (MANDATORY)

```bash
# Sync Beads to git
bd sync

# Stage, commit, push
git add -A
git commit -m "chore: session end - <summary>"
git pull --rebase
git push

# VERIFY - must be clean and pushed
git status
```

### Step 5: Verify Final State

```bash
bd list --status open    # Review open issues
bd ready                 # Show what's ready for next session
git status               # MUST be clean and pushed
```

### Step 6: Handoff

Provide context for next session:

```markdown
## Session Handoff

### Completed This Session
- <list of completed items>

### Current State
- **Active Epic**: <id and title>
- **In Progress**: <id and title> (or "none")
- **Ready Work**: `bd ready` shows N issues

### Key Decisions
- <decision 1 and rationale>
- <decision 2 and rationale>

### Blockers
- <blocker 1> (waiting on: <what>)

### Next Session Priorities
1. <first priority>
2. <second priority>

### Important Context
- <anything the next session needs to know>
```

## Critical Rules

| Rule | Why |
|------|-----|
| Work is NOT complete until `git push` succeeds | Local-only work is stranded work |
| NEVER stop before pushing | Leaves work invisible to others |
| NEVER say "ready to push when you are" | YOU must push, not the user |
| ALWAYS run `bd sync` before committing | Captures issue changes in git |
| If push fails, resolve and retry | Don't give up on pushes |

## Emergency Procedures

### Context About to Compact

```bash
# Immediate dump of current state
bd update <id> --add-note "EMERGENCY CHECKPOINT:
$(Get-Date -Format 'yyyy-MM-dd HH:mm')
Working on: <current task>
Progress: <what's done>
Next steps: <what was about to happen>
Key files: <list of modified files>
Test status: <pass/fail/not run>"

bd sync
git add -A
git commit -m "wip: emergency checkpoint before compaction"
git push
```

### Lost Context After Compaction

```bash
# Recover from beads
bd list --status in_progress
bd show <id>                    # Read the notes!

# Check git for recent changes
git log --oneline -10
git diff HEAD~3

# Check Agent Mail inbox
fetch_inbox(urgent_only=true)
```

## Integration Points

### With OpenSpec

- Use `openspec:change-name` as thread_id and label
- Keep tasks.md in sync with bd issues
- Archive change when all issues closed

### With TodoWrite

- TodoWrite for immediate tactical work (this hour)
- Beads for persistent tracking (this week/month)
- Update Beads notes with TodoWrite outcomes
