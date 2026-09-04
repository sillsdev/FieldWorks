# Contributing to FieldWorks Development

Thank you for your interest in contributing to FieldWorks (FLEx)!

## Ways to Contribute

There are several ways you can contribute to the development of FieldWorks:

- **Contributing code** - Fix bugs, add features, improve documentation
- **Testing alpha and beta versions** - Help us find and report issues ([download](https://software.sil.org/fieldworks/download/) from our website or choose Tools > Options > Updates and automatically download Alpha or Beta updates)
- **Reporting bugs** - We plan to enable [GitHub Issues](https://github.com/sillsdev/FieldWorks/issues) in the near future. Until then, you can
  - Choose Help > Report a Problem... from within FieldWorks
  - Fill out the [contact form](https://software.sil.org/fieldworks/about/contact/) at our website
  - Create an account on [Jira](https://jira.sil.org/issues/?jql=project%20%3D%20LT)

## Getting Started

The following steps are required for setting up a FieldWorks development environment on Windows.

> **Note**: FieldWorks build, test, installer, and setup workflows are Windows-only.
> Linux and macOS are supported for editing, code search, documentation, specs, and agent work only.

### 1. Install Required Software

#### Git

Download and install the latest version of [Git](https://git-scm.com/).

During installation:
- On "Adjusting your PATH environment": Select any option you want - "Use Git Bash only" is sufficient unless you want to run git commands from the Windows command prompt.
- On "Configuring the line ending conversions": Select **"Checkout Windows-style, commit Unix-style line endings"**.

#### Visual Studio 2026 or 2022

Download and install Visual Studio Community Edition or higher (2026 preferred; when both are installed the build uses the newest). See [Visual Studio Setup](visual-studio-setup.md) for detailed configuration, or import the repo-root `.vsconfig` in the Visual Studio Installer.

Required workloads:
- .NET desktop development
- Desktop development with C++ (including ATL/MFC)

#### Windows Defender Exclusions (Recommended)

FieldWorks builds can be significantly slowed by Windows Defender real-time scanning. To configure exclusions, run the following in an **Administrator PowerShell**:

```powershell
.\Build\Agent\Setup-DefenderExclusions.ps1
```

This adds exclusions for build outputs, NuGet caches and development tools. Use `-DryRun` to preview changes without applying them.

If you also work in sibling repos in the same parent folder (e.g. PanGloss, motif, foma-rs), run `..\Setup-DefenderExclusions.ps1` instead (one level up) — it covers this repo plus the Rust toolchain (`.cargo`/`.rustup`) and CMake/Rust process exclusions the FieldWorks-only script doesn't need.

### 2. Clone the Repository

Clone the FieldWorks repository using HTTPS or SSH:

**HTTPS:**
```powershell
git clone https://github.com/sillsdev/FieldWorks.git
cd FieldWorks
```

**SSH:**
```powershell
git clone git@github.com:sillsdev/FieldWorks.git
cd FieldWorks
```

#### Run the setup script

```powershell
.\Setup-Developer-Machine.ps1

# Option: set up with installer helper repositories (Helps, Localizations, etc.)
.\Setup-Developer-Machine.ps1 -InstallerDeps
```

See [Installer Build Guide](installer-build-guide.md) for building installers locally.

#### Optional: Clone FwLocalizations and LibLCM (for translation work)

If you're working on translations, and you didn't run `.\Setup-Developer-Machine.ps1 -InstallerDeps`:

```powershell
git clone https://github.com/sillsdev/FwLocalizations.git Localizations
git clone https://github.com/sillsdev/liblcm.git Localizations/LCMRepo
```

#### Set up fonts for Non-Roman test data

Install the PigLatin font by right-clicking on `DistFiles/Graphite/pl/piglatin.ttf` and selecting **Install**.

### 3. Set Environment Variables

Add the following environment variables:

```powershell
# Prevent sending usage statistics
$env:FEEDBACK = "off"

# Set up ICU data path (required for debugging ICU-related projects)
$env:ICU_DATA = "C:\path-to-repo\DistFiles\Icu70\icudt70l"

# For Paratext integration (optional)
$env:FIELDWORKSDIR = "C:\path-to-repo\Output\Debug"

# For FlexBridge development (optional)
$env:FLEXBRIDGEDIR = "C:\path-to-repo\Output\Debug\net462"

# For working on translations (optional)
$env:LcmRootDir = "C:\path-to-liblcm-repo"
```

> **Tip**: Add these to your PowerShell profile or system environment variables for persistence.

### 4. Build FieldWorks

Build FieldWorks using the PowerShell build script:

```powershell
.\build.ps1
```

For more build options, see [.github/instructions/build.instructions.md](../.github/instructions/build.instructions.md).

On Linux or macOS, do not run `build.ps1` or `test.ps1`; those entry points intentionally fail fast with a not-supported message.

### Run tests from the command line

Use `test.ps1` for local test runs:

```powershell
.\test.ps1
.\test.ps1 -TestProject "Src/Common/FwUtils/FwUtilsTests"
.\test.ps1 -SkipManaged -TestProject TestGeneric
```

By default, test runs suppress FieldWorks assertion dialogs so unattended runs cannot hang on Abort/Retry/Ignore UI. For a local debugger session where you intentionally want the previous interactive assertion dialog behavior, use the command-line switch:

```powershell
.\test.ps1 -SkipManaged -TestProject TestGeneric -AllowAssertDialogs
```

The environment-variable equivalent is `FW_TEST_ALLOW_ASSERT_DIALOGS=1`:

```powershell
$env:FW_TEST_ALLOW_ASSERT_DIALOGS = '1'
.\test.ps1 -SkipManaged -TestProject TestGeneric
Remove-Item Env:FW_TEST_ALLOW_ASSERT_DIALOGS
```

Only use this opt-in for attended local debugging. CI and normal local runs should leave it unset.

### 5. VS Code and Visual Studio usage

Default recommendation:
- Use **VS Code + ReSharper extension** for everyday coding, navigation, and managed test explorer workflows.
- **C# Dev Kit is discouraged** in this workspace (it doesn't support debugging or test discovery for legacy .NET Framework projects, which this workspace uses).
- Use repo scripts/tasks as source of truth for build/test: `./build.ps1` and `./test.ps1`.

Switch to **Visual Studio** (2026 or 2022) when you need:
- WinForms designer workflows
- Mixed managed/native debugging across interop boundaries
- Complex legacy .NET Framework project-system scenarios where VS Code is unreliable

See [VS Code Stability Profile](vscode-stability-profile.md) for current workspace guidance.

## Git Configuration

It is helpful to increase the rename limits for Git to properly detect renames in large commits:

```powershell
git config diff.renameLimit 10000
git config merge.renameLimit 10000
```

### Recommended Global Settings

```powershell
# Use rebase by default when pulling
git config --global pull.rebase true

# Prune deleted remote branches on fetch
git config --global fetch.prune true

# Use diff3 conflict style for better merge conflict resolution
git config --global merge.conflictstyle diff3

# Enable helpful coloring
git config --global color.ui auto
```

## Contributing Code

### General Guidelines

- **Write tests**: For any new functionality and when modifying existing code, write NUnit tests. This helps others not introduce problems and assists in maintaining existing functionality.

- **Follow formatting and commit conventions**: Use `.editorconfig` for formatting and see [commit message guidelines](../.github/commit-guidelines.md) for CI-enforced commit rules.

- **Make sure tests pass**: Ensure all tests pass before submitting. Tests are directly integrated into our build system.

### Contributing Changes

We welcome any contribution! To get started:

1. **Fork** the FieldWorks repository on GitHub
2. **Clone** your fork locally
3. **Create a branch** for your changes:
   ```powershell
   git checkout -b feature/my-feature-name
   ```
4. **Make your changes** and commit them with clear messages
5. **Push** to your fork
6. **Submit a pull request** to the main repository

See [workflows/pull-request-workflow.md](workflows/pull-request-workflow.md) for detailed PR guidelines.

Core developers doing AI-assisted work should start with [AI-Assisted PR Workflow](workflows/ai-pr-workflow.md) instead of jumping directly to the generic PR checklist.

### Becoming a Core Developer

People we know well might be asked to join the core development team. Core developers get additional privileges including the ability to make branches directly in the main repository and contribute in additional ways.

## Getting Help

- **Documentation**: Check the [docs/](.) folder for additional guides
- **Issues**: Search or file issues on [Jira](https://jira.sil.org/issues/?jql=project%20%3D%20LT)
- **Wiki**: Historical documentation at [FwDocumentation wiki](https://github.com/sillsdev/FwDocumentation/wiki) (being migrated to this repository)

## See Also

- [Visual Studio Setup](visual-studio-setup.md) - Detailed VS configuration
- [Core Developer Setup](core-developer-setup.md) - Additional setup for core developers
- [AI-Assisted PR Workflow](workflows/ai-pr-workflow.md) - Canonical Jira-to-PR workflow for core developers using GitHub Copilot or Claude Code
- [Pull Request Workflow](workflows/pull-request-workflow.md) - How to submit changes
- [Build Instructions](../.github/instructions/build.instructions.md) - Detailed build guide
