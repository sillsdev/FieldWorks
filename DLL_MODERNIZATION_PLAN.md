# DLL and Dependency Modernization Plan for FieldWorks

## Executive Summary

This document provides a comprehensive analysis of all DLLs currently being copied from the `packages/` folder and proposes modernizations to leverage SDK-style project features and NuGet PackageReference.

**Current State**: ~70+ packages are manually copied from the NuGet packages folder to the Output directory via build targets.

**Goal**: Convert manual copy operations to automatic NuGet PackageReference where possible, reducing build complexity and improving maintainability.

---

## Comprehensive Package Inventory

### Category A: Standard .NET Packages (CONVERT TO PackageReference)

These are standard NuGet packages that MSBuild handles automatically. They should be referenced directly in consuming .csproj files.

| Package | Version | Current Copy Method | Proposed Action |
|---------|---------|---------------------|-----------------|
| Newtonsoft.Json | 13.0.3 | NuGottenFiles | Add PackageReference to consuming projects |
| System.Buffers | 4.6.0 | SILNugetPackages | Add PackageReference (transitive, may not be needed) |
| System.Drawing.Common | 9.0.0 | SILNugetPackages | Add PackageReference to projects using it |
| System.Memory | 4.5.4 | SILNugetPackages | Add PackageReference (transitive, may not be needed) |
| System.Net.Http | 4.3.4 | NuGottenFiles | Add PackageReference (already in InstallValidator) |
| System.Numerics.Vectors | 4.5.0 | NuGottenFiles | Add PackageReference (transitive, may not be needed) |
| System.Resources.Extensions | 8.0.0 | SILNugetPackages + NuGottenFiles | Add PackageReference to projects using it |
| System.Runtime.CompilerServices.Unsafe | 6.0.0 | NuGottenFiles | Add PackageReference (transitive, may not be needed) |
| System.Threading.Tasks.Extensions | 4.5.4 | NuGottenFiles | Add PackageReference (transitive, may not be needed) |
| System.ValueTuple | 4.5.0 | NuGottenFiles | Add PackageReference (transitive for < .NET 4.7) |
| System.CodeDom | 4.4.0 | SILNugetPackages | Add PackageReference to projects using it |
| Castle.Core | 4.4.1 | NuGottenFiles | Add PackageReference (Moq dependency - transitive) |
| Moq | 4.17.2 | NuGottenFiles | Add PackageReference to test projects |
| DocumentFormat.OpenXml | 2.20.0 | NuGottenFiles | Add PackageReference to projects using it |
| DotNetZip | 1.16.0 | NuGottenFiles | Add PackageReference to projects using it |
| Autofac | 4.9.4 | SILNugetPackages + NuGottenFiles | Add PackageReference to projects using it |
| CommonServiceLocator | 2.0.7 | SILNugetPackages | Add PackageReference (already in SimpleRootSite) |
| protobuf-net | 2.4.6 | SILNugetPackages | Add PackageReference to projects using it |
| SharpZipLib | 1.4.0 | SILNugetPackages | Add PackageReference to projects using it |
| WeCantSpell.Hunspell | 6.0.0 | SILNugetPackages | Add PackageReference to projects using it |
| Markdig.Signed | 0.30.0 | NuGottenFiles | Add PackageReference to projects using it |
| CsvHelper | 28.0.1 | NuGottenFiles | Add PackageReference to projects using it |
| Analytics | 3.6.0 | NuGottenFiles | Add PackageReference to projects using it |

**Benefit**: MSBuild automatically handles these packages, including transitive dependencies and version resolution.

---

### Category B: SIL Managed Packages (CONVERT TO PackageReference)

All SIL.* packages should use PackageReference for proper dependency management.

#### SIL.LCModel Family (Already partially converted)

