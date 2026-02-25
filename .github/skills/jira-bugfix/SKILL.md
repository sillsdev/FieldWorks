```skill
---
name: jira-bugfix
description: >
  End-to-end JIRA bugfix workflow: fetch issue, assign, branch,
  TDD, fix, test, update AGENTS.md, commit, PR, and update JIRA.
  Use when the user says "fix LT-XXXXX" or references a JIRA bug
  to resolve.
license: MIT
compatibility: Requires atlassian-skills, atlassian-readonly-skills.
metadata:
  author: FieldWorks team
  version: "1.0"
---

# JIRA Bugfix Workflow

End-to-end skill for fixing bugs sourced from SIL's JIRA
(LT-prefixed tickets). Orchestrates the full lifecycle from
issue triage through PR creation and JIRA update.

## When to Use

Activate this skill when:
- The user says "fix LT-XXXXX" or "work on LT-XXXXX"
- The user references a JIRA bug they want resolved
- You are starting a bugfix session for a known defect

## Announce the Plan

Before starting any work, **always tell the user** the steps
you will follow, in order:

> I will follow the JIRA bugfix workflow:
>
> 1. Fetch the issue details from JIRA
> 2. Ensure the issue is assigned to you and in progress
> 3. Verify or create a branch named after the ticket
> 4. Reproduce the bug with a failing test (TDD)
> 5. Implement the minimal fix to make the test pass
> 6. Assess whether additional test coverage is needed
> 7. Devil's advocate & code review
> 8. Check and update relevant AGENTS.md files
> 9. Commit, push, and create a PR
> 10. Update the JIRA ticket with a comment and PR link
>
> I will pause for your input at key decision points.

Then proceed through the steps below.

## Step 0: Fetch Issue from JIRA

Use the `atlassian-readonly-skills` scripts to get the issue:

```powershell
python -c "import sys; sys.path.insert(0, '.github/skills/atlassian-readonly-skills/scripts'); from jira_issues import jira_get_issue; print(jira_get_issue('LT-XXXXX'))"
```

Extract and present to the user:
- **Summary** and **description**
- **Status**, **priority**, **assignee**
- **Components** and **affected versions**
- **Comments** (recent ones may contain reproduction steps)

If the issue is not a Bug type, note this and ask the user
whether to proceed with the bugfix workflow anyway.

## Step 1: Assign and Transition to In Progress

### Check current assignment

If the issue is unassigned or assigned to someone else, ask
the user for their JIRA username and assign it:

```powershell
# Assign the issue (Data Center uses 'name' field)
python -c "
import sys, json
sys.path.insert(0, '.github/skills/atlassian-skills/scripts')
from jira_issues import jira_update_issue
print(jira_update_issue('LT-XXXXX', assignee='username'))
"
```

> **Note**: SIL JIRA is Data Center, so the assignee field
> uses `name` (username string), not `accountId`. If the
> `jira_update_issue` call fails with assignee, fall back to
> setting it via `custom_fields`:
> `custom_fields={'assignee': {'name': 'username'}}`

### Transition to In Progress

```powershell
# Get available transitions
python -c "
import sys
sys.path.insert(0, '.github/skills/atlassian-readonly-skills/scripts')
from jira_workflow import jira_get_transitions
print(jira_get_transitions('LT-XXXXX'))
"

