# Research: Wiki Documentation Migration

**Feature**: 007-wiki-docs-migration
**Date**: 2025-12-01

## Executive Summary

Analysis of the FwDocumentation wiki identified ~50 pages across 5 major categories. The migration strategy splits content between `.github/instructions/` (code guidance) and `docs/` (onboarding/tutorials). Legacy Gerrit/Jenkins workflows require significant rewriting to GitHub equivalents.

## Wiki Content Inventory

### Category 1: Getting Started (Priority: P1) ✅ MIGRATE

| Wiki Page | Status | Target Location | Notes |
|-----------|--------|-----------------|-------|
| Contributing to FieldWorks Development | ACTIVE | `docs/CONTRIBUTING.md` | Core contributor guide; needs update for GitHub flow |
| Getting Started for Core Developers | PARTIALLY OBSOLETE | `docs/core-developer-setup.md` | Gerrit/fwmeta references obsolete; SSH key setup still valid |
| Set Up Visual Studio for FieldWorks Development on Windows | ACTIVE | `docs/visual-studio-setup.md` | VS 2022 instructions current; build.bat references need update to build.ps1 |

**Decision**: Migrate with updates. Replace fwmeta/initrepo with direct git clone. Update build commands to use `build.ps1`.

### Category 2: Workflow (Priority: P2) ⚠️ REWRITE

| Wiki Page | Status | Target Location | Notes |
|-----------|--------|-----------------|-------|
| Workflow Overview | OBSOLETE | N/A | Gerrit-centric; replace entirely |
| Detailed Description of the Workflow | OBSOLETE | N/A | Gerrit-centric; replace entirely |
| Workflow Step by Step | OBSOLETE | `docs/workflows/pull-request-workflow.md` | Rewrite for GitHub PRs |
| Release Workflow Steps | CONFIRMATION_NEEDED | `docs/workflows/release-process.md` | May still be relevant for release managers |
| Code Reviews | PARTIALLY OBSOLETE | `.github/instructions/code-review.instructions.md` | Gerrit UI obsolete; review principles still valid |

**Decision**: Create new GitHub-native workflow docs. Preserve code review principles. Mark release workflow for `CONFIRMATION_NEEDED`.

### Category 3: Coding Standards (Priority: P2) ✅ MIGRATE

| Wiki Page | Status | Target Location | Notes |
|-----------|--------|-----------------|-------|
| Coding Standard | ACTIVE | `.github/instructions/coding-standard.instructions.md` | Commit message and whitespace rules still enforced |
| Palaso Coding Standards (linked) | EXTERNAL | Link only | External doc; preserve link |

**Decision**: Migrate to instruction file. Merge with existing `.editorconfig` guidance.

### Category 4: Architecture (Priority: P3) ✅ MIGRATE

| Wiki Page | Status | Target Location | Notes |
|-----------|--------|-----------------|-------|
| Data Migrations | ACTIVE | `docs/architecture/data-migrations.md` | Critical for FLEx developers; file paths need verification |
| Dependencies on Other Repos | PARTIALLY ACTIVE | `docs/architecture/dependencies.md` | TeamCity references obsolete; dependency concepts valid |
| Dispose | ACTIVE | `.github/instructions/dispose.instructions.md` | IDisposable patterns still relevant |

**Decision**: Migrate Data Migrations as high priority. Verify file paths in current codebase.

### Category 5: Linux/Platform ❌ DO NOT MIGRATE (OBSOLETE)

| Wiki Page | Status | Notes |
|-----------|--------|-------|
| Build FieldWorks (Linux) | OBSOLETE | Linux builds no longer supported |
| Using Vagrant | OBSOLETE | Vagrant development box no longer maintained |
| Flatpak packaging | OBSOLETE | Flatpak packaging discontinued |

**Decision (2025-12-02)**: Do not migrate Linux/platform documentation. FieldWorks is Windows-only. The `vagrant/` folder in the repository is legacy and the Linux build tooling is not maintained. Stakeholder clarification confirmed this content is obsolete.

### Category 6: Historical/Obsolete ❌ DO NOT MIGRATE

| Wiki Page | Reason |
|-----------|--------|
| Gerrit: Linking Accounts | Gerrit no longer used |
| Transition to Google Apps | Historical (2013) |
| Installing Hudson Jenkins On Windows | Jenkins replaced by GitHub Actions |
| Mono and Gerrit | Gerrit no longer used |
| fwmeta/initrepo instructions | Replaced by direct git clone |

**Decision**: Do not migrate. These are historical artifacts.

## Stakeholder Clarifications (2025-12-02)

The following clarifications were received from stakeholders:

### Release Process
- ✅ Release branch naming `release/X.Y` is still used
- ✅ Hotfix workflow (branch from release, cherry-pick to develop) is still followed
- ✅ **Release Manager**: Jason Naylor