| Package | Version | Current Status | Proposed Action |
|---------|---------|----------------|-----------------|
| SIL.LCModel | 11.0.0-beta0140 | Copied + Some PackageReferences | Ensure all consuming projects have PackageReference |
| SIL.LCModel.Core | 11.0.0-beta0140 | Copied + Some PackageReferences | Ensure all consuming projects have PackageReference |
| SIL.LCModel.Utils | 11.0.0-beta0140 | Copied + Some PackageReferences | Ensure all consuming projects have PackageReference |
| SIL.LCModel.FixData | 11.0.0-beta0140 | Copied only | Add PackageReference to projects using it |
| SIL.LCModel.Tools | 11.0.0-beta0140 | Copied only | Add PackageReference to projects using it |
| SIL.LCModel.Build.Tasks | 11.0.0-beta0140 | Copied to tools/ | Keep current (build tool, not runtime dependency) |
| SIL.LCModel.Core.Tests | 11.0.0-beta0140 | Copied + Some PackageReferences | Ensure test projects have PackageReference with PrivateAssets="All" |
| SIL.LCModel.Utils.Tests | 11.0.0-beta0140 | Copied + Some PackageReferences | Ensure test projects have PackageReference with PrivateAssets="All" |
| SIL.LCModel.Tests | 11.0.0-beta0140 | Copied only | Add PackageReference to test projects with PrivateAssets="All" |

#### SIL.Palaso Family (Partially converted)

| Package | Version | Current Status | Proposed Action |
|---------|---------|----------------|-----------------|
| SIL.Core | 17.0.0-beta0080 | Copied + Some PackageReferences | Ensure all consuming projects have PackageReference |
| SIL.Core.Desktop | 17.0.0-beta0080 | Copied + Some PackageReferences | Ensure all consuming projects have PackageReference |
| SIL.Lift | 17.0.0-beta0080 | Copied only | Add PackageReference to projects using it |
| SIL.Media | 17.0.0-beta0080 | Copied only | Add PackageReference to projects using it |
| SIL.Scripture | 17.0.0-beta0080 | Copied only | Add PackageReference to projects using it |
| SIL.Windows.Forms | 17.0.0-beta0080 | Copied only | Add PackageReference to projects using it |
| SIL.Windows.Forms.GeckoBrowserAdapter | 17.0.0-beta0080 | Copied only | Add PackageReference to projects using it |
| SIL.Archiving | 17.0.0-beta0080 | Copied only | Add PackageReference to projects using it |
| SIL.Windows.Forms.Archiving | 17.0.0-beta0080 | Copied only | Add PackageReference to projects using it |
| SIL.Windows.Forms.Keyboarding | 17.0.0-beta0080 | Copied + Some PackageReferences | Ensure all consuming projects have PackageReference |
| SIL.Windows.Forms.WritingSystems | 17.0.0-beta0080 | Copied only | Add PackageReference to projects using it |
| SIL.WritingSystems | 17.0.0-beta0080 | Copied + Some PackageReferences | Ensure all consuming projects have PackageReference |
| SIL.WritingSystems.Tests | 17.0.0-beta0080 | Copied only | Add PackageReference to test projects with PrivateAssets="All" |
| SIL.Lexicon | 17.0.0-beta0080 | Copied only | Add PackageReference to projects using it |
| SIL.TestUtilities | 17.0.0-beta0080 | Copied + Some PackageReferences | Ensure test projects have PackageReference |

#### SIL.Chorus Family

| Package | Version | Current Status | Proposed Action |
|---------|---------|----------------|-----------------|
| SIL.Chorus.LibChorus | 6.0.0-beta0063 | Copied only | Add PackageReference to projects using it |
| SIL.Chorus.App | 6.0.0-beta0063 | Copied only | Add PackageReference to projects using it |

#### Other SIL Packages

| Package | Version | Current Status | Proposed Action |
|---------|---------|----------------|-----------------|
| SIL.Machine | 3.7.4 | Copied only | Add PackageReference to projects using it |
| SIL.Machine.Morphology.HermitCrab | 3.7.4 | Copied only | Add PackageReference to projects using it |
| SIL.DesktopAnalytics | 4.0.0 | Copied only | Add PackageReference to projects using it |
| SIL.FLExBridge.IPCFramework | 1.1.1-beta0001 | Copied only | Add PackageReference to projects using it |

**Benefit**: Automatic transitive dependency resolution, better version management, and reduced build script complexity.

---

### Category C: Third-Party Managed Packages (CONVERT TO PackageReference)

