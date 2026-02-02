# OpenSpec Instructions

These instructions are for AI assistants working in the FieldWorks repository.

<!-- OPENSPEC:START -->
Always open `@/openspec/AGENTS.md` when the request:
- Mentions planning or proposals (words like proposal, spec, change, plan)
- Introduces new capabilities, breaking changes, architecture shifts, or big performance/security work
- Sounds ambiguous and you need the authoritative spec before coding

Use `@/openspec/AGENTS.md` to learn:
- How to create and apply change proposals
- Spec format and conventions
- Project structure and guidelines

Keep this managed block so 'openspec update' can refresh the instructions.
<!-- OPENSPEC:END -->

## When to Use Beads vs OpenSpec

| Situation | Tool | Action |
|-----------|------|--------|
| New feature/capability | OpenSpec | Create change proposal first |
| Approved spec ready for implementation | Both | Import tasks to Beads, then implement |
| Bug fix, small task, tech debt | Beads | `bd create` directly |
| Discovered issue during work | Beads | `bd create --discovered-from <parent>` |
| Tracking what's ready to work on | Beads | `bd ready` |
| Feature complete | OpenSpec | Archive the change |

## The Complete Workflow

### Phase 1: Analysis (Before OpenSpec)

Before creating an OpenSpec proposal, run thorough analysis:

```
ultrathink and traverse and analyse the code to thoroughly understand the context
before preparing a detailed plan to implement the requirement.

Before finalizing the plan you can ask me any question to clarify the requirement.
```

This forces in-depth analysis. Fix the analysis until you're satisfied before proceeding.

### Phase 2: OpenSpec Proposal

When analysis is complete, create the proposal:

```bash
openspec new change "my-feature-name"
```

Then follow the artifact workflow (proposal → specs → design → tasks).

### Phase 3: OpenSpec Validation (CRITICAL)

**THOROUGHLY VALIDATE all OpenSpec artifacts.** Read them, understand what was captured, and fix them until correct.

This validation is based on understanding of the FieldWorks platform:
- .NET Framework 4.8, C#, WinForms
- C++/CLI native layer
- Registration-free COM
- MSBuild traversal SDK (native before managed)

Skipping validation is asking for troubles.

### Phase 4: Import Tasks to Beads

When OpenSpec is validated and ready for implementation, import to Beads:

```bash
# Create epic for the change
bd create "<change-name>" -t epic -p 1 -l "openspec:<change-name>" -d "## OpenSpec Change
See: openspec/changes/<change-name>/

## Artifacts
- proposal.md: High-level description
- specs/: Detailed specifications
- design/: Implementation approach
- tasks.md: Execution checklist"

# For each task in tasks.md, create a child issue WITH FULL CONTEXT
bd create "<task description>" -t task -p 2 -l "openspec:<change-name>" -d "## Spec Reference
openspec/changes/<change-name>/tasks.md#task-N

## Requirements
<copy key requirements from spec>

## Acceptance Criteria
<copy from spec or design>

## Context
<relevant file paths, dependencies, technical notes>"
```

**The test:** Could someone implement this issue correctly with ONLY the bd description and access to the codebase? If not, add more context.

### Phase 5: Execute Tasks

Work through tasks using the Beads daily workflow:

1. **Orient**: `bd ready --json` to see unblocked work
2. **Pick work**: Select highest priority ready issue
3. **Update status**: `bd update <id> --status in_progress`
4. **Implement**: Do the work
5. **Discover**: File any new issues found: `bd create "Found: <issue>" -t bug --discovered-from <current-id>`
6. **Complete**: `bd close <id> --reason "Implemented"`
7. **Sync**: Keep OpenSpec `tasks.md` and Beads in sync:
   - When completing a Beads issue, also mark `[x]` in tasks.md

### Phase 6: Archive

When all tasks complete:

```bash
openspec archive "<change-name>"
```

## Importing OpenSpec Tasks to Beads (Detailed)

**ALWAYS include full context.** Issues must be **self-contained** — an agent must understand the task without re-reading OpenSpec files.

**REQUIRED in every issue description:**
1. Spec file reference path
2. Relevant requirements (copy key points)
3. Acceptance criteria from the spec
4. Any technical context needed

### BAD — Never do this:
```bash
bd create "Update FLExBridge integration" -t task
```

