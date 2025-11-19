# PackageReference Management - Quick Start Guide

This guide shows you how to use the Python automation scripts to modernize DLL handling in FieldWorks.

## The Problem

Currently, many NuGet packages are manually copied from the `packages/` folder to the Output directory via MSBuild targets in `Build/mkall.targets`. This is inefficient and hard to maintain.

## The Solution

Use standard NuGet PackageReference in .csproj files. MSBuild automatically handles copying DLLs and resolving transitive dependencies.

## Three Simple Scripts

### 1. Find which projects use a namespace

```bash
python find_projects_using_namespace.py "Newtonsoft.Json"
```

**Output:**
```
Src/xWorks/xWorks.csproj ✗ (missing PackageReference)
  3 file(s) with matches
```

### 2. Add packages to projects

```bash
python add_package_reference.py Newtonsoft.Json "13.0.3" Src/xWorks/xWorks.csproj
```

**Output:**
```
✓ Added 'Newtonsoft.Json' v13.0.3 to Src/xWorks/xWorks.csproj
```

### 3. Find and remove unused packages

```bash
# Find unused
python find_projects_using_namespace.py "System.ValueTuple" \
  --check-package "System.ValueTuple" --find-unused

# Remove them
python remove_package_reference.py System.ValueTuple "Src/**/*.csproj"
```

## Complete Workflow Examples

### Adding a New Package to Projects That Need It

**Step 1: Find which projects use it**
```bash
python find_projects_using_namespace.py "Newtonsoft.Json" \
  --check-package "Newtonsoft.Json"
```

**Step 2: Generate the add command**
```bash
python find_projects_using_namespace.py "Newtonsoft.Json" \
  --check-package "Newtonsoft.Json" \
  --generate-command "Newtonsoft.Json" "13.0.3"
```

**Step 3: Copy and run the generated command**
```bash
python add_package_reference.py Newtonsoft.Json "13.0.3" \
  Src/Project1/Project1.csproj \
  Src/Project2/Project2.csproj
```

**Step 4: Test the build**
```bash
./build.ps1
```

### Cleaning Up Unused Packages

**Step 1: Find unused packages**
```bash
python find_projects_using_namespace.py "System.Memory" \
  --check-package "System.Memory" --find-unused
```

**Step 2: Generate remove command**
```bash
python find_projects_using_namespace.py "System.Memory" \
  --check-package "System.Memory" \
  --find-unused --generate-remove-command
```

**Step 3: Run the generated command**
```bash
python remove_package_reference.py System.Memory \
  Src/Project1/Project1.csproj \
  Src/Project2/Project2.csproj
```

### Batch Operations with Glob Patterns

**Add to multiple projects at once:**
```bash
# Add Moq to all test projects
python add_package_reference.py Moq "4.17.2" "Src/**/*Tests/*.csproj"

# Add icu.net to all Common projects
python add_package_reference.py icu.net "3.0.0-beta.297" "Src/Common/**/*.csproj"
```

**Always use --dry-run first:**
```bash
python add_package_reference.py --dry-run icu.net "3.0.0-beta.297" "Src/**/*.csproj"
```

## Common Scenarios

### Scenario 1: Adding Standard .NET Packages

```bash
# Find projects using System.Drawing.Common
python find_projects_using_namespace.py "System.Drawing.Common" \
  --generate-command "System.Drawing.Common" "9.0.0"

# Run the generated command
python add_package_reference.py System.Drawing.Common "9.0.0" \
  Src/Common/Controls/FwControls/FwControls.csproj \
  Src/Common/Controls/Widgets/Widgets.csproj
```

### Scenario 2: Adding Test Packages with PrivateAssets

```bash
# Add test utilities to test projects only
python add_package_reference.py --attr PrivateAssets=All \
  SIL.TestUtilities "17.0.0-*" \
  "Src/**/*Tests/*.csproj"
```

### Scenario 3: Finding All Projects Using ICU

```bash
# Search for multiple patterns
python find_projects_using_namespace.py "Icu" \
  --include-patterns "Collator" "Normalizer" "BreakIterator" \
  --check-package "icu.net" \
  --show-files
```

### Scenario 4: Bulk Cleanup

```bash
# Find all transitive packages that might not be needed
for pkg in System.Buffers System.Memory System.Runtime.CompilerServices.Unsafe; do
  echo "=== Checking $pkg ==="
  python find_projects_using_namespace.py "$pkg" \
    --check-package "$pkg" --find-unused
done
```

## Integration with Modernization Plan

These scripts implement the phases from `DLL_MODERNIZATION_PLAN.md`:

