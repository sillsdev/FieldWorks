# FieldWorks Spec-Kit Customizations

This document guides Copilot (and developers) on how to maintain FieldWorks-specific
customizations when updating the upstream spec-kit files.

## Architecture

FieldWorks extends spec-kit without modifying upstream files directly:

```
.specify/scripts/powershell/
├── common.ps1           # UPSTREAM - do not modify
├── fw-extensions.ps1    # FW CUSTOM - our function overrides
├── fw-common.ps1        # FW CUSTOM - loader that sources both
├── create-new-feature.ps1   # UPSTREAM - do not modify
├── setup-plan.ps1           # UPSTREAM - do not modify
└── update-agent-context.ps1 # UPSTREAM - do not modify
```

## What fw-extensions.ps1 Provides

1. **`Normalize-FeatureBranchName`** - Strips prefixes like `specs/`, `feature/`, etc.
2. **`Test-FeatureBranch`** (override) - Enforces branch naming with prefixes
3. **`Get-FeatureDir`** (override) - Uses normalized branch names for directory mapping
4. **`Get-FeaturePathsEnv`** (override) - Adds `FEATURE_NAME` property

## Updating Spec-Kit

When pulling new versions of spec-kit:

1. **Pull the upstream changes** - Let them overwrite `common.ps1` and other upstream files
2. **Check for conflicts** - Run: `git diff HEAD .specify/scripts/powershell/`
3. **Verify extensions still work** - The overrides in `fw-extensions.ps1` should still apply
4. **Update fw-extensions.ps1 if needed** - If upstream changed function signatures

### Post-Update Checklist

After updating spec-kit, verify:

- [ ] `fw-extensions.ps1` still loads without errors
- [ ] `Normalize-FeatureBranchName` works: `specs/005-test` → `005-test`
- [ ] `Test-FeatureBranch` enforces prefixes
- [ ] `Get-FeaturePathsEnv` returns `FEATURE_NAME` property

```powershell
# Quick verification
. .specify/scripts/powershell/fw-common.ps1
Normalize-FeatureBranchName "specs/005-test-feature"  # Should return: 005-test-feature
(Get-FeaturePathsEnv).FEATURE_NAME  # Should return normalized name
```

## If Upstream Breaks Extensions

If spec-kit changes break our extensions:

1. Check what changed in `common.ps1`
2. Update `fw-extensions.ps1` to match new signatures
3. Add any new properties/functions needed
4. Test with the verification commands above

## Scripts That Need fw-common.ps1

If you create new scripts that need FieldWorks branch normalization:

```powershell
# Use this instead of common.ps1
. "$PSScriptRoot/fw-common.ps1"

# Now you have access to:
# - All upstream common.ps1 functions
# - Normalize-FeatureBranchName
# - Overridden Test-FeatureBranch, Get-FeatureDir, Get-FeaturePathsEnv
```

## Why This Architecture?

- **Minimal merge conflicts** - Upstream files can be replaced wholesale
- **Clear separation** - FW customizations are isolated in `fw-extensions.ps1`
- **Easy updates** - Just pull new spec-kit, verify extensions still work
- **Discoverable** - This README explains everything
