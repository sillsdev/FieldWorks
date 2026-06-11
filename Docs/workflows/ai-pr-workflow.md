# AI-Assisted PR Workflow

This is the canonical core-developer workflow for AI-assisted FieldWorks work.
It starts from a Jira ticket, uses a dedicated branch and git worktree, and
ends after PR preflight, reviewer feedback, and Jira follow-up.

Use this workflow with either:

- **GitHub Copilot** in VS Code
- **Claude Code** running inside the same FieldWorks worktree

## Guiding Rules

- Keep one ticket, one branch, one worktree, and one PR together.
- Include the Jira key in the branch name, commit messages, and PR title.
- Prefer repo tasks and scripts over ad hoc commands.
- Use agent-safe Jira access. Do not point agents at Jira URLs directly.
- Keep PRs small enough that a reviewer can understand them in one sitting.

## Tool Map

| Goal | GitHub Copilot | Claude Code | Repo anchor |
|------|----------------|-------------|-------------|
| Read Jira issue | Atlassian VS Code extension or ask Copilot to use the Atlassian helpers | Use the Atlassian helper scripts or ask Claude to follow them | [../.github/copilot-jira-setup.md](../../.github/copilot-jira-setup.md), [../.claude/skills/atlassian-readonly-skills/SKILL.md](../../.claude/skills/atlassian-readonly-skills/SKILL.md) |
| Create isolated worktree | VS Code task: `Worktree: Create/Open from branch` | Use the same VS Code task, then start Claude inside that worktree | [../.vscode/tasks.json](../../.vscode/tasks.json), [../scripts/Worktree-CreateFromBranch.ps1](../../scripts/Worktree-CreateFromBranch.ps1) |
| Preflight branch before PR | `/pr-preflight` | Ask Claude to follow [../.claude/skills/pr-preflight/SKILL.md](../../.claude/skills/pr-preflight/SKILL.md) for the current branch | [../.github/prompts/pr-preflight.prompt.md](../../.github/prompts/pr-preflight.prompt.md) |
| Respond to review comments | `/respond-to-review-comments` | Ask Claude to follow [../.claude/skills/respond-to-review-comments/SKILL.md](../../.claude/skills/respond-to-review-comments/SKILL.md) | [../.github/prompts/respond-to-review-comments.prompt.md](../../.github/prompts/respond-to-review-comments.prompt.md) |
| Build and test | VS Code `Build`, `Test`, and `CI: Full local check` tasks | Same tasks or `./build.ps1` and `./test.ps1` from the worktree root | [../ReadMe.md](../../ReadMe.md), [../AGENTS.md](../../AGENTS.md) |

## Phase 1: Pull The Jira Issue

1. Start from a real Jira ticket, usually an `LT-` issue.
2. Read the summary, description, recent comments, and acceptance signals before you ask an AI tool to edit code.
3. If you want agent access to Jira, use the approved service-user path described in [../.github/copilot-jira-setup.md](../../.github/copilot-jira-setup.md).
4. Keep the Jira key uppercase. Jira development linking depends on the exact key format.

Recommended human-first path:

- Use the **Atlassian VS Code** extension to browse assigned issues and copy the ticket key.

Recommended scriptable path:

```powershell
# Read one Jira issue through the repo's read-only helper
python -c "import sys; sys.path.insert(0, '.claude/skills/atlassian-readonly-skills/scripts'); from jira_issues import jira_get_issue; print(jira_get_issue('LT-22382'))"
```

Best practice from Atlassian's linked-development docs:

- Put the Jira key in the branch name.
- Put the Jira key in commit messages.
- Put the Jira key in the PR title.

For example:

- Branch: `bugfix/LT-22382-fix-crash`
- Commit: `LT-22382 guard deleted item selection`
- PR title: `LT-22382: guard deleted item selection`

## Phase 2: Create A Dedicated Worktree

FieldWorks supports concurrent work across git worktrees. Use them instead of stacking unrelated work on one checkout.

Preferred path:

1. Open the Command Palette and run **Tasks: Run Task**.
2. Run **`Worktree: Create/Open from branch`**.
3. Enter the branch name you chose for the Jira issue.
4. Let the new VS Code window open for that worktree.

Useful worktree tasks:

- `Worktree: Create/Open from branch`
- `Worktree: Create/Open from branch (picker in terminal)`
- `Worktree: Create/Open from branch (dry run)`
- `Worktree: Rename to branch`
- `Worktree: Open (picker)`

Important repo-specific guidance:

- Prefer the repo task over raw `git worktree add` when you want the normal FieldWorks setup.
- `Setup: Colorize Worktree` runs on folder open and gives each worktree a distinct VS Code window color.
- Open one VS Code window per worktree.
- If Serena or MCP feels stale after switching worktrees, run **MCP: Reset Cached Tools**.

