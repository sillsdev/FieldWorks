# add_package_reference.py - Efficiently Add NuGet PackageReferences

A Python script to add PackageReference entries to .csproj files efficiently, with support for glob patterns and batch operations.

## Features

- **Smart Detection**: Only adds packages that don't already exist
- **Alphabetical Sorting**: Maintains sorted order within ItemGroups
- **Glob Pattern Support**: Process multiple projects at once
- **Dry Run Mode**: Preview changes before applying them
- **Custom Attributes**: Support for PrivateAssets, ExcludeAssets, etc.
- **Namespace Aware**: Works with both namespaced and non-namespaced project files

## Usage

### Basic Usage

Add a single package to a single project:

```bash
python add_package_reference.py <package_name> <version> <project.csproj>
```

**Example:**
```bash
python add_package_reference.py icu.net "3.0.0-beta.297" Src/Common/FwUtils/FwUtils.csproj
```

### Multiple Projects

Add a package to multiple projects at once:

```bash
python add_package_reference.py Moq "4.17.2" \
  Src/Common/Controls/XMLViews/XMLViewsTests/XMLViewsTests.csproj \
  Src/Common/Framework/FrameworkTests/FrameworkTests.csproj \
  Src/Common/RootSite/RootSiteTests/RootSiteTests.csproj
```

### Glob Patterns

Use glob patterns to match multiple projects:

```bash
# Add Moq to all test projects
python add_package_reference.py Moq "4.17.2" "Src/**/*Tests/*.csproj"

# Add to all projects in a specific folder
python add_package_reference.py Autofac "4.9.4" "Src/LexText/*/*.csproj"

# Add to specific pattern of projects
python add_package_reference.py Newtonsoft.Json "13.0.3" "Src/*/xWorks/*.csproj"
```

### Custom Attributes

Add packages with additional attributes like `PrivateAssets`:

```bash
# Add test packages with PrivateAssets=All (prevents test dependencies from being copied)
python add_package_reference.py --attr PrivateAssets=All \
  SIL.LCModel.Tests "11.0.0-*" \
  "Src/**/*Tests/*.csproj"

# Multiple attributes
python add_package_reference.py \
  --attr PrivateAssets=All \
  --attr ExcludeAssets=Runtime \
  SIL.TestUtilities "17.0.0-*" \
  "Src/**/*Tests.csproj"
```

### Dry Run

Preview what would be added without making changes:

```bash
python add_package_reference.py --dry-run --verbose \
  icu.net "3.0.0-beta.297" \
  "Src/Common/**/*.csproj"
```

**Output:**
```
Found 42 project file(s)
Package: icu.net v3.0.0-beta.297

[DRY RUN] Would add 'icu.net' to Src/Common/Controls/FwControls/FwControls.csproj
[DRY RUN] Would skip Src/Common/FwUtils/FwUtils.csproj (already has icu.net)
...
Summary: 35 added, 7 skipped, 0 errors
```

### Verbose Mode

Get detailed information about what's happening:

```bash
python add_package_reference.py --verbose \
  Newtonsoft.Json "13.0.3" \
  Src/Common/FwUtils/FwUtils.csproj
```

## Common Use Cases

### Add Standard .NET Packages

```bash
# Add System packages (usually these are transitive, but sometimes needed explicitly)
python add_package_reference.py System.Drawing.Common "9.0.0" "Src/Common/Controls/**/*.csproj"
python add_package_reference.py System.Resources.Extensions "8.0.0" "Src/**/*.csproj"

# Add common utilities
python add_package_reference.py Newtonsoft.Json "13.0.3" "Src/xWorks/*.csproj"
python add_package_reference.py DocumentFormat.OpenXml "2.20.0" "Src/xWorks/*.csproj"
```

### Add Test Packages

```bash
# Add Moq to all test projects
python add_package_reference.py Moq "4.17.2" "Src/**/*Tests/*.csproj"

# Add test utilities with PrivateAssets
python add_package_reference.py --attr PrivateAssets=All \
  SIL.TestUtilities "17.0.0-*" \
  "Src/**/*Tests/*.csproj"
```

### Add SIL Packages

```bash
# Add commonly used SIL packages
python add_package_reference.py SIL.Lexicon "17.0.0-*" "Src/LexText/**/*.csproj"
python add_package_reference.py SIL.Scripture "17.0.0-*" "Src/LexText/**/*.csproj"
python add_package_reference.py SIL.Media "17.0.0-*" "Src/Common/Controls/**/*.csproj"

# Add with wildcard versions for beta packages
python add_package_reference.py L10NSharp "9.0.0-*" "Src/**/*.csproj"
```

## Command Line Options