### Phase 1: Standard .NET Packages
```bash
# icu.net
python find_projects_using_namespace.py "Icu" \
  --generate-command "icu.net" "3.0.0-beta.297"

# Newtonsoft.Json
python find_projects_using_namespace.py "Newtonsoft.Json" \
  --generate-command "Newtonsoft.Json" "13.0.3"

# Moq (test projects)
python find_projects_using_namespace.py "Moq" \
  --generate-command "Moq" "4.17.2"
```

### Phase 2: SIL Packages
```bash
# Most SIL packages are already partially done
# Find which projects are missing them

python find_projects_using_namespace.py "SIL.Lexicon" \
  --check-package "SIL.Lexicon" \
  --generate-command "SIL.Lexicon" "17.0.0-*"

python find_projects_using_namespace.py "SIL.Scripture" \
  --check-package "SIL.Scripture" \
  --generate-command "SIL.Scripture" "17.0.0-*"
```

### Phase 3: Third-Party Packages
```bash
python find_projects_using_namespace.py "L10NSharp" \
  --generate-command "L10NSharp" "9.0.0-*"

python find_projects_using_namespace.py "Enchant" \
  --generate-command "Enchant.Net" "1.4.3-beta0010"
```

## Tips and Best Practices

### Always Test with --dry-run First
```bash
# Preview changes
python add_package_reference.py --dry-run --verbose \
  icu.net "3.0.0-beta.297" "Src/**/*.csproj"

# If it looks good, run for real
python add_package_reference.py \
  icu.net "3.0.0-beta.297" "Src/**/*.csproj"
```

### Use Quotes for Glob Patterns
```bash
# Good - quoted
python add_package_reference.py Moq "4.17.2" "Src/**/*Tests/*.csproj"

# Bad - shell expands it
python add_package_reference.py Moq "4.17.2" Src/**/*Tests/*.csproj
```

### Check Your Work
```bash
# After adding packages, verify they're there
git diff Src/Common/FwUtils/FwUtils.csproj

# Build to ensure everything works
./build.ps1

# Commit incrementally
git add Src/Common/FwUtils/FwUtils.csproj
git commit -m "Add icu.net PackageReference to FwUtils"
```

### Find Projects Needing a Package
```bash
# Use grep to find usage
grep -r "using Newtonsoft.Json" Src --include="*.cs" | \
  sed 's|/[^/]*\.cs:.*||' | sort -u

# Or use the script
python find_projects_using_namespace.py "Newtonsoft.Json"
```

## Troubleshooting

### Package Not Being Added?
```bash
# Check if it already exists
python add_package_reference.py --verbose --dry-run \
  icu.net "3.0.0-beta.297" Src/Common/FwUtils/FwUtils.csproj
```

### Package Not Being Found?
```bash
# Check if the namespace pattern is correct
python find_projects_using_namespace.py "Newtonsoft.Json" --show-files

# Try with additional patterns
python find_projects_using_namespace.py "Newtonsoft.Json" \
  --include-patterns "JObject" "JsonConvert"
```

### Build Fails After Adding Package?
1. Check if the version is correct
2. Make sure it's compatible with net48
3. Look for missing transitive dependencies
4. Check the Output/ folder to ensure DLLs are copied

### Unused Package Detection Missing Usage?
The script looks for `using` statements and specific code patterns. If it misses usage:
- Add more patterns with `--include-patterns`
- Check for dynamic loading or reflection usage
- Some packages might be needed even without `using` statements

## Quick Reference Card

| Task | Command |
|------|---------|
| Find projects using namespace | `python find_projects_using_namespace.py "Namespace"` |
| Check if projects have package | `python find_projects_using_namespace.py "Namespace" --check-package "Package"` |
| Generate add command | `python find_projects_using_namespace.py "Namespace" --generate-command "Package" "Version"` |
| Add package | `python add_package_reference.py Package "Version" project.csproj` |
| Add to multiple projects | `python add_package_reference.py Package "Version" "Src/**/*.csproj"` |
| Add with attributes | `python add_package_reference.py --attr PrivateAssets=All Package "Version" project.csproj` |
| Find unused packages | `python find_projects_using_namespace.py "Namespace" --check-package "Package" --find-unused` |
| Generate remove command | `python find_projects_using_namespace.py "Namespace" --check-package "Package" --find-unused --generate-remove-command` |
| Remove package | `python remove_package_reference.py Package project.csproj` |
| Dry run | Add `--dry-run` to any command |
| Verbose output | Add `--verbose` or `-v` to any command |

## Documentation

- `DLL_MODERNIZATION_PLAN.md` - Complete modernization strategy and package inventory
- `ADD_PACKAGE_REFERENCE_README.md` - Detailed documentation for add_package_reference.py
- `python <script> --help` - Built-in help for each script

## Support

If you encounter issues:
1. Run with `--verbose` flag
2. Check the script's help: `python <script> --help`
3. Review the documentation files
4. Test with `--dry-run` first
5. Make incremental changes and commit frequently