| Package | Version | Current Status | Proposed Action |
|---------|---------|----------------|-----------------|
| L10NSharp | 9.0.0-beta0001 | Copied only | Add PackageReference to projects using it |
| L10NSharp.Windows.Forms | 9.0.0-beta0001 | Copied only | Add PackageReference to projects using it |
| Enchant.Net | 1.4.3-beta0010 | Copied only | Add PackageReference to projects using it |
| NDesk.DBus | 0.15.0 | Copied + Some PackageReferences | Ensure all consuming projects have PackageReference |
| Spart | 1.0.0 | Copied only | Add PackageReference to projects using it |
| TagLibSharp | 2.2.0 | Copied only | Add PackageReference to projects using it |
| Tenuto | 1.0.0.39 | Copied only | Add PackageReference to projects using it |
| relaxngDatatype | 1.0.0.39 | Copied only | Add PackageReference to projects using it |
| Vulcan.Uczniowie.HelpProvider | 1.0.16 | Copied only | Add PackageReference to projects using it |
| Sandwych.Quickgraph.Core | 1.0.0 | Copied only | Add PackageReference to projects using it |
| icu.net | 3.0.0-beta.297 | Copied only | Add PackageReference to projects using it |
| mixpanel-csharp | 6.0.0 | Copied only | Add PackageReference to projects using it |
| NAudio | 1.10.0 | Copied only | Add PackageReference to projects using it |
| NAudio.Lame | 1.1.5 | Copied only | Add PackageReference (includes native libmp3lame DLLs) |
| CommandLineArgumentsParser | 3.0.22 | Copied only | Add PackageReference to projects using it |
| Microsoft.Extensions.DependencyModel | 2.0.4 | Copied only | Add PackageReference to projects using it |
| Microsoft.Win32.Registry | 4.7.0 | Copied only | Add PackageReference (may be transitive) |
| structuremap.patched | 4.7.3 | Copied only | Add PackageReference to projects using it |

**Benefit**: Standard NuGet workflow, automatic updates, less manual maintenance.

---

### Category D: Paratext Packages (EVALUATE - May need runtime-specific handling)

| Package | Version | Current Status | Proposed Action |
|---------|---------|----------------|-----------------|
| ParatextData | 9.4.0.1-beta | Copied only | Add PackageReference to projects using it |
| SIL.ParatextShared | 7.4.0.1 | Copied only | Add PackageReference with proper runtime configuration |

**Note**: These have runtime-specific paths (`runtimes/win/lib/net40/`). Need to verify NuGet handles this correctly.

---

### Category E: Localization Packages (CONVERT with contentFiles)

| Package | Version | Current Status | Proposed Action |
|---------|---------|----------------|-----------------|
| SIL.Chorus.l10ns | (variable) | Manually found in packages/ and copied | Add PackageReference with proper contentFiles mapping |
| SIL.libPalaso.l10ns | (variable) | Manually found in packages/ and copied | Add PackageReference with proper contentFiles mapping |

**Current Code in Localize.targets** (lines 136-171):
```xml
<Target Name="copyLibL10ns">
    <ItemGroup>
        <ChorusL10nsDirs Include="$(fwrt)/packages/sil.chorus.l10ns/*" />
        <PalasoL10nsDirs Include="$(fwrt)/packages/sil.libpalaso.l10ns/*" />
    </ItemGroup>
    <!-- ... manual copy logic ... -->
</Target>
```

**Proposed Solution**:
1. Add PackageReference to a central project (e.g., a localization project)
2. Configure contentFiles to automatically deploy to `DistFiles/CommonLocalizations/`
3. Remove manual copy target

---

### Category F: Native Dependencies (KEEP CURRENT APPROACH)

These have complex layouts and require custom handling. Manual copying is appropriate.

| Package | Type | Reason to Keep Manual Copy |
|---------|------|----------------------------|
| Icu4c.Win.Fw.Bin | Native binaries | Needs x86/x64 separation in lib/ folder |
| Icu4c.Win.Fw.Lib | Native libraries | Headers and .lib files for C++ compilation |
| Geckofx60 | Native + managed | Complex runtime file organization |
| KeymanLegacyBundle | Native interop | Keyman7Interop.dll, Keyman10Interop.dll, KeymanLink.dll |
| Encoding-Converters-Core | Native plugins | Plugin directory structure |

**Also keep**:
- GeckofxHtmlToPdf (TeamCity artifact, not NuGet)
- ExCss (TeamCity artifact, not NuGet)

---

### Category G: DistFiles (NOT FROM NUGET - Keep as-is)

These are checked into the repository under `DistFiles/` and not from NuGet:
- LinqBridge.dll
- log4net.dll
- xample32.dll / xample64.dll
- Interop.ResourceDriver.dll (in Lib/Common/)

---

## Implementation Plan

