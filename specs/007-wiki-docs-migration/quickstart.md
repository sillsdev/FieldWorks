# Quickstart: Wiki Documentation Migration

**Feature**: 007-wiki-docs-migration
**Date**: 2025-12-01

## Prerequisites

- Git access to FieldWorks repository
- Text editor (VS Code recommended)
- Browser access to https://github.com/sillsdev/FwDocumentation/wiki

## Quick Reference

### Documentation Locations

| Content Type | Location | Example |
|--------------|----------|---------|
| Code guidance (Copilot) | `.github/instructions/` | `coding-standard.instructions.md` |
| Onboarding tutorials | `docs/` | `CONTRIBUTING.md` |
| Workflow guides | `docs/workflows/` | `pull-request-workflow.md` |
| Architecture docs | `docs/architecture/` | `data-migrations.md` |
| Linux/Platform docs | `docs/linux/` | `build-linux.md` |

### Migration Status Markers

```markdown
# For verified content (no marker needed)

# For unverified content:
> ⚠️ **CONFIRMATION_NEEDED**: This section needs verification against current codebase.

# For historical context:
> 📝 **Historical Note**: This describes the legacy Gerrit workflow. See [current workflow](./pull-request-workflow.md).
```

### Common Transformations

| Wiki Content | Replace With |
|--------------|--------------|
| `C:\fwrepo\fw\` | `<repo-root>/` or relative path |
| `build.bat` | `build.ps1` |
| `FW.sln` | `FieldWorks.sln` |
| `git review` | Push branch + create PR |
| `git start task` | `git checkout -b feature/name` |
| Gerrit +2 approval | GitHub PR approval |

## Implementation Checklist

### Phase 1: Setup (P1)
- [ ] Create `docs/` directory structure
- [ ] Create `docs/CONTRIBUTING.md` from wiki
- [ ] Create `docs/visual-studio-setup.md` from wiki
- [ ] Update `ReadMe.md` to link to docs

### Phase 2: Workflows (P2)
- [ ] Create `docs/workflows/pull-request-workflow.md` (new content)
- [ ] Create `docs/workflows/release-process.md` from wiki (with CONFIRMATION_NEEDED)
- [ ] Create `.github/instructions/code-review.instructions.md`

### Phase 3: Architecture (P3)
- [ ] Create `docs/architecture/data-migrations.md` from wiki
- [ ] Create `docs/architecture/dependencies.md` from wiki
- [ ] Create `.github/instructions/coding-standard.instructions.md`
- [ ] Create `.github/instructions/dispose.instructions.md`

### Phase 4: Platform Docs (P3)
- [ ] Create `docs/linux/build-linux.md` from wiki (with CONFIRMATION_NEEDED)
- [ ] Create `docs/linux/vagrant.md` from wiki (with CONFIRMATION_NEEDED)

### Phase 5: Validation
- [ ] Run link checker on all docs
- [ ] Verify all file paths against codebase
- [ ] Test contributor journey end-to-end

## Instruction File Template

```markdown
---
applyTo: "<glob-pattern>"
name: "<file-name-without-extension>"
description: "<brief description>"
---

# Title

## Purpose & Scope
Brief description of what this guidance covers.

## Key Rules
- Rule 1
- Rule 2

## Examples
\`\`\`csharp
// Example code
\`\`\`
```

## Doc File Template

```markdown
# Title

Brief introduction.

## Prerequisites

What you need before starting.

## Steps

### Step 1: ...

Instructions...

### Step 2: ...

Instructions...

## Troubleshooting

Common issues and solutions.

## See Also

- [Related Doc](./related.md)
- [Instruction File](../.github/instructions/related.instructions.md)
```

## Validation Commands

```powershell
# Check for broken links (requires markdown-link-check)
npx markdown-link-check docs/**/*.md

# Verify instruction file format
python scripts/tools/validate_instructions.py

# Check COPILOT.md files
python .github/check_copilot_docs.py --only-changed --fail
```

## Success Criteria Verification

| Criterion | How to Verify |
|-----------|---------------|
| SC-001: Essential pages migrated | Check `docs/CONTRIBUTING.md`, `docs/visual-studio-setup.md`, etc. exist |
| SC-002: No broken links | Run `markdown-link-check` |
| SC-003: 2-hour contributor test | Manual test with new developer |
| SC-004: ReadMe links | Check `ReadMe.md` has links to `docs/` |
| SC-005: Gerrit content updated | Search for "gerrit" in migrated docs |
