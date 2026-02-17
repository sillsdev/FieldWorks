# Dependencies on Other Repositories

FieldWorks depends on several external libraries and related repositories. This document describes those dependencies and how to work with them.

## Overview

Most dependencies are automatically downloaded as NuGet packages during the build process. However, if you need to debug into or modify these libraries, you may need to build them locally.

## Primary Dependencies

### Core Libraries

| Repository | Purpose | Branch |
|------------|---------|--------|
| [sillsdev/liblcm](https://github.com/sillsdev/liblcm) | Language/Culture Model - data access layer | `master` |
| [sillsdev/libpalaso](https://github.com/sillsdev/libpalaso) | SIL shared utilities | `master` |
| [sillsdev/chorus](https://github.com/sillsdev/chorus) | Version control for linguistic data | `master` |

### Related Projects

| Repository | Purpose | Notes |
|------------|---------|-------|
| [sillsdev/FwLocalizations](https://github.com/sillsdev/FwLocalizations) | Translations (Crowdin integration) | Localization workflow |

## Default Dependency Source

By default, dependencies are downloaded as NuGet packages during the build. The version numbers are specified in `Build/mkall.targets`:

```xml
<ChorusNugetVersion>...</ChorusNugetVersion>
<PalasoNugetVersion>...</PalasoNugetVersion>
<LcmNugetVersion>...</LcmNugetVersion>
```

## Building Dependencies Locally

If you need to debug into or modify a dependency library, you can build it locally.

### Step 1: Clone the Repositories

```bash
# Clone to any location
git clone https://github.com/sillsdev/liblcm.git
git clone https://github.com/sillsdev/libpalaso.git
git clone https://github.com/sillsdev/chorus.git
```

### Step 2: Set Up Local NuGet Repository

1. **Create a local NuGet folder** (e.g., `C:\localnugetpackages`)

2. **Add as NuGet source in Visual Studio**:
   - Tools → Options → NuGet Package Manager → Package Sources
   - Add your local folder

3. **Set environment variable**:
   ```powershell
   $env:LOCAL_NUGET_REPO = "C:\localnugetpackages"
   # Add to your profile for persistence
   ```

4. **Add the CopyPackage target** to each dependency's `Directory.Build.targets`:
   ```xml
   <Target Name="CopyPackage" AfterTargets="Pack"
           Condition="'$(LOCAL_NUGET_REPO)'!='' AND '$(IsPackable)'=='true'">
     <Copy SourceFiles="$(PackageOutputPath)/$(PackageId).$(PackageVersion).nupkg"
           DestinationFolder="$(LOCAL_NUGET_REPO)"/>
   </Target>
   ```

### Step 3: Build in Order

Dependencies must be built in a specific order:

1. **libpalaso** (no dependencies on other SIL libraries)
2. **chorus** and **liblcm** (depend on libpalaso)
3. **FieldWorks** (depends on all of the above)

For each library:

```bash
# Create a local branch for versioning
git checkout -b localcommit

# Make a small change to bump version (e.g., edit README.md)
git commit -am "Local build version bump"

# Build
dotnet build

# Pack and publish to local repo
dotnet pack
```

### Step 4: Update FieldWorks

Update the NuGet versions in FieldWorks to use your local packages:

1. Clear cached packages:
   - `~\.nuget\packages\` (user cache)
   - `packages\` (solution packages)
   - Your local NuGet folder

2. Update version numbers in `Build/mkall.targets`

3. Build FieldWorks

## Debugging Dependencies

To debug into dependency code:

1. Build the dependency in Debug configuration
2. Open the dependency project in Visual Studio alongside FieldWorks
3. Start debugging FLEx
4. Choose **Debug → Attach to Process** from the dependency project
5. If breakpoints show "No symbols loaded", disable **Debug → Options → Enable Just My Code**

## Dependency Configuration

Build dependency information is also available in:
- `Build/Agent/dependencies.config`
- `Build/mkall.targets` (target `CopyDlls`)

## GitHub Actions Integration

FieldWorks uses GitHub Actions for CI/CD. The workflow files are in `.github/workflows/`.

Dependencies are restored automatically from NuGet during CI builds.

## See Also

- [Data Migrations](data-migrations.md) - Working with the data model
- [Build Instructions](../../.github/instructions/build.instructions.md) - Building FieldWorks
- [CONTRIBUTING.md](../CONTRIBUTING.md) - Getting started
