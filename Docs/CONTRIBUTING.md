# Contributing to FieldWorks Development

Thank you for your interest in contributing to FieldWorks (FLEx)!

## Ways to Contribute

There are several ways you can contribute to the development of FieldWorks:

- **Contributing code** - Fix bugs, add features, improve documentation
- **Testing alpha and beta versions** - Help us find and report issues
- **Reporting issues** - File bugs on [GitHub Issues](https://github.com/sillsdev/FieldWorks/issues)

## Getting Started

The following steps are required for setting up a FieldWorks development environment on Windows.

> **Note**: FieldWorks is Windows-only. Linux builds are no longer supported.

### 1. Install Required Software

#### Git

Download and install the latest version of [Git](https://git-scm.com/).

During installation:
- On "Adjusting your PATH environment": Select any option you want - "Use Git Bash only" is sufficient unless you want to run git commands from the Windows command prompt.
- On "Configuring the line ending conversions": Select **"Checkout Windows-style, commit Unix-style line endings"**.

#### Visual Studio 2022

Download and install Visual Studio 2022 Community Edition or higher. See [Visual Studio Setup](visual-studio-setup.md) for detailed configuration.

Required workloads:
- .NET desktop development
- Desktop development with C++ (including ATL/MFC)

#### WiX Toolset 3.14.1 (for installer building)

Run the automated setup script:

```powershell
.\Setup-Developer-Machine.ps1
```

Or install manually from [WiX releases](https://github.com/wixtoolset/wix3/releases/tag/wix3141rtm).

### 2. Clone the Repository

Clone the FieldWorks repository using HTTPS or SSH:

**HTTPS:**
```bash
git clone https://github.com/sillsdev/FieldWorks.git
cd FieldWorks
```

**SSH:**
```bash
git clone git@github.com:sillsdev/FieldWorks.git
cd FieldWorks
```

#### Optional: Clone FwLocalizations (for translation work)

If you're working on translations:

```bash
git clone https://github.com/sillsdev/FwLocalizations.git Localizations
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

# For FlexBridge development (optional)
$env:FIELDWORKSDIR = "C:\path-to-repo\Output\Debug"
```

> **Tip**: Add these to your PowerShell profile or system environment variables for persistence.

### 4. Build FieldWorks

Build FieldWorks using the PowerShell build script:

```powershell
.\build.ps1
```

For more build options, see [.github/instructions/build.instructions.md](../.github/instructions/build.instructions.md).

#### Git Configuration Tips

It is helpful to increase the rename limits for Git to properly detect renames in large commits:

```bash
git config diff.renameLimit 10000
git config merge.renameLimit 10000
```

## Contributing Code

### General Guidelines

- **Write tests**: For any new functionality and when modifying existing code, write NUnit tests. This helps others not introduce problems and assists in maintaining existing functionality.

- **Follow coding standards**: Please review our [Coding Standards](../.github/instructions/coding-standard.instructions.md). Note that our coding format is different from the default style in Visual Studio.

- **Make sure tests pass**: Ensure all tests pass before submitting. Tests are directly integrated into our build system.

### Contributing Changes

We welcome any contribution! To get started:

1. **Fork** the FieldWorks repository on GitHub
2. **Clone** your fork locally
3. **Create a branch** for your changes:
   ```bash
   git checkout -b feature/my-feature-name
   ```
4. **Make your changes** and commit them with clear messages
5. **Push** to your fork
6. **Submit a pull request** to the main repository

See [workflows/pull-request-workflow.md](workflows/pull-request-workflow.md) for detailed PR guidelines.

### Becoming a Core Developer

People we know well might be asked to join the core development team. Core developers get additional privileges including the ability to make branches directly in the main repository and contribute in additional ways.

## Getting Help

- **Documentation**: Check the [docs/](.) folder for additional guides
- **Issues**: Search or file issues on [GitHub](https://github.com/sillsdev/FieldWorks/issues)
- **Wiki**: Historical documentation at [FwDocumentation wiki](https://github.com/sillsdev/FwDocumentation/wiki) (being migrated to this repository)

## See Also

- [Visual Studio Setup](visual-studio-setup.md) - Detailed VS configuration
- [Core Developer Setup](core-developer-setup.md) - Additional setup for core developers
- [Pull Request Workflow](workflows/pull-request-workflow.md) - How to submit changes
- [Build Instructions](../.github/instructions/build.instructions.md) - Detailed build guide