### GOOD — Always do this:
```bash
bd create "Add retry logic to FLExBridge sync calls" -t task -p 2 \
  -l "openspec:flexbridge-improvements" \
  -d "## Spec Reference
openspec/changes/flexbridge-improvements/specs/sync/spec.md

## Requirements
- Add exponential backoff retry for network failures
- Maximum 3 retries with 1s, 2s, 4s delays
- Log retry attempts at Warning level

## Acceptance Criteria
- Network transient failures don't cause immediate failure
- User sees progress during retries
- Logs show retry pattern for debugging

## Files to modify
- Src/LexText/LexTextControls/FLExBridgeListener.cs
- Src/LexText/LexTextControls/FLExBridgeSyncService.cs

## Technical Notes
- Uses existing SIL.LCModel.Utils.Retrier pattern
- Must handle both HTTP timeout and connection refused"
```

## Label Conventions

- `openspec:<change-name>` - Links issue to OpenSpec change proposal
- `spec:<spec-name>` - Links to specific spec file
- `discovered` - Issue found during other work
- `tech-debt` - Technical debt items
- `blocked-external` - Blocked by external dependency
- `native` - Requires C++/CLI changes
- `managed` - C#/.NET changes only
- `installer` - WiX/installer changes

### Coordinate via Thread ID

Use the OpenSpec change name as the thread ID for all related messages:

```
# Start work announcement
send_message(
  thread_id="openspec:my-feature",
  subject="[openspec:my-feature] Starting implementation",
  body_md="Beginning work on tasks 1-3. Reserving: Src/LexText/**"
)
```

### File Reservations

Before editing files for an OpenSpec task:

```
file_reservation_paths(
  paths=["Src/LexText/**/*.cs"],
  reason="openspec:my-feature#task-1",
  exclusive=true
)
```

## Landing the Plane (Session Completion)

**When ending a work session**, complete ALL steps. Work is NOT complete until `git push` succeeds.

### Mandatory Workflow

#### 1. File Issues for Remaining Work
```bash
bd create "TODO: <description>" -t task -p 2
bd create "Bug: <description>" -t bug -p 1
```

#### 2. Run Quality Gates (if code changed)
```powershell
.\build.ps1
.\test.ps1
.\Build\Agent\check-and-fix-whitespace.ps1
```

#### 3. Update All Tracking

**Beads issues:**
```bash
bd close <id> --reason "Completed"              # Finished work
bd update <id> --status in_progress             # Partially done
bd update <id> --add-note "Session end: <ctx>"  # Add context
```

**OpenSpec tasks.md:**
- Mark completed tasks: `- [x] Task description`
- Add notes for partial progress

#### 4. Sync and Push (MANDATORY)
```bash
# Sync Beads database
bd sync

# Pull, rebase, push
git pull --rebase
git add -A
git commit -m "chore: session end - <summary>"
git push

# VERIFY - must show "up to date with origin"
git status
```

#### 5. Clean Up
- Clear stashes: `git stash clear` (if appropriate)
- Prune remote branches if needed

#### 6. Verify Final State
```bash
bd list --status open    # Review open issues
bd ready                 # Show what's ready for next session
git status               # Must be clean and pushed
```

#### 7. Hand Off

Provide context for next session:
```
## Next Session Context
- Current epic: <id and name>
- Ready work: `bd ready` shows N issues
- Blocked items: <any blockers>
- Notes: <important context>
```

### Critical Rules

- Work is NOT complete until `git push` succeeds
- NEVER stop before pushing - that leaves work stranded locally
- NEVER say "ready to push when you are" - YOU must push
- If push fails, resolve and retry until it succeeds
- ALWAYS run `bd sync` before committing to capture issue changes

## FieldWorks-Specific Guidelines

### Build/Test Requirements
- Always use `.\build.ps1` (enforces native-first order)
- Always use `.\test.ps1` for testing
- Native C++ (Phase 2) must build before managed assemblies

### COM and Registry
- FieldWorks uses registration-free COM
- Never register COM components globally
- Update manifests via `Build/RegFree.targets`

### Existing Specs Reference
Check existing specs in `specs/` folder for architectural decisions:
- `specs/003-convergence-regfree-com-coverage` - COM registration patterns
- `specs/006-convergence-platform-target` - Platform targeting
- `specs/007-wix-314-installer` - Installer patterns