### Data Migrations
- ❌ `Src/FDO/` path does not exist in this repo - data migrations are in [liblcm](https://github.com/sillsdev/liblcm)
- ❌ FDO namespace was renamed to LCM (lives in liblcm repo)
- ❌ Migration test data not in `TestLangProj/` - lives in liblcm repo

### External Repository Dependencies
- ❌ `sillsdev/FwSampleProjects` - No longer used
- ✅ `sillsdev/FwLocalizations` - Still used for translations (Crowdin integration)
- ❌ `sillsdev/Helps` - No longer used

### Prerequisites
- ✅ Visual Studio 2022 with .NET desktop + C++ workloads (including ATL/MFC)
- ✅ WiX Toolset 3.14.1 for installer building
- ✅ Git for Windows
- `Setup-Developer-Machine.ps1` automates tool installation

### Linux Support
- ❌ Linux builds are **obsolete** - FieldWorks is Windows-only
- ❌ Vagrant development box is obsolete
- ❌ Flatpak packaging is obsolete
- ✅ `build.sh` removed from repository (2025-12-02)

---

## Technical Decisions

### Decision 1: Documentation Structure

**Decision**: Split documentation between two locations
- `.github/instructions/` - Copilot-facing code guidance (instruction files)
- `docs/` - Human-facing onboarding and tutorials

**Rationale**: The repo already has 30+ instruction files in `.github/instructions/`. These are consumed by Copilot for code review. Human onboarding docs belong in `docs/` following GitHub conventions.

**Alternatives Considered**:
- Single `docs/` folder: Rejected because it would duplicate existing instruction file content
- Single `.github/instructions/`: Rejected because instruction files have specific formatting requirements

### Decision 2: Gerrit → GitHub Workflow Translation

**Decision**: Create new workflow documentation for GitHub-native processes

| Gerrit Concept | GitHub Equivalent |
|----------------|-------------------|
| `git review` | `git push origin <branch>` + PR |
| Change-Id in commits | PR number |
| +2 Code Review | PR Approval |
| Verified by Jenkins | GitHub Actions checks |
| Submit button | Merge button |
| `git start task` | `git checkout -b feature/<name>` |
| `git finish task` | Merge PR + delete branch |

**Rationale**: Gerrit workflow is fundamentally different. Line-by-line translation would be confusing.

### Decision 3: File Path Verification

**Decision**: Validate all file paths mentioned in wiki against current codebase

| Wiki Reference | Current Status |
|----------------|----------------|
| `C:\fwrepo\fw\` | ❌ Obsolete path convention |
| `$FWROOT` | ✅ Valid environment variable concept |
| `Build\build.bat` | ⚠️ Exists but `build.ps1` preferred |
| `remakefw` target | ✅ Still exists in mkall.targets |
| `FDO` namespace | ⚠️ May have been renamed; verify |

**Rationale**: Wiki docs reference old paths. Must verify against current repo structure.

### Decision 4: Image Handling

**Decision**: Download referenced images to `docs/images/`

**Rationale**: Wiki images hosted on GitHub wiki storage. Need local copies for in-repo docs.

**Implementation**: Extract image URLs from wiki pages, download, store with attribution.

## Validation Results

### Codebase Verification

| Item | Wiki Says | Codebase Reality | Action |
|------|-----------|------------------|--------|
| Build script | `build.bat` | `build.ps1` exists, preferred | Update |
| Solution file | `FW.sln` | `FieldWorks.sln` exists | Update |
| fwmeta/initrepo | Required for setup | Not needed; direct clone works | Remove |
| Gerrit SSH port 59418 | Required | N/A - GitHub uses HTTPS | Remove |
| Visual Studio 2022 | Required | ✅ Correct | Keep |
| .NET Framework 4.6.1 | Required | ⚠️ Verify current requirements | Verify |

### Existing Instruction Files (No Duplication)

The following topics already have instruction files - wiki content should reference, not duplicate:

- `build.instructions.md` - Build system (comprehensive)
- `testing.instructions.md` - Test execution
- `managed.instructions.md` - C# development
- `native.instructions.md` - C++ development
- `security.instructions.md` - Security practices
- `csharp.instructions.md` - C# coding guidelines

## Dependencies

### External Documentation

- Palaso Coding Standards: External Google Doc (preserve link)
- FwLocalizations: Separate repo (Crowdin translations workflow)

### Related Repos

| Repo | Purpose | Status |
|------|---------|--------|
| sillsdev/liblcm | Data model and migrations | ✅ Active - primary dependency |
| sillsdev/libpalaso | SIL shared utilities | ✅ Active - primary dependency |
| sillsdev/chorus | Version control for Send/Receive | ✅ Active - primary dependency |
| sillsdev/FwLocalizations | Translations | ✅ Active - Crowdin integration |
| sillsdev/FwSampleProjects | Test data | ❌ No longer used |
| sillsdev/Helps | Help files | ❌ No longer used |
| sillsdev/fwmeta | Setup scripts | ❌ OBSOLETE - do not reference |

## Risk Assessment

| Risk | Likelihood | Impact | Mitigation |
|------|------------|--------|------------|
| Outdated file paths in migrated docs | High | Medium | Verify each path before migration |
| Missing Gerrit→GitHub translation | Medium | High | Create comprehensive workflow mapping |
| Duplicate content with instruction files | Low | Medium | Cross-reference, don't duplicate |

## Next Steps

1. Create `docs/` directory structure
2. Migrate P1 pages (Contributing, Setup) with updates
3. Create new GitHub workflow docs (P2)
4. Migrate architecture docs with path verification (P3)
5. Update ReadMe.md to link to new docs
