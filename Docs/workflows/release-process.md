# Release Workflow

This document describes the release workflow for FieldWorks. It covers creating release branches, fixing bugs in release branches, and publishing releases.

**Release Manager**: Jason Naylor

## Release Branch Workflow

### Creating a Release Branch

When it's time to prepare a new release, create a new release branch named after the upcoming version.

```bash
# Create and checkout release branch from the develop branch
git checkout develop
git pull origin develop
git checkout -b release/9.3

# Push to remote
git push -u origin release/9.3
```

> **Note**: Creating release branches typically requires release manager permissions.

### Joining Work on a Release Branch

If someone else has already started a release branch:

```bash
# Fetch all branches
git fetch --all

# Track and checkout the release branch
git checkout release/9.3
```

### Fixing Bugs in a Release Branch

#### Starting a Bugfix

```bash
# Ensure you're on the release branch
git checkout release/9.3
git pull

# Create a bugfix branch
git checkout -b bugfix/LT-12345-fix-description
```

#### Submitting the Fix

1. Make your changes and commit them
2. Push your branch:
   ```bash
   git push -u origin bugfix/LT-12345-fix-description
   ```
3. Create a Pull Request targeting the release branch

#### After the PR is Merged

```bash
# Clean up local branch
git checkout release/9.3
git pull
git branch -d bugfix/LT-12345-fix-description
```

### Releasing a New Version

When the release is ready:

1. Ensure all pending PRs for the release branch are merged
2. Create a release tag:
   ```bash
   git checkout release/9.3
   git pull
   git tag -a v9.3.0 -m "Release 9.3.0"
   git push origin v9.3.0
   ```

3. Merge the release branch back to develop (if applicable)

## Hotfix Workflow

Hotfixes are for critical bugs in released versions that can't wait for the next scheduled release.

### Creating a Hotfix Branch

```bash
# Create hotfix from the release tag
git checkout v9.2.0
git checkout -b hotfix/9.2.1

# Push to remote
git push -u origin hotfix/9.2.1
```

### Fixing Bugs in a Hotfix Branch

Same process as release branch bugfixes:

```bash
git checkout hotfix/9.2.1
git checkout -b bugfix/LT-12345-critical-fix
# Make changes, commit, push, create PR
```

### Releasing a Hotfix

1. Complete all fixes on the hotfix branch
2. Create the hotfix release tag:
   ```bash
   git checkout hotfix/9.2.1
   git tag -a v9.2.1 -m "Hotfix release 9.2.1"
   git push origin v9.2.1
   ```

3. Merge hotfix changes back to the current release branch and develop

## Support Branches

For maintaining older versions (e.g., fixing bugs in version 9.0 when 9.2 is current):

### Creating a Support Branch

```bash
# Create support branch from the old release tag
git checkout v9.0.0
git checkout -b support/9.0
git push -u origin support/9.0
```

### Releasing a Support Fix

Process is similar to hotfixes, but hotfix branches are based on the support branch:

```bash
git checkout support/9.0
git checkout -b hotfix/9.0.1
```

## Merge Conflict Resolution

If you get merge failures when releasing:

1. Resolve the conflicts locally
2. Commit the resolution
3. Push and continue with the release

```bash
# After resolving conflicts
git add .
git commit -m "Resolve merge conflicts for release"
git push
```

## Version Numbering

FieldWorks uses semantic versioning:

- **Major** (9.x.x): Breaking changes, major new features
- **Minor** (x.3.x): New features, backwards compatible
- **Patch** (x.x.1): Bug fixes, backwards compatible

## See Also

- [Pull Request Workflow](pull-request-workflow.md) - Standard PR process
- [CONTRIBUTING.md](../CONTRIBUTING.md) - Getting started with contributions