Claude Code users should still create the worktree with the repo task first, then start Claude inside that worktree. That keeps the workspace layout, colorization, and MCP setup consistent with the rest of the team.

## Phase 3: Implement Inside Repo Guardrails

Before editing:

1. Read [../AGENTS.md](../../AGENTS.md) and [../CONTEXT.md](../../CONTEXT.md).
2. Load the instructions that match the touched area.
3. If the ticket language is fuzzy, use the `grill-with-docs` discipline before implementation.

During implementation:

- Keep changes scoped to the Jira ticket.
- Prefer a plan-first pass for larger or riskier changes.
- Use repo build and test entrypoints, not ad hoc `msbuild` or `dotnet build` commands.
- Keep validation notes as you go so the PR description is easy to write later.

Standard validation surfaces:

```powershell
./build.ps1
./test.ps1
./test.ps1 -TestProject "Src/Common/FwUtils/FwUtilsTests"
```

Useful VS Code tasks:

- `Build`
- `Build Release`
- `Test`
- `Test (with filter)`
- `Test (specific project)`
- `CI: Full local check`

Best practices from Claude Code and GitHub review guidance also apply here:

- Plan before editing when the change is not obvious.
- Delegate broad investigation to subagents or focused review passes instead of mixing everything into one long conversation.
- Verify each risky change with the narrowest reliable test before widening scope.

## Phase 4: Run Branch Preflight Before Posting Or Updating The PR

Do a branch-level review before you ask humans to review the PR.

### GitHub Copilot

Run:

```text
/pr-preflight <one-sentence branch purpose>
```

### Claude Code

Ask Claude to follow the repo skill directly. A good prompt is:

```text
Follow .claude/skills/pr-preflight/SKILL.md for the current branch.
Use CONTEXT.md, .github/context/codebase.context.md, and .github/instructions/review-analyzer.instructions.md.
Write .review/summary.md.
```

`pr-preflight` is the multi-phase branch review. It is expected to:

- review the branch diff from `main`
- apply the FieldWorks review policy
- challenge unclear reasoning and validation gaps
- write `.review/summary.md`

Review the generated summary before you open or update the PR. Fix obvious issues first; do not outsource that judgment completely to the agent.

## Phase 5: Create Or Update The PR

When the branch is ready:

1. Push the branch.
2. Create a PR against the correct base branch.
3. Use a title that starts with the Jira key.
4. Include testing and risk notes in the description.
5. Use a draft PR if you want feedback before the change is ready to merge.

Recommended PR description content:

- What changed
- Why the Jira issue required it
- Validation you ran
- Remaining risks or manual checks

If the work also tracks a GitHub issue, link it in the PR description with the normal closing keywords.

GitHub best practices worth keeping:

- request specific reviewers
- re-request review after significant updates
- keep follow-up commits on the same branch so the discussion stays attached to the PR

## Phase 6: Work The Review Loop

Once Copilot or humans leave feedback, use the review-response workflow instead of replying ad hoc.

### GitHub Copilot

Run:

```text
/respond-to-review-comments <PR URL, reviewer name, or focus area>
```

### Claude Code

Ask Claude to follow the repo skill directly. A good prompt is:

```text
Follow .claude/skills/respond-to-review-comments/SKILL.md for this PR.
Classify each comment as Fix, Clarify, Reply only, or Defer.
Verify each change with the narrowest FieldWorks check.
```

Expected behavior during review response:

- verify comments against the code before changing anything
- make minimal scoped fixes
- ask one focused question when feedback is ambiguous
- reply in-thread with what changed or why no change is needed
- resolve a thread only after code and validation fully address it

## Phase 7: Close The Jira Loop

Before merge or immediately after it, make sure Jira reflects the actual state of the work.

- Because the Jira key is in the branch, commits, and PR title, Jira's development panel can show linked branches, commits, and PRs.
- If you need to add a Jira comment or transition the issue, use the write-enabled Atlassian helpers rather than improvising API calls.

Example:

```powershell
python -c "import sys; sys.path.insert(0, '.claude/skills/atlassian-skills/scripts'); from jira_issues import jira_add_comment; print(jira_add_comment('LT-22382', 'PR ready for review: https://github.com/sillsdev/FieldWorks/pull/905'))"
```

## Short Checklist

- [ ] Jira ticket understood and key copied exactly
- [ ] Branch name includes the Jira key
- [ ] Dedicated worktree created with the repo task
- [ ] Changes built and tested with repo scripts or tasks
- [ ] `pr-preflight` run and `.review/summary.md` reviewed
- [ ] PR title starts with the Jira key
- [ ] Review comments handled through the review-response workflow
- [ ] Jira updated with the final branch or PR state when needed

## See Also

- [Core Developer Setup](../core-developer-setup.md)
- [Pull Request Workflow](pull-request-workflow.md)
- [MCP Helpers](../mcp.md)
- [Agent playbook](../../AGENTS.md)