# FieldWorks Docker Build Environment

This directory contains the Dockerfile and supporting files to create a Windows Docker image with all dependencies required to build FieldWorks.

## Files

- **Dockerfile.windows** - Multi-stage Dockerfile that installs Visual Studio Build Tools, .NET SDKs, WiX Toolset, and other build dependencies
- **Post-Install-Setup.ps1** - PowerShell script that configures paths, environment variables, and creates necessary directory structures
- **VsDevShell.cmd** - Batch script that initializes the Visual Studio build environment

## Pre-built Images

Pre-built Docker images are automatically published to GitHub Container Registry whenever the Dockerfile changes on the `release/9.3` branch.

### Pulling the Image

```powershell
# Pull the latest version
docker pull ghcr.io/sillsdev/fieldworks/fieldworks-build:latest

# Pull a specific version (e.g., version 5)
docker pull ghcr.io/sillsdev/fieldworks/fieldworks-build:5
```

### Authentication

To pull images from GitHub Container Registry, you need to authenticate:

```powershell
# Using a GitHub Personal Access Token (PAT)
$env:CR_PAT = "your_github_pat_here"
echo $env:CR_PAT | docker login ghcr.io -u YOUR_GITHUB_USERNAME --password-stdin
```

Or in GitHub Actions workflows, authentication is automatic using `GITHUB_TOKEN`.

## Using in GitHub Actions

To use the pre-built Docker image in your workflows, add a container specification:

```yaml
jobs:
  build:
    runs-on: windows-latest
    container:
      image: ghcr.io/sillsdev/fieldworks/fieldworks-build:latest
      credentials:
        username: ${{ github.actor }}
        password: ${{ secrets.GITHUB_TOKEN }}

    steps:
      - uses: actions/checkout@v4

      - name: Build FieldWorks
        run: |
          cd Build
          .\build64.bat /t:remakefw
```

## Building Locally

If you need to build the Docker image locally:

```powershell
# Build the image
docker build -t fieldworks-build:local -f Dockerfile.windows .

# Run a container
docker run -it fieldworks-build:local

# Run with a mounted volume (to access your code)
docker run -it -v ${PWD}:C:\workspace fieldworks-build:local
```

## Image Contents

The Docker image includes:

- **Windows Server Core LTSC 2022** base image
- **.NET Framework 4.8 SDK**
- **Visual Studio 2022 Build Tools** with:
  - ManagedDesktopBuildTools workload
  - VCTools workload
  - VC.ATLMFC component
  - VC.Tools.x86.x64 component
  - .NET 4.8.1 SDK and Targeting Pack
  - Windows 11 SDK 22621
- **.NET 8 SDK** (for modern dotnet operations)
- **WiX Toolset 3.14.1** (for installer creation)
- **NuGet CLI**
- **MSBuild** (via Build Tools)

## Image Size

The Windows container image is approximately **10-12 GB** due to Visual Studio Build Tools and Windows Server Core base image requirements.

## Versioning

Images are tagged with:
- `latest` - The most recent build from the primary branch
- `<number>` - Incrementing version number based on GitHub Actions run number

## Maintenance

The Docker image is automatically rebuilt when:
- `Dockerfile.windows` is modified on `release/9.3` branch
- `Post-Install-Setup.ps1` is modified on `release/9.3` branch
- `VsDevShell.cmd` is modified on `release/9.3` branch

The build workflow can also be manually triggered via GitHub Actions UI.

## Worktree Build Isolation

When using Docker containers with git worktrees (e.g., `fw-agent-N` containers), the worktree directory is bind-mounted into the container. This creates potential conflicts between:

- **Host builds**: IDE services like Serena/OmniSharp, main repo builds
- **Container builds**: Worktree builds running inside Docker

### Container-Local Build Output

To avoid file locking conflicts, container builds use **container-local paths** for intermediate and output files:

| Artifact | Host Path | Container Path |
|----------|-----------|----------------|
| FwBuildTasks.dll | `BuildTools/FwBuildTasks/<Config>/` | `C:\Temp\BuildTools\FwBuildTasks\<Config>\` |
| Intermediate files | `Obj/<Project>/` | `C:\Temp\Obj/<Project>/` |
| NuGet packages | `packages/` | `C:\NuGetCache\packages\` (named volume) |

This isolation is automatic:
- `DOTNET_RUNNING_IN_CONTAINER` controls intermediate output paths
- `NUGET_PACKAGES` environment variable controls NuGet package location
- Containers use `--isolation=hyperv` to fix Windows Docker MoveFile bug

### NuGet Cache: Hybrid Architecture

Container builds use a **hybrid caching strategy** that separates shared and isolated caches:

```
┌─────────────────────────────────────────────────────────────────┐
│ SHARED (Docker Named Volume: fw-nuget-cache @ C:\NuGetCache)    │
│   ├── packages/      - global-packages (download once, shared)  │
│   └── http-cache/    - HTTP cache (feed metadata, shared)       │
└─────────────────────────────────────────────────────────────────┘
                              │
        ┌─────────────────────┼─────────────────────┐
        │                     │                     │
        ▼                     ▼                     ▼