# Transition (use the ID for "In Progress" from above)
python -c "
import sys
sys.path.insert(0, '.github/skills/atlassian-skills/scripts')
from jira_workflow import jira_transition_issue
print(jira_transition_issue('LT-XXXXX', 'TRANSITION_ID'))
"
```

If the issue is already In Progress, skip this step.

## Step 2: Branch Management

### Check current branch

```powershell
git branch --show-current
```

**Decision tree:**

1. If the current branch name contains the LT number
   (e.g., `LT-22427`), you are already on the right branch.
   Proceed.

2. If not, **ask the user**:

   > The current branch is `<name>`. This doesn't match
   > LT-XXXXX. Options:
   > - Create a new branch `LT-XXXXX` from `main`
   > - Continue on the current branch
   > - Switch to an existing branch (specify name)

   If creating a new branch:
   ```powershell
   git fetch origin
   git checkout -b LT-XXXXX origin/main
   ```

> **Important**: Do NOT create worktrees automatically. This
> repo uses worktrees but creating them involves workspace
> setup scripts. If a worktree is needed, tell the user to
> run the "Worktree: Create/Open from branch" VS Code task.

## Step 3: Reproduce the Bug (TDD)

**This is the most important step.** Default to test-driven
development: write a failing test that captures the bug
before writing any fix.

### Process

1. **Analyze the bug**: From the JIRA description and code
   exploration, understand the root cause.

2. **Find the right test file**: Locate existing tests for
   the affected component. Follow the conventions in
   `.github/instructions/testing.instructions.md`.

3. **Write a failing test** that demonstrates the bug:
   - Name it descriptively:
     `MethodName_Scenario_ExpectedBehavior`
   - The test should FAIL with the current code
   - The test should PASS after the fix

4. **Run the test** to confirm it fails:
   ```powershell
   .\test.ps1 -TestFilter "Name~TestMethodName"
   ```

5. **If a test is impossible**, explain to the user WHY:
   - The bug is purely visual/UI and untestable in NUnit
   - The bug requires external services not available in
     test harness
   - The bug is in build/packaging infrastructure

   Then ask the user:
   > I cannot write an automated test for this bug because
   > [reason]. Would you like me to proceed with the fix
   > anyway, or would you prefer to explore alternative
   > verification approaches?

   Wait for confirmation before proceeding.

## Step 4: Implement the Fix

1. Apply the **minimal change** needed to make the failing
   test pass.
2. Follow repo conventions:
   - `.github/instructions/managed.instructions.md` for C#
   - `.github/instructions/native.instructions.md` for C++
   - `.github/instructions/testing.instructions.md` for tests
3. Run the previously-failing test to confirm it passes:
   ```powershell
   .\test.ps1 -TestFilter "Name~TestMethodName"
   ```

## Step 5: Assess Additional Test Coverage

After the fix passes, evaluate whether additional tests are
needed. Ask yourself:

- **Are there related edge cases** the fix might affect?
- **Are there other code paths** that use the same logic?
- **Is the existing test coverage** for this component
  adequate?
- **Could a devil's advocate** argue the fix is incomplete?

If gaps exist, add tests. Common high-value additions:
- Backward compatibility tests (old behavior still works)
- Isolation tests (fix doesn't leak to unrelated paths)
- Boundary/edge-case tests
- Tests for other call sites of modified code

Run the full component tests:
```powershell
.\test.ps1 -TestProject "path/to/TestProject"
```

## Step 6: Devil's Advocate & Code Review

Before finalizing, critically review your own work. Play
devil's advocate against the fix and the tests.

### Challenge the fix

Ask yourself and present findings to the user:

- **Is this the best solution?** Are there simpler or more
  robust alternatives? If multiple reasonable approaches
  exist and the best choice is unclear, **present the
  options to the user** with trade-offs and ask which they
  prefer.
- **Does the fix introduce new risks?** Could it regress
  other behavior, cause performance issues, or break
  backward compatibility?
- **Is the scope right?** Is the fix too narrow (misses
  related cases) or too broad (changes more than needed)?
- **Are there subtle edge cases** not covered by the tests?
- **Does the fix match the codebase style** and patterns?
  Check naming, error handling, null safety, threading.

### Clean code review

Review the diff as if you were a code reviewer:

- No dead code, commented-out code, or debug artifacts
- No unintended whitespace or formatting changes
- Method/variable names are clear and consistent
- Comments explain *why*, not *what*
- No accidental scope expansion beyond the bug fix
- Error paths are handled correctly

### Decision point

If the review surfaces **any uncertainties**, present them
to the user:

> **Devil's advocate findings:**
>
> 1. [Finding and why it matters]
> 2. [Alternative approach and trade-offs]
>
> Would you like me to adjust the approach, or proceed
> as-is?

Wait for the user's response before continuing. If no
uncertainties exist, briefly summarize why the fix is
solid and proceed.

## Step 7: Update AGENTS.md

Check whether the fix changes any contracts, behaviors, or
architecture documented in AGENTS.md files:

1. Find relevant AGENTS.md files:
   ```powershell
   # Check for AGENTS.md in affected directories
   Get-ChildItem -Path "Src/<affected-area>" -Filter "AGENTS.md" -Recurse
   ```

2. If behavior or contracts changed, update the AGENTS.md
   to reflect the new state.

3. If no AGENTS.md exists for the affected area and the
   change is architecturally significant, note this but
   do not create one unless the user requests it.

## Step 8: Commit and Push

### Pre-commit checks

```powershell
.\build.ps1
.\test.ps1
.\Build\Agent\check-and-fix-whitespace.ps1
```

### Commit

Follow `.github/instructions/commit-messages.instructions.md`:
- Subject: max 72 characters, imperative mood, no trailing
  punctuation
- Body: wrap at 80 characters, explain what and why

Pattern:
```
Fix LT-XXXXX: <concise description of fix>

