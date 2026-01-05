# Core Developer Setup

This document describes additional setup steps for core FieldWorks developers. Unless you are a core team member, you should follow the steps in [CONTRIBUTING.md](CONTRIBUTING.md) instead.

> **Note**: Core developers have direct commit access to the main repository and additional responsibilities for code review and release management.

## Prerequisites

Complete all steps in [CONTRIBUTING.md](CONTRIBUTING.md) first:
1. Install required software (Git, Visual Studio 2022)
2. Clone the repository
3. Verify you can build successfully

## Required Software

The following tools are required for FieldWorks development:

### Visual Studio 2022

Install with these workloads:
- **.NET desktop development**
- **Desktop development with C++** (including ATL/MFC components)

See [Visual Studio Setup](visual-studio-setup.md) for detailed component list.

### WiX Toolset (v6 via NuGet restore)

Installer builds use SDK-style `.wixproj` projects and restore WiX v6 tools via NuGet during the build. No separate WiX 3.x installation is required.

```powershell
# Standard developer machine setup
.\Setup-Developer-Machine.ps1

# Optional: set up installer helper repositories
.\Setup-Developer-Machine.ps1 -InstallerDeps
```

### Environment Variables

No WiX-specific environment variables are required for WiX v6 SDK builds.

### Verification

Run these commands to verify your environment:

```powershell
# Check Visual Studio
& "${env:ProgramFiles(x86)}\Microsoft Visual Studio\Installer\vswhere.exe" -latest

# Check Git
git --version

# Verify installer build prerequisites
.\Build\Agent\Setup-InstallerBuild.ps1 -ValidateOnly
```

## Additional Setup

### 1. Register with Services

#### GitHub

Ensure your GitHub account has the correct permissions:
- You should be a member of the [sillsdev](https://github.com/sillsdev) organization
- Request access to the FieldWorks repository with write permissions

Contact the team lead to request permissions if needed.

#### GitHub SSH Key (Recommended)

For streamlined pushing and pulling, set up an SSH key:

1. Generate an SSH key if you don't have one:
   ```bash
   ssh-keygen -t ed25519 -C "your_email@example.com"
   ```

2. Add the key to your GitHub account:
   - Go to **GitHub → Settings → SSH and GPG keys → New SSH key**
   - Paste your public key (`~/.ssh/id_ed25519.pub`)

3. Test the connection:
   ```bash
   ssh -T git@github.com
   ```

4. Update your remote to use SSH:
   ```bash
   git remote set-url origin git@github.com:sillsdev/FieldWorks.git
   ```

### 2. Configure Git for the Project

#### Set Identity

```bash
git config user.name "Your Name"
git config user.email "your.email@example.com"
```

#### Increase Rename Limits

```bash
git config diff.renameLimit 10000
git config merge.renameLimit 10000
```

#### Configure Branch Tracking

Set up tracking for release branches you'll be working on:

```bash
# Fetch all branches
git fetch --all

# Track a specific release branch
git checkout release/9.3
```

### 3. Development Environment

#### IDE Extensions

Consider installing these Visual Studio extensions:
- **ReSharper** (if you have a license) - Uses shared settings from `FW.sln.DotSettings`
- **GitHub Extension for Visual Studio** - For PR management

#### Git GUI Tools

Recommended Git tools:
- **Git GUI** (included with Git) - For commits and basic operations
- **GitKraken** or **SourceTree** - For visual branch management
- **VS Code** - Has excellent Git integration

### 4. Working with Branches

#### Branch Naming Conventions

- `feature/<name>` - New features
- `bugfix/<issue-number>-<description>` - Bug fixes
- `hotfix/<version>` - Emergency fixes for released versions
- `release/<version>` - Release preparation branches

#### Creating Feature Branches

```bash
# Create a new feature branch from the default branch
git checkout release/9.3
git pull
git checkout -b feature/my-feature-name
```

#### Submitting Changes

1. Push your branch to origin:
   ```bash
   git push -u origin feature/my-feature-name
   ```

2. Create a Pull Request on GitHub

3. Request review from team members

4. After approval and CI passes, merge the PR

See [Pull Request Workflow](workflows/pull-request-workflow.md) for detailed guidelines.

### 5. Release Management

If you are a release manager, additional setup may be required. Contact Jason Naylor for:
- Access to release automation scripts
- Build server access
- Installer signing certificates

## Git Configuration Reference

### Recommended Global Settings

```bash
# Use rebase by default when pulling
git config --global pull.rebase true

# Prune deleted remote branches on fetch
git config --global fetch.prune true

# Use diff3 conflict style for better merge conflict resolution
git config --global merge.conflictstyle diff3

# Enable helpful coloring
git config --global color.ui auto
```

### Repository-Specific Settings

These are set in the FieldWorks repository:

```bash
# Increase rename detection limits
git config diff.renameLimit 10000
git config merge.renameLimit 10000
```

## Troubleshooting

### Permission Denied on Push

If you get "Permission denied" when pushing:
1. Verify you have write access to the repository
2. Check your SSH key is properly configured
3. Ensure you're not pushing to a protected branch directly

### Branch Not Found

If a branch you're looking for isn't available:
```bash
git fetch --all
git branch -a  # List all branches including remote
```

## See Also

- [CONTRIBUTING.md](CONTRIBUTING.md) - Basic setup for all contributors
- [Pull Request Workflow](workflows/pull-request-workflow.md) - How to submit changes
- [Release Process](workflows/release-process.md) - Release workflow documentation
