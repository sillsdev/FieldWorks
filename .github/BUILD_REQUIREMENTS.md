# Build Requirements

## Local Development

### Full Build (C# + Native C++)

To build the complete FieldWorks solution including native C++ components:

**PowerShell (Recommended - auto-initializes VS environment):**
```powershell
.\build.ps1
```

**Bash (Git Bash - requires Developer Command Prompt):**
```bash
# 1. Open "Developer Command Prompt for VS 2022" from Start Menu
# 2. Type: bash
# 3. Navigate to repo: cd /c/path/to/FieldWorks
# 4. Run: ./build.sh
```

**Why?** Native components (DebugProcs, GenericLib, FwKernel, Views, graphite2) require:
- `nmake.exe` (from Visual Studio C++ Build Tools)
- C++ compiler toolchain
- Environment variables set by VsDevCmd.bat (VCINSTALLDIR, INCLUDE, LIB, etc.)

**Note:** The PowerShell script (`build.ps1`) automatically initializes the Visual Studio environment using `vswhere.exe`. The Bash script requires you to run from a Developer Command Prompt because Git Bash has issues reliably calling VsDevCmd.bat.

### Managed-Only Build (C# projects)

If you only need to build C# projects and already have native artifacts from a previous build:

```powershell
# Build only managed projects (skips native C++)
msbuild FieldWorks.proj /p:Configuration=Debug /p:Platform=x64
```

## CI Builds

GitHub Actions CI automatically configures the Developer environment using the `microsoft/setup-msbuild@v2` action. No manual setup is required.

From `.github/workflows/CI.yml`:
```yaml
- name: Setup MSBuild
  uses: microsoft/setup-msbuild@v2  # Configures VS environment automatically

- name: Build Debug and run tests
  run: ./build.ps1 -Configuration Debug -Platform x64
```

## Troubleshooting

### Error: "nmake.exe could not be run" or "VCINSTALLDIR not set"

**Cause:** Build script was run from a regular PowerShell/bash session instead of a Developer Command Prompt.

**Solution:**
1. Close your current terminal
2. Open "Developer Command Prompt for VS 2022" or "Developer PowerShell for VS 2022" from the Start Menu
3. Navigate to the repository
4. Run the build script again

### Error: "Missing FieldWorks build tasks assembly"

**Cause:** FwBuildTasks.dll hasn't been built yet (typically on first build or after clean).

**Solution:** The build scripts now automatically bootstrap FwBuildTasks. If this fails, manually build it first:
```powershell
msbuild Build/Src/FwBuildTasks/FwBuildTasks.csproj /t:Restore;Build /p:Configuration=Debug
```

## Build Script Features

Both `build.ps1` and `build.sh` now include:

1. **Automatic FwBuildTasks bootstrap**: Builds build infrastructure before main build
2. **Environment validation**: Warns if Developer environment is not detected
3. **Package restoration**: Restores NuGet packages before build
4. **Traversal build**: Uses MSBuild Traversal SDK (FieldWorks.proj) for correct dependency ordering

## Visual Studio Requirements

- **Visual Studio 2022** (Community, Professional, or Enterprise)
- **Required Workloads:**
  - .NET desktop development
  - Desktop development with C++
- **Optional:** WiX Toolset 3.11.x (only for installer builds)
