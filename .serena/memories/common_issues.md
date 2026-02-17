# Common Issues & Solutions

## Build Issues

### "Cannot generate Views.cs" / Missing Native Artifacts
**Symptom**: ViewsInterfaces fails during build
**Cause**: Native C++ components (Phase 2) haven't been built
**Solution**:
```powershell
msbuild Build\Src\NativeBuild\NativeBuild.csproj /p:Configuration=Debug /p:Platform=x64
.\build.ps1
```

### Random Parallel Build Failures
**Symptom**: Intermittent errors about missing assemblies
**Cause**: Race condition in parallel builds
**Solution**: Reduce parallelism or use traversal build
```powershell
.\build.ps1 -MsBuildArgs @('/m:1')
```

### Project X Can't Find Assembly from Project Y
**Symptom**: CS0234 or similar reference errors
**Cause**: Build order not respected
**Solution**: Use traversal build (`.\build.ps1`) which respects `FieldWorks.proj` phases

## Container/Worktree Issues

### Agent Worktree Build Fails on Host
**Symptom**: COM/registry errors when building `worktrees\agent-N` paths
**Cause**: Agent worktrees require containerized builds
**Solution**:
```powershell
docker exec fw-agent-N powershell -NoProfile -c "msbuild FieldWorks.sln /m /p:Configuration=Debug"
```

### Container Can't Access NuGet Packages
**Symptom**: Package restore fails inside container
**Cause**: Volume mounts or network issues
**Solution**: Check `scripts/spin-up-agents.ps1` for proper mount configuration

## Test Issues

### Tests Fail with "SIL.FieldWorks" Assembly Errors
**Symptom**: NUnit tests can't load required assemblies
**Cause**: Output directory not properly populated
**Solution**: Run full traversal build before testing

### Integration Tests Require Specific Data
**Symptom**: Tests fail looking for project files
**Cause**: Test data not in expected location
**Solution**: Check `TestLangProj/` and `DistFiles/Projects/` for required data

## Documentation Issues

### COPILOT.md Validation Fails
**Symptom**: `check_copilot_docs.py` reports missing sections
**Cause**: Document doesn't match expected template
**Solution**:
```powershell
python .github/scaffold_copilot_markdown.py --folders Src/<Folder>
python .github/check_copilot_docs.py --paths Src/<Folder>/COPILOT.md
```

## Environment Issues

### Serena Language Server Not Initializing
**Symptom**: `mcp_oraios_serena_get_symbols_overview` returns "language server manager is not initialized"
**Cause**: Language servers (OmniSharp for C#, clangd for C++) not installed or not on PATH
**Important**: VS Code's C# extension (ms-dotnettools.csharp 2.x) uses Roslyn, NOT OmniSharp. Serena requires standalone OmniSharp.
**Solution**:
```powershell
# Verify prerequisites
dotnet --version      # Should return .NET SDK version
clangd --version      # Should return clangd version (for C++)
OmniSharp --version   # Must be standalone OmniSharp, NOT VS Code's

# Install clangd if missing
winget install LLVM.LLVM

# Install standalone OmniSharp (REQUIRED - VS Code's C# extension won't work)
Invoke-WebRequest -Uri "https://github.com/OmniSharp/omnisharp-roslyn/releases/download/v1.39.12/omnisharp-win-x64-net6.0.zip" -OutFile "$env:TEMP\omnisharp.zip"
Expand-Archive "$env:TEMP\omnisharp.zip" -DestinationPath "C:\OmniSharp" -Force
[Environment]::SetEnvironmentVariable("PATH", "$([Environment]::GetEnvironmentVariable('PATH', 'User'));C:\OmniSharp", "User")

# Restart VS Code/terminal after installation
```

### MSBuild Not Found
**Symptom**: `msbuild` command not recognized
**Cause**: Not running in VS Developer environment
**Solution**: Use `.\build.ps1` (auto-initializes) or run from Developer Command Prompt

### WiX Tools Missing
**Symptom**: Installer build fails with candle/light errors
**Cause**: WiX Toolset 3.14.1 not installed
**Solution**: Install WiX Toolset from https://wixtoolset.org/
