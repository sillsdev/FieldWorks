# Workflow Architecture Diagram

## How the Windows Build Works

```
┌─────────────────────────────────────────────────────────────────┐
│                   GitHub Actions Workflow                        │
│                                                                  │
│  ┌────────────────────────────────────────────────────────┐    │
│  │  example-windows-build.yml (or your workflow)          │    │
│  │                                                         │    │
│  │  jobs:                                                  │    │
│  │    build:                                               │    │
│  │      uses: ./.github/workflows/copilot-setup-steps.yml │    │
│  │      with:                                              │    │
│  │        msbuild_args: "FieldWorks.sln /m /p:Config=..." │    │
│  └────────────────────┬───────────────────────────────────┘    │
│                       │                                         │
│                       │ calls                                   │
│                       ▼                                         │
│  ┌────────────────────────────────────────────────────────┐    │
│  │  copilot-setup-steps.yml (reusable workflow)          │    │
│  │                                                         │    │
│  │  jobs:                                                  │    │
│  │    windows-setup:                                       │    │
│  │      runs-on: windows-latest  ◄─────────────────┐     │    │
│  │                                                   │     │    │
│  │      steps:                                       │     │    │
│  │        1. Checkout repo                          │     │    │
│  │        2. Find MSBuild (vswhere)                 │     │    │
│  │        3. Run msbuild with args                  │     │    │
│  │        4. Check exit code & report errors        │     │    │
│  └────────────────────────────────────────────────────────┘    │
│                                                                  │
└─────────────────────────────────────────────────────────────────┘
                                │
                                │ Spins up
                                ▼
┌─────────────────────────────────────────────────────────────────┐
│                   Windows Runner (GitHub-hosted)                 │
│                                                                  │
│  OS: Windows Server 2022                                        │
│  Has: Visual Studio 2022 Build Tools                            │
│  Has: MSBuild (found via vswhere)                               │
│  Has: .NET Framework 4.8                                        │
│                                                                  │
│  ┌─────────────────────────────────────────────────────────┐   │
│  │  MSBuild Process                                         │   │
│  │                                                          │   │
│  │  Command: msbuild FieldWorks.sln /m /p:Configuration=...│   │
│  │                                                          │   │
│  │  ┌─────────────────┐                                    │   │
│  │  │ Compile C# code │                                    │   │
│  │  └─────────────────┘                                    │   │
│  │  ┌─────────────────┐                                    │   │
│  │  │ Compile C++ code│                                    │   │
│  │  └─────────────────┘                                    │   │
│  │  ┌─────────────────┐                                    │   │
│  │  │ Link binaries   │                                    │   │
│  │  └─────────────────┘                                    │   │
│  │  ┌─────────────────┐                                    │   │
│  │  │ Output results  │                                    │   │
│  │  └─────────────────┘                                    │   │
│  │                                                          │   │
│  │  Exit Code: 0 = Success, Non-zero = Error              │   │
│  └─────────────────────────────────────────────────────────┘   │
│                                                                  │
└─────────────────────────────────────────────────────────────────┘
                                │
                                │ Reports back
                                ▼
┌─────────────────────────────────────────────────────────────────┐
│                      Workflow Results                            │
│                                                                  │
│  ✅ Success: "Build completed successfully!" (Green)            │
│  ❌ Failure: "MSBuild failed with exit code N" (Red)            │
│                                                                  │
│  View in: GitHub Actions → Workflows → Your workflow            │
└─────────────────────────────────────────────────────────────────┘
```

## Key Points

### 1. You Don't Need a Windows Machine Locally
- GitHub Actions provides Windows runners automatically
- Just push your workflow, and GitHub handles the rest

### 2. The Workflow is Already Configured
- `copilot-setup-steps.yml` has `runs-on: windows-latest`
- This ensures a Windows machine spins up
- No additional configuration needed!

### 3. Error Detection is Automatic
- The workflow checks MSBuild's exit code
- If `$LASTEXITCODE -ne 0`, the job fails
- Error messages are displayed in the workflow log

### 4. How to Use It

**Option A: Use the example workflow**
```bash
# The workflow is already committed
# Just trigger it manually in GitHub Actions UI
```

**Option B: Add to your existing workflow**
```yaml
jobs:
  my-build:
    uses: ./.github/workflows/copilot-setup-steps.yml
    with:
      msbuild_args: "FieldWorks.sln /m /p:Configuration=Debug"
```

### 5. What Happens When You Run It

1. GitHub receives workflow trigger
2. GitHub spins up a fresh Windows Server 2022 VM
3. VM has Visual Studio 2022 pre-installed
4. Workflow checks out your code
5. Workflow finds MSBuild using vswhere
6. Workflow runs: `msbuild FieldWorks.sln /m /p:Configuration=Debug`
7. Workflow checks exit code
8. If successful: Green checkmark + success message
9. If failed: Red X + error message with exit code

### 6. No Local Windows Needed!
You asked: "If the machine is not windows that spins up, then tell me how I can get it"

✅ **Answer**: The workflow guarantees a Windows machine will spin up because of `runs-on: windows-latest`

If you're on Linux locally (like you are now), you can:
- Use GitHub Actions (recommended) ✅
- Get a Windows VM (Azure, AWS, etc.)
- Use a self-hosted Windows runner
- Install Windows locally

See `WINDOWS_BUILD_GUIDE.md` for detailed options.
