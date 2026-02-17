# Set Up Visual Studio for FieldWorks Development on Windows

This guide covers the Visual Studio 2022 setup required for FieldWorks development.

> `$FWROOT` in this document refers to the root directory of the FieldWorks source tree (where you cloned the repository).

## Install Visual Studio 2022

1. **Download Visual Studio 2022 Community Edition** (or Professional/Enterprise):
   - Go to [https://visualstudio.microsoft.com/vs/](https://visualstudio.microsoft.com/vs/)
   - Download and run the installer

2. **Select the following Workloads:**
   - ✅ **.NET desktop development**
   - ✅ **Desktop development with C++**

3. **Select the following Individual Components:**
   - ✅ C++ ATL for latest v143 build tools (x86 & x64)
   - ✅ C++ MFC for latest v143 build tools (x86 & x64)
   - ✅ Windows 11 SDK (10.0.22621.0)
   - ✅ .NET Framework 4.8.1 SDK
   - ✅ .NET Framework 4.8.1 targeting pack

## Configure Visual Studio Settings

### Set "Keep tabs" in Text Editor

This is required to match our coding standards:

1. Go to **Tools → Options**
2. Navigate to **Text Editor → All Languages → Tabs**
3. Select the **"Keep tabs"** radio option
4. Verify Tab size and Indent size are both **4**

### Using Shared Settings for ReSharper (Optional)

If you have ReSharper installed:

The shared settings file is located at `$FWROOT/FW.sln.DotSettings`. If you save your solution as `$FWROOT/FieldWorks.sln`, the shared settings will automatically be picked up.

Alternatively:
- Copy the file and give it the same name as your solution with a `.sln.DotSettings` extension
- Or import settings via **ReSharper → Manage Options → Import/Export Settings → Import from file**

> **Tip**: Using the shared settings file directly has the advantage that ReSharper will pick up any changes made to the shared settings.

## Open the FieldWorks Solution

Open the main solution file:

```
$FWROOT\FieldWorks.sln
```

This solution contains all the FieldWorks projects organized for development.

## Building from Visual Studio

### Option 1: Use External Tools (Recommended)

Visual Studio may have difficulty building all projects in the correct order. For reliable builds, add the build script as an External Tool:

1. Click **Tools → External Tools...**
2. Click **Add** and configure:

**Build FW (Full):**
```
Title:             Build FW Full
Command:           powershell.exe
Arguments:         -ExecutionPolicy Bypass -File .\build.ps1
Initial directory: $(SolutionDir)
```
Select ✅ "Use Output window"

**Build FW (Release):**
```
Title:             Build FW Release
Command:           powershell.exe
Arguments:         -ExecutionPolicy Bypass -File .\build.ps1 -Configuration Release
Initial directory: $(SolutionDir)
```
Select ✅ "Use Output window"

### Option 2: Command Line Build

Build from PowerShell before opening Visual Studio:

```powershell
.\build.ps1
```

Then use Visual Studio for editing and debugging.

## Debugging FieldWorks

1. Set **FieldWorks** (or the specific project you're working on) as the **Startup Project**
2. Ensure the configuration is **Debug** and platform is **x64**
3. Press **F5** to start debugging

## Running Tests

### Using Test Explorer

1. Open **Test → Test Explorer**
2. Build the solution to discover tests
3. Run individual tests or all tests from the Test Explorer

### Using External Tools

Add a test tool:
```
Title:             Run Tests
Command:           powershell.exe
Arguments:         -ExecutionPolicy Bypass -File .\build.ps1 -MsBuildArgs @('/p:action=test')
Initial directory: $(SolutionDir)
```

## Troubleshooting

### .NET Framework 2.0 Required (Legacy Branches)

Some legacy branches may require .NET Framework 2.0. To install:

1. Launch **"Turn Windows features on or off"**
2. Select **".NET Framework 3.5 (includes .NET 2.0 and 3.0)"** and click OK

### Native Build Failures

If native C++ components fail to build:

1. Ensure you have the C++ workload installed
2. Try building from the command line first:
   ```powershell
   msbuild Build\Src\NativeBuild\NativeBuild.csproj /p:Configuration=Debug /p:Platform=x64
   ```

### Solution Won't Load All Projects

This is expected - Visual Studio may not load all 100+ projects correctly. Use the command-line build for full builds, and Visual Studio for editing and debugging specific projects.

## See Also

- [CONTRIBUTING.md](CONTRIBUTING.md) - Getting started guide
- [Build Instructions](../.github/instructions/build.instructions.md) - Detailed build system documentation
- [Coding Standards](../.github/instructions/coding-standard.instructions.md) - Code style guidelines