| Option | Description |
|--------|-------------|
| `package` | Package name (e.g., `icu.net`) |
| `version` | Package version (e.g., `3.0.0-beta.297` or `17.0.0-*`) |
| `projects` | One or more project files or glob patterns |
| `--attr KEY=VALUE` | Add custom attribute (can be used multiple times) |
| `--dry-run` | Preview changes without modifying files |
| `-v, --verbose` | Show detailed output |
| `-h, --help` | Show help message |

## How It Works

1. **Parses** each .csproj file as XML
2. **Checks** if the package already exists (case-insensitive)
3. **Finds** the appropriate `<ItemGroup>` with existing PackageReferences
4. **Inserts** the new PackageReference in alphabetical order
5. **Preserves** existing formatting and indentation
6. **Writes** the modified file back (only if changes were made)

## Exit Codes

- `0`: Success (packages added or skipped)
- `1`: Error occurred (parsing failure, invalid arguments, etc.)

## Tips

### Check Before Committing

Always use `--dry-run` first to verify what will be changed:

```bash
# Check what would be added
python add_package_reference.py --dry-run icu.net "3.0.0-beta.297" "Src/**/*.csproj"

# If it looks good, run without --dry-run
python add_package_reference.py icu.net "3.0.0-beta.297" "Src/**/*.csproj"
```

### Find Projects That Need a Package

Use grep to find which projects use a package:

```bash
# Find projects using Newtonsoft.Json
grep -r "using Newtonsoft.Json\|JObject\|JsonConvert" Src --include="*.cs" | \
  sed 's|/[^/]*\.cs:.*||' | sort -u

# Then add the package to those projects
python add_package_reference.py Newtonsoft.Json "13.0.3" "Src/path/to/*.csproj"
```

### Quote Glob Patterns

Always quote glob patterns to prevent shell expansion:

```bash
# Good - quoted
python add_package_reference.py Moq "4.17.2" "Src/**/*Tests/*.csproj"

# Bad - unquoted (shell expands before script sees it)
python add_package_reference.py Moq "4.17.2" Src/**/*Tests/*.csproj
```

## Troubleshooting

### Package Not Added

If a package isn't added, run with `--verbose` to see why:

```bash
python add_package_reference.py --verbose icu.net "3.0.0-beta.297" Src/Common/FwUtils/FwUtils.csproj
```

Common reasons:
- Package already exists (shown as "skipped")
- File parsing error
- Project file not found

### Formatting Issues

The script tries to match existing indentation. If formatting looks off:

1. Check the original file's indentation (tabs vs spaces)
2. The script uses 4 spaces by default
3. Consider reformatting with your IDE after bulk changes

### Glob Pattern Not Matching

If glob patterns don't match expected files:

```bash
# Test the pattern with find
find Src -name "*Tests.csproj"

# Or with ls
ls Src/**/*Tests/*.csproj
```

## Integration with DLL Modernization Plan

This script is part of the DLL modernization effort documented in `DLL_MODERNIZATION_PLAN.md`. It helps automate:

- **Phase 1**: Adding standard .NET packages (System.*, Newtonsoft.Json, etc.)
- **Phase 2**: Completing SIL package references
- **Phase 3**: Adding third-party managed packages

See `DLL_MODERNIZATION_PLAN.md` for the complete modernization strategy.

## Examples from the Modernization Plan

### Phase 1: Standard .NET Packages

```bash
# Add icu.net to projects that use ICU functionality
python add_package_reference.py icu.net "3.0.0-beta.297" \
  Src/Common/FwUtils/FwUtils.csproj \
  Src/Common/Controls/FwControls/FwControls.csproj \
  Src/Common/Controls/Widgets/Widgets.csproj

# Add Moq to test projects
python add_package_reference.py Moq "4.17.2" "Src/**/*Tests/*.csproj"

# Add mixpanel-csharp for analytics
python add_package_reference.py mixpanel-csharp "6.0.0" Src/Common/FieldWorks/FieldWorks.csproj
```

### Phase 2: SIL Packages

```bash
# Add SIL.Lexicon where needed
python add_package_reference.py SIL.Lexicon "17.0.0-*" \
  "Src/LexText/**/*.csproj"

# Add SIL.Scripture
python add_package_reference.py SIL.Scripture "17.0.0-*" \
  "Src/LexText/**/*.csproj"

# Add SIL.Media
python add_package_reference.py SIL.Media "17.0.0-*" \
  "Src/Common/Controls/**/*.csproj"
```

### Phase 3: Third-Party Packages

```bash
# Add L10NSharp
python add_package_reference.py L10NSharp "9.0.0-*" \
  Src/Common/FieldWorks/FieldWorks.csproj

# Add Enchant.Net
python add_package_reference.py Enchant.Net "1.4.3-beta0010" \
  "Src/**/*.csproj"
```

## License

This script is part of the FieldWorks project and follows the same license.
