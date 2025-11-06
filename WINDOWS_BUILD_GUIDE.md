# Windows Build Guide for FieldWorks

## Overview
FieldWorks requires Windows and MSBuild (.NET Framework 4.8) to build. This guide explains how to get a Windows environment and build the software.

## Current Environment Check
If you're running on Linux (as indicated by running `uname -a`), you **cannot** build FieldWorks natively. You need a Windows machine.

## How to Get a Windows Machine

### Option 1: GitHub Actions (Recommended for CI/CD)
The repository already has workflows configured to run on Windows:

1. **Using the copilot-setup-steps.yml workflow** (for reusable builds):
   ```yaml
   jobs:
     build:
       uses: ./.github/workflows/copilot-setup-steps.yml
       with:
         msbuild_args: "FieldWorks.sln /m /p:Configuration=Debug"
   ```

2. **Existing CI workflow**: The `CI.yml` workflow already runs on `windows-latest`

3. **Example workflow**: See `.github/workflows/example-windows-build.yml` for a complete example

### Option 2: Local Windows Machine
To build locally on Windows:

1. **Prerequisites**:
   - Windows 10 or later
   - Visual Studio 2022 with:
     - .NET desktop development workload
     - Desktop development with C++ workload
   - .NET Framework 4.8 Developer Pack
   - MSBuild (included with Visual Studio)

2. **Build Steps**:
   ```cmd
   # Open Developer Command Prompt for VS 2022
   cd Build
   build64.bat /t:remakefw-jenkins /p:action=test
   ```

   Or directly with MSBuild:
   ```cmd
   msbuild FieldWorks.sln /m /p:Configuration=Debug /p:Platform=x64
   ```

### Option 3: Self-Hosted Windows Runner
For GitHub Actions with your own hardware:

1. Set up a self-hosted Windows runner: https://docs.github.com/en/actions/hosting-your-own-runners
2. Tag it with `windows` label
3. Modify workflow to use: `runs-on: [self-hosted, windows]`

### Option 4: Cloud Windows VM
Use cloud providers:
- **Azure**: Windows Server VM
- **AWS**: Windows EC2 instance
- **DigitalOcean**: Windows Droplet

## Build Commands

### Basic Build
```cmd
msbuild FieldWorks.sln /m /p:Configuration=Debug
```

### Build with Multiple CPUs
```cmd
msbuild FieldWorks.sln /m /p:Configuration=Debug /maxcpucount:4
```

### Build and Test
```cmd
cd Build
build64.bat /t:remakefw-jenkins /p:action=test /p:desktopNotAvailable=true
```

## Checking for Build Errors

After building, check for errors:

```powershell
# In PowerShell
Select-String -Path "Build/build.log" -Pattern "^\s*[1-9][0-9]* Error\(s\)"
```

Or manually inspect the build output for lines containing "error" or "failed".

## Testing the Build

Run NUnit tests:
```cmd
cd Build
NUnitReport.exe /a
```

## Common Issues

### Issue: "MSBuild not found"
**Solution**: Install Visual Studio with MSBuild, or use the Developer Command Prompt

### Issue: ".NET Framework 4.8 not found"
**Solution**: Install the .NET Framework 4.8 Developer Pack from Microsoft

### Issue: "Cannot run on Linux"
**Solution**: FieldWorks is a Windows-only project. Use one of the options above to get a Windows environment.

## GitHub Actions Configuration

The repository provides `copilot-setup-steps.yml` which:
1. Automatically runs on `windows-latest`
2. Finds MSBuild using vswhere
3. Executes msbuild with your specified arguments
4. Reports build success or failure

See `.github/workflows/example-windows-build.yml` for usage.

## References

- Developer Documentation: https://github.com/sillsdev/FwDocumentation/wiki
- Build Instructions: `.github/instructions/build.instructions.md`
- CI Workflow: `.github/workflows/CI.yml`
- Copilot Setup: `.github/workflows/copilot-setup-steps.yml`
