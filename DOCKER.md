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
- **WiX Toolset 3.11.x** (for installer creation)
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