### Phase 1: Standard .NET Packages (Quick wins)

**Packages to convert**:
- System.* packages (most are transitive, verify which are actually needed)
- Newtonsoft.Json
- Moq (test projects only)
- DocumentFormat.OpenXml
- DotNetZip
- Autofac
- CsvHelper
- Markdig.Signed
- Analytics

**Steps**:
1. Identify which projects use each package (grep for namespace usage)
2. Add `<PackageReference>` to those projects
3. Test build
4. Remove from NuGottenFiles in mkall.targets
5. Verify Output/ folder still contains necessary files (transitive copies)

### Phase 2: SIL Packages (Highest impact)

**Already partially done**: Some projects have PackageReferences for:
- SIL.LCModel.*
- SIL.Core / SIL.Core.Desktop
- SIL.Windows.Forms.Keyboarding
- SIL.WritingSystems
- SIL.TestUtilities
- NDesk.DBus

**Need to complete**:
1. Audit all 102 .csproj files for missing SIL.* PackageReferences
2. Add missing references
3. Remove from SILNugetPackages in mkall.targets
4. Test build

### Phase 3: Third-Party Managed

**Packages**:
- L10NSharp / L10NSharp.Windows.Forms
- Enchant.Net
- icu.net
- NAudio / NAudio.Lame
- SIL.ParatextShared / ParatextData
- All other Category C packages

**Steps**: Same as Phase 1

### Phase 4: Localization Packages

**Special handling required**:
1. Create or identify a project to own the PackageReference
2. Configure NuGet to deploy contentFiles to the correct location
3. Update or remove `copyLibL10ns` target in Localize.targets
4. Test localization build

### Phase 5: Cleanup

1. Remove unnecessary Copy tasks from mkall.targets
2. Remove SILNugetPackages ItemGroup (or keep only native packages)
3. Remove NuGottenFiles ItemGroup (or keep only special cases)
4. Simplify downloadDlls target to only handle native dependencies
5. Update documentation
6. Verify CI builds

---

## Expected Outcomes

### Benefits

1. **Reduced Build Script Complexity**: ~70% reduction in manual copy operations
2. **Automatic Dependency Resolution**: NuGet handles transitive dependencies
3. **Better Version Management**: PackageReference versions in .csproj files, not build scripts
4. **Faster Builds**: NuGet caching and incremental restore
5. **Easier Maintenance**: Standard .NET workflow, familiar to all developers
6. **IDE Support**: Better IntelliSense and package management UI

### Risks and Mitigation

| Risk | Mitigation |
|------|------------|
| Missing DLLs in Output/ | Test thoroughly; NuGet should copy transitive dependencies |
| Version conflicts | Use wildcard versions (11.0.0-*) where appropriate; lock specific versions if needed |
| Native binaries not copied | Keep manual copy for Category F packages |
| Localization files not deployed | Custom contentFiles configuration or keep manual copy initially |
| CI build failures | Test locally first; incremental conversion allows rollback per package |

---

## Testing Strategy

For each phase:
1. Run `dotnet restore` / `msbuild /t:restore`
2. Build the solution: `msbuild FieldWorks.sln /p:Configuration=Debug`
3. Check Output/ folder for expected DLLs
4. Run unit tests
5. Run a sampling of applications (FieldWorks.exe, etc.)
6. Verify localization still works (Phase 4)

---

## Success Criteria

- [ ] All Category A, B, C packages converted to PackageReference
- [ ] mkall.targets simplified (fewer Copy tasks)
- [ ] Solution builds successfully
- [ ] All tests pass
- [ ] Applications run correctly
- [ ] Localization works (if Phase 4 completed)
- [ ] Documentation updated

---

## Appendix: Package Count Summary

| Category | Package Count | Conversion Status |
|----------|--------------|-------------------|
| Standard .NET | ~20 | Convert to PackageReference |
| SIL Packages | ~35 | Convert to PackageReference (partially done) |
| Third-Party Managed | ~15 | Convert to PackageReference |
| Paratext | 2 | Evaluate then convert |
| Localization | 2 | Convert with contentFiles configuration |
| Native Dependencies | ~8 | Keep manual copy |
| DistFiles | 4 | Keep as-is (not from NuGet) |
| **Total** | **~86** | **~74 can be modernized** |

---

*Generated: 2025-11-08*
*Status: Proposal - Ready for Implementation*
