# Pull Request Workflow

This guide describes the process for submitting code changes to FieldWorks via GitHub Pull Requests.

## Overview

All code changes to FieldWorks go through a pull request (PR) workflow:

1. Create a feature branch
2. Make your changes
3. Submit a pull request
4. Code review
5. Address feedback
6. Merge

## Branch Naming Conventions

Use descriptive branch names with appropriate prefixes:

| Prefix | Purpose | Example |
|--------|---------|---------|
| `feature/` | New features | `feature/add-export-dialog` |
| `bugfix/` | Bug fixes | `bugfix/LT-12345-fix-crash` |
| `hotfix/` | Emergency fixes for released versions | `hotfix/9.2.1` |
| `docs/` | Documentation changes | `docs/update-contributing-guide` |

Include the issue number when applicable (e.g., `bugfix/LT-12345-description`).

## Creating a Pull Request

### Step 1: Create a Feature Branch

```bash
# Ensure you're on the latest default branch
git checkout release/9.3
git pull origin release/9.3

# Create your feature branch
git checkout -b feature/my-feature-name
```

### Step 2: Make Your Changes

1. Write your code following `.editorconfig` and the repo conventions in `.github/copilot-instructions.md`
2. Write or update tests for your changes
3. Ensure all tests pass locally
4. Commit with clear, descriptive messages

### Step 3: Push and Create the PR

```bash
# Push your branch to GitHub
git push -u origin feature/my-feature-name
```

Then on GitHub:
1. Navigate to the repository
2. Click **"Compare & pull request"** (or go to Pull Requests → New)
3. Fill out the PR template

### Step 4: PR Description

A good PR description includes:

- **Summary**: What does this PR do?
- **Issue Reference**: Link to related issues (e.g., "Fixes #123" or "Relates to LT-12345")
- **Testing**: How was this tested?
- **Screenshots**: For UI changes, include before/after screenshots
- **Breaking Changes**: Note any breaking changes

## Code Review Process

### For Authors

- Respond to all reviewer comments
- Make requested changes in new commits (don't force-push during review)
- Mark conversations as resolved when addressed
- Request re-review after making changes

### For Reviewers

When reviewing, check for:

- **Correctness**: Does the code do what it's supposed to?
- **Tests**: Are there adequate tests? Do they pass?
- **Code Quality**: Does it follow coding standards?
- **Security**: Are there any security concerns?
- **Performance**: Are there any performance implications?
- **Documentation**: Is the code well-documented?

Follow the checklist in the PR template and ensure relevant tests are run.

## Merge Requirements

Before a PR can be merged:

1. ✅ **CI Checks Pass** - All automated tests and builds must succeed
2. ✅ **Code Review Approved** - At least one approving review from a team member
3. ✅ **No Merge Conflicts** - Branch must be up-to-date with the target branch
4. ✅ **Conversations Resolved** - All review comments should be addressed

### Updating Your Branch

If your branch has conflicts or is behind:

```bash
# Fetch latest changes
git fetch origin

# Rebase on the target branch (preferred)
git rebase origin/release/9.3

# Or merge (if rebase is problematic)
git merge origin/release/9.3

# Push updated branch
git push --force-with-lease  # for rebase
git push                      # for merge
```

## Merging the PR

Once all requirements are met:

1. Click **"Squash and merge"** (preferred) or **"Merge pull request"**
2. Edit the commit message if needed
3. Delete the branch after merging (GitHub offers this automatically)

### Merge Strategies

- **Squash and merge**: Combines all commits into one. Use for most PRs.
- **Merge commit**: Preserves individual commits. Use for large features with meaningful commit history.
- **Rebase and merge**: Replays commits on target branch. Use rarely.

## After Merging

1. Delete your feature branch (locally and remotely)
2. Update any related issues
3. Verify the changes in the target branch

```bash
# Delete local branch
git branch -d feature/my-feature-name

# Delete remote branch (if not auto-deleted)
git push origin --delete feature/my-feature-name

# Update your local default branch
git checkout release/9.3
git pull
```

## Special Cases

### Draft Pull Requests

Use draft PRs for:
- Work in progress that you want early feedback on
- Changes that depend on other PRs
- Large changes you want to discuss before completing

Convert to a regular PR when ready for review.

### Stacked PRs

For large features, consider breaking into smaller PRs:
1. Create a base feature branch
2. Create sub-branches for each piece
3. PR sub-branches into the feature branch
4. PR the feature branch into the main branch

### Reverting Changes

If a merged PR causes issues:

```bash
# Create a revert PR
git checkout release/9.3
git pull
git checkout -b revert/feature-name
git revert <merge-commit-sha>
git push -u origin revert/feature-name
```

## See Also

- [CONTRIBUTING.md](../CONTRIBUTING.md) - Getting started with contributions
- [Commit message guidelines](../../.github/commit-guidelines.md) - CI-enforced commit rules
- [Copilot guidance governance](../../.github/AI_GOVERNANCE.md) - Where docs and rules live
- [Release Process](release-process.md) - How releases are managed