<What changed and why, wrapped at 80 chars.>
<Reference the root cause.>

<What tests were added/changed.>
```

### Push

```powershell
git add -A
git commit -m "<message>"
git push -u origin LT-XXXXX
```

If the branch already exists on the remote, just `git push`.

## Step 9: Create a Pull Request

Use the GitHub MCP tools (or the `mcp_github_create_pull_request`
tool) to create a PR:

- **Title**: `Fix LT-XXXXX: <summary>`
- **Base**: `main` (or the appropriate target branch)
- **Body** should include:
  - Problem description (from JIRA)
  - Root cause analysis
  - Fix description with rationale
  - Files changed table
  - All tests added with descriptions
  - Test results summary
  - Design rationale for non-obvious choices

Present the PR URL to the user.

## Step 10: Update JIRA Ticket

Add a comment to the JIRA ticket with the fix summary and
PR link:

```powershell
python -c "
import sys
sys.path.insert(0, '.github/skills/atlassian-skills/scripts')
from jira_issues import jira_add_comment
comment = '''Fix implemented and PR created.

*Root cause*: <brief root cause>

*Fix*: <brief fix description>

*PR*: https://github.com/sillsdev/FieldWorks/pull/NNN

*Tests added*:
- <test 1 name>: <what it verifies>
- <test 2 name>: <what it verifies>
'''
print(jira_add_comment('LT-XXXXX', comment))
"
```

Do NOT transition the ticket to "Done" or "Resolved" — that
happens after the PR is merged and verified.

## IDE-Specific Notes

### VS Code
- Use the terminal for all Python/git/build commands
- Use the GitHub MCP tools for PR creation
- Use the `mcp_github_*` tools for PR review operations

### Visual Studio
- Use the Package Manager Console or Developer PowerShell
  for Python/git commands
- The JIRA Python scripts work the same way
- For PR creation, use the GitHub CLI (`gh pr create`) or
  the VS GitHub extension

## Decision Points (Pause for User)

This workflow pauses for user input at these points:
1. **Branch**: If current branch doesn't match the ticket
2. **Untestable bug**: If a failing test cannot be written
3. **Devil's advocate**: If the review surfaces
   uncertainties or alternative approaches
4. **Scope expansion**: If the fix reveals larger issues

All other steps proceed automatically.

## Error Handling

- **JIRA unreachable**: Ask user for issue details manually,
  continue with the workflow, and update JIRA at the end
- **Tests fail after fix**: Present failures, ask user
  whether to investigate or revert
- **Build fails**: Present errors, attempt to fix, or ask
  user for guidance
- **Push rejected**: Pull with rebase, resolve conflicts,
  retry

## Integration with Other Skills

This skill composes with:
- `atlassian-readonly-skills` — reading JIRA issues
- `atlassian-skills` — writing to JIRA (assign, comment)
- `session-workflow` — session management and handoff
- `execute-implement` — implementation conventions
- `verify-test` — test verification
- `review` — self-review before PR

## Quick Reference

```
Skill chain:
  jira-bugfix
    ├── atlassian-readonly-skills  (Step 0: read)
    ├── atlassian-skills           (Steps 1, 10: write)
    ├── execute-implement          (Step 4: fix)
    ├── verify-test                (Steps 3, 5: test)
    ├── review                     (Step 6: devil's advocate)
    ├── commit-messages.instructions (Step 8: commit)
    └── mcp_github / gh CLI        (Step 9: PR)
```
```