┌───────────────┐    ┌───────────────┐    ┌───────────────┐
│  fw-agent-1   │    │  fw-agent-2   │    │  fw-agent-3   │
│  C:\Temp\     │    │  C:\Temp\     │    │  C:\Temp\     │
│  NuGetScratch │    │  NuGetScratch │    │  NuGetScratch │
│  (isolated)   │    │  (isolated)   │    │  (isolated)   │
└───────────────┘    └───────────────┘    └───────────────┘
```

**Why hybrid?**
| Cache | Shared? | Reason |
|-------|---------|--------|
| global-packages | ✅ YES | Read-only after extraction, ~2-3GB saved vs duplicating |
| http-cache | ✅ YES | Cached HTTP responses, rarely written |
| temp (NuGetScratch) | ❌ NO | Active file operations during extraction, must be isolated |

**Why Hyper-V isolation?** Windows Docker has a known bug ([moby/moby#38256](https://github.com/moby/moby/issues/38256)) where `MoveFile()` operations fail with process isolation. NuGet uses `MoveFile()` to atomically move packages from temp to global-packages. **Hyper-V isolation fixes this bug** with negligible performance impact (~1 second container startup difference).

**Parallel build safety:**
- Different package versions: ✅ Safe (each version gets its own subfolder)
- Same version, staggered timing: ✅ Safe (second agent finds cached package)
- Same version, simultaneous: ⚠️ Low risk (Hyper-V + filesystem atomicity usually handles it)

**First-run behavior:**
If the shared volume is empty, the first restore in any container downloads all packages.
Subsequent containers/builds find packages already in the shared cache.

**Managing the cache:**
```powershell
# View volume info
docker volume inspect fw-nuget-cache

# Remove volume (forces full re-download on next build)
.\scripts\tear-down-agents.ps1 -RepoRoot "C:\dev\FieldWorks" -RemoveNuGetVolume

# Seed cache from host packages (faster than fresh download)
docker exec fw-agent-1 powershell -Command "Copy-Item -Path 'C:\fw-mounts\C\...\packages\*' -Destination 'C:\NuGetCache\packages\' -Recurse -Force"
```

### How It Works

1. MSBuild `.targets` files check for `DOTNET_RUNNING_IN_CONTAINER`
2. Container builds output to `C:\Temp\` (not bind-mounted)
3. Host services continue accessing files on the bind mount without conflicts
4. Both host and container can build simultaneously

### Files with Container-Aware Paths

- `Directory.Build.props` - intermediate output paths
- `Build/FwBuildTasks.targets` - build task output paths
- `Build/Src/NativeBuild/NativeBuild.csproj` - respects NUGET_PACKAGES env var
- `Build/mkall.targets` - PackagesDir respects NUGET_PACKAGES env var
- `Build/Localize.targets`
- `Build/RegFree.targets`

### Localization packages and worktrees

- Container/worktree builds intentionally skip copying `sil.chorus.l10ns` and `sil.libpalaso.l10ns` content (only host release builds need those artifacts). The copy step is gated by `DOTNET_RUNNING_IN_CONTAINER` to avoid unnecessary cache seeding in agents. Host Release builds in the main repo should run with a fully restored `packages/` folder so localization files are included.
- Package version strategy: we standardized on the newer Palaso/Chorus line (e.g., `17.0.0-beta0089`) to match mkall/native copy expectations. Host builds must restore those versions in `Build/nuget-common/packages.config`; agent/worktree builds still skip localization copy, but share the same package versions for consistency.
- `Build/Windows.targets`
- `Build/SetupInclude.targets`
- `Build/Src/FwBuildTasks/FwBuildTasks.csproj`
- `Build/Src/FwBuildTasks/Directory.Build.props`
- `nuget.config` - documents package cache policy

## Troubleshooting

### Container startup is slow
Windows containers take longer to start than Linux containers (typically 30-60 seconds). This is normal.

### Docker Desktop settings
Ensure Docker Desktop is configured to run Windows containers:
- Right-click Docker Desktop tray icon
- Select "Switch to Windows containers..."

### Build failures
If the Docker build fails, check:
1. Sufficient disk space (at least 25 GB free)
2. Docker Desktop has enough memory allocated (8 GB minimum recommended)
3. Network connectivity for downloading VS Build Tools and other dependencies

## References

- [Docker documentation for Windows containers](https://learn.microsoft.com/en-us/virtualization/windowscontainers/)
- [Visual Studio Build Tools](https://visualstudio.microsoft.com/downloads/#build-tools-for-visual-studio-2022)
- [GitHub Container Registry](https://docs.github.com/en/packages/working-with-a-github-packages-registry/working-with-the-container-registry)
