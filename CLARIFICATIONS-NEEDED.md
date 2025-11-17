# Clarifications Needed for Convergence Implementation Plans

This document consolidates all "NEEDS CLARIFICATION" items from the 5 convergence implementation plans. Please provide guidance on each item to proceed with detailed research and task planning.

---

## Convergence 2: GenerateAssemblyInfo Standardization
**Plan**: specs/002-convergence-generate-assembly-info/plan.md
**Status**: Need to rework so that CommonAssemblyInfoTemplate is used for all assemblies and that custom AssemblyInfo.cs files are preserved from before this migration wherever possible.  Revert ones that were deleted / removed.

---

## Convergence 3: RegFree COM Coverage Completion
**Plan**: specs/003-convergence-regfree-com-coverage/plan.md

### D1: Complete EXE Inventory
**Question**: What is the complete list of FieldWorks executables (EXEs) that need COM registration-free manifests?

**Known**:
- FieldWorks.exe (already has manifest from spec 001 and is now the sole supported launcher)
- LCMBrowser.exe
- UnicodeCharEditor.exe
- FXT.exe (FLEx Text processor)
- Test host executables (e.g., TestViews.exe, TestGeneric.exe) that still exercise COM but are slated for retirement

**Need to identify**:
- Te.exe (translation editor - does this exist?) - NO, this is dead.
- Other tools - build 3 exe's from same solution
  - LCM Browser
  - Unicode Char Editor
- Test executables (or do they use shared test host?)
  - TestViews and TestGeneric (executables that unit test frameworks) that need Registry free COM
  - This code will die - but not today.  All C++ code will die.

**Action needed**: Please provide complete list of EXE names and paths, or confirm we should discover via automated inventory scan.

---

### D2: COM Usage Audit Methodology
**Question**: What approach should we use to audit COM usage in each EXE?

**Options**:
- **A. Manual code review** - Search for CoCreateInstance, COM interface declarations, examine using statements
  - Pros: Thorough, catches indirect usage
  - Cons: Time-consuming, may miss dynamic activation
- **B. Automated static analysis** - Parse source for COM patterns, analyze dependencies
  - Pros: Fast, reproducible
  - Cons: May miss reflection-based activation
- **C. Instrumentation + runtime detection** - Run each EXE with COM monitoring, log activations
  - Pros: Catches actual runtime usage
  - Cons: Requires test scenarios to exercise all code paths

**Action needed**: Which approach (or combination) do you prefer?
- Use the makeall.targets to determine which projects use COM by their use of the regfree task.

---

### D3: Test Executable Manifest Strategy
**Question**: How should test executables handle COM activation?

**Options**:
- **A. Individual manifests per test EXE** - Each test project generates its own manifest
  - Pros: Independent, explicit
  - Cons: Many manifests to maintain, duplication
- **B. Shared test host with manifest** - Tests run under NUnit console/test adapter with single manifest
  - Pros: DRY, matches spec 001 pattern
  - Cons: Requires coordination between test projects
- **C. Hybrid** - Core test tools with manifests, unit tests use shared host
  - Pros: Flexible
  - Cons: More complex

**Action needed**: Which strategy aligns with FieldWorks test architecture?
- Src\AssemblyInfoForTests.cs
- Src\AssemblyInfoForUiIndependentTests.cs
- These two files provide the COM and test bootstrapping context for testing.  Use these.

---

### D4: Unique COM Dependencies
**Question**: Do any EXEs have COM dependencies not covered by existing RegFree.targets patterns?
- Check Keyman.  Otherwise no.

### D5: Manifest File Organization (NEW)
**Question**: Should we create per-EXE manifest files or a single shared manifest?

**Options**:
- **A. Per-EXE manifests** - FieldWorks.exe.manifest, LCMBrowser.exe.manifest, etc.
  - Pros: Each EXE has only COM servers it uses, smaller files
  - Cons: Duplication if many EXEs use same COM servers, maintenance overhead
- **B. Shared manifest** - Single manifest with all COM servers, copied/linked to each EXE
  - Pros: DRY, single source of truth
  - Cons: Larger manifest files, each EXE carries all COM registrations

**Action needed**: Which approach do you prefer?

---

## Convergence 4: Test Exclusion Pattern Standardization
**Plan**: specs/004-convergence-test-exclusion-patterns/plan.md

### D2: Mixed Test and Non-Test Code Projects
**Question**: How should we handle projects that contain both test and non-test code?

**Context**: Some projects may have test utilities or test helpers alongside actual test classes. Current exclusion patterns may not cleanly separate these.

**Options**:
- **A. Exclude all test-related code** - Aggressive pattern catches everything with "Test" in name/path
  - Pros: Simple, comprehensive
  - Cons: May exclude test utilities needed by multiple projects
- **B. Explicit per-file exclusions** - List each test file/directory individually
  - Pros: Precise control
  - Cons: Verbose, brittle
- **C. Separate test utilities to own projects** - Refactor mixed projects (out of scope?)
  - Pros: Clean separation
  - Cons: Large structural change

**Action needed**: Are there any projects with mixed code? If so, which approach?

Answer: There should be no projects with mixed code. Test projects were separated in a subproject of the project folder and have their own .csproj files. If any specific project looks to have violated that principle bring it to our attention.

---

### D3: Centralized vs. Per-Project Exclusion Patterns
**Question**: Should exclusion patterns be defined centrally in Directory.Build.props or per-project in .csproj files?

**Options**:
- **A. Centralized in Directory.Build.props**
  ```xml
  <CompileRemove>$(MSBuildProjectName)Tests/**</CompileRemove>
  ```
  - Pros: DRY, single place to update pattern
  - Cons: Requires MSBuild property expansion understanding, less explicit
  - Concern: Does this syntax work correctly? Needs testing.

- **B. Per-project in .csproj files**
  ```xml
  <ItemGroup>
    <Compile Remove="FwUtilsTests/**" />
  </ItemGroup>
  ```
  - Pros: Explicit, clear what each project excludes, no MSBuild magic
  - Cons: Verbose, 35 projects to update, duplication

**Action needed**: Which approach do you prefer? If centralized, confirm MSBuild syntax works.
Answer: I prefer the per project approach because there are also other subfolders that need exclusion. There were subprojects in some projects which needed this Compile Remove treatment. That makes me lean to a per project approach because it keeps all the exclusion for a project in one clear location.

---

## Convergence 5: PrivateAssets Standardization
**Plan**: specs/005-convergence-private-assets/plan.md

### D2: Scope of PrivateAssets Application
**Question**: Should we apply PrivateAssets="All" to all packages in test projects or only known test frameworks?

**Options**:
- **A. Known test frameworks only (Conservative)**
  - Packages: NUnit, NUnit3TestAdapter, Moq, FluentAssertions, xunit.*, MSTest.*, coverlet.*, Microsoft.NET.Test.Sdk
  - Pros: Safe, won't break legitimate transitive dependencies
  - Cons: May miss some test-only packages, requires maintaining list

- **B. All packages in test projects (Aggressive)**
  - PrivateAssets="All" on every PackageReference in projects ending with "Tests"
  - Pros: Complete isolation, no test dependencies leak
  - Cons: May break edge cases (test projects referencing shared libraries that need to propagate dependencies)

- **C. Heuristic-based (Hybrid)**
  - Known test frameworks + packages matching patterns (*test*, *mock*, *fake*, etc.)
  - Pros: Catches more test packages without being too aggressive
  - Cons: Heuristic may have false positives

**Action needed**: Which scope do you prefer? Conservative is safest for initial convergence.

Answer: The PrivateAssets="All" was added specifically for an assembly in LCM that contained a mix of test utilities which we need and its own test code which we do not use. It should not be applied anywhere else unless build failures prove it necessary.

---

### D3: Handling Existing PrivateAssets Values
**Question**: How should we handle packages that already have PrivateAssets set to a different value?

**Context**: Some packages may have PrivateAssets="Compile" (for analyzers) or PrivateAssets="Runtime" (for platform-specific deps).

**Options**:
- **A. Overwrite to "All"**
  - Pros: Consistent, ensures complete isolation
  - Cons: May break intentional partial isolation (e.g., analyzer that should flow to consumers)

- **B. Merge with existing value**
  - If PrivateAssets="Compile", change to PrivateAssets="Compile;Runtime" → equivalent to "All"
  - Pros: Preserves intent, combines restrictions
  - Cons: Complex logic, verbose attribute values

- **C. Skip if already set**
  - Only add PrivateAssets if attribute is absent
  - Pros: Respects existing decisions
  - Cons: May leave inconsistent partial isolation

**Action needed**: Which approach do you prefer? Recommend **A (Overwrite to "All")** for test frameworks specifically, **C (Skip)** for other packages.

Answer: No action is needed due to the answer in D2.
---

## Convergence 6: PlatformTarget Redundancy Cleanup
**Plan**: specs/006-convergence-platform-target/plan.md

### D2: Edge Cases Requiring Explicit PlatformTarget
**Question**: When is explicit PlatformTarget=x64 functionally required despite matching the inherited value?

**Context**: Some projects may have conditional compilation, platform-specific code, or other reasons requiring explicit platform declaration.

**Potential edge cases**:
- Projects with `#if x64` or `#if WIN64` conditional compilation directives
- Projects that P/Invoke native libraries (need explicit platform for DllImport resolution)
- Projects with multi-targeting (though not currently in FieldWorks)
- Projects where AnyCPU behavior would differ from explicit x64

**Action needed**: Are there any known edge cases in FieldWorks? Should we use conservative heuristic (keep explicit if project has P/Invoke or conditional compilation)?

Answer: Because fieldworks assemblies are not frequently consumed by outside entities and we are not targeting 32bit we should safely be able to make all assemblies in FieldWorks x64

---

### D3: Platform vs. PlatformTarget Properties
**Question**: Should we clean up both `<Platform>` and `<PlatformTarget>` properties, or only `<PlatformTarget>`?

**Context**: MSBuild has both properties:
- `<Platform>` - Solution-level configuration (e.g., "x64", "Any CPU")
- `<PlatformTarget>` - Project-level compiler target (e.g., "x64", "AnyCPU", "x86")

**Options**:
- **A. Only clean up PlatformTarget** - Conservative, focus on redundant compiler settings
- **B. Clean up both Platform and PlatformTarget** - Comprehensive, remove all redundancy
- **C. Only clean up Platform, leave PlatformTarget explicit** - Unusual, but possible

**Action needed**: Which properties should we clean up? Recommend **A (PlatformTarget only)** as Platform is typically solution-managed.

Answer: B

---

### D4: AnyCPU Policy - Explicit vs. Implicit
**Question**: For library projects that could be AnyCPU, should PlatformTarget=AnyCPU be explicit or rely on SDK default?

**Context**: .NET SDK default is AnyCPU if PlatformTarget not set. Some FieldWorks libraries may benefit from AnyCPU (cross-platform compatibility), others must be x64 (COM interop, P/Invoke).

**Current state**: Inconsistent - some libraries explicit x64, some explicit AnyCPU, some implicit.

**Options**:
- **A. Explicit AnyCPU for clarity** - All libraries that can be AnyCPU state it explicitly
  - Pros: Clear intent, easier to identify platform requirements
  - Cons: Verbose, may be redundant

- **B. Implicit AnyCPU (no PlatformTarget)** - Rely on SDK default for libraries
  - Pros: DRY, minimal project files
  - Cons: Less explicit, harder to identify intentional AnyCPU vs. missing platform specification

- **C. Explicit x64 only, allow implicit AnyCPU** - Libraries requiring x64 state it, others default
  - Pros: Only specify when deviate from default
  - Cons: Asymmetric policy

**Action needed**: Which policy do you prefer for FieldWorks? Note: FieldWorks is currently x64-only after migration, so this may be moot if no libraries should be AnyCPU.

**Follow-up question**: Should **any** FieldWorks libraries be AnyCPU, or should all be x64 for consistency with 64-bit-only migration?

Answer: All x64

## Convergence 7: SetupIncludeTargets
There are significant things happening in SetupInclude.targets and mkall.targets.
We no longer need to generate fieldworks.targets.  Remove all code that is only used to generate that file.  Review both files for custom logic and assess for each piece if there is a standard way of accomplishing the same task.

There are custom msbuild tasks defined in Build/Src which have been used as part of the old build system. We wish to build from the solution now and some build tasks needed to bootstrap a build will use those tasks.
A careful tracing of the tasks performed by remakefw and its dependencies is necessary to identify all the bootstrapping tasks that will make a FieldWorks build and subsequent execution on a developer machine successful.

---

## Summary of Clarifications by Priority

### HIGH Priority (blocks convergence implementation):
1. **[C3-D1]** Complete EXE inventory for RegFree COM coverage
2. **[C3-D2]** COM usage audit methodology
3. **[C3-D3]** Test executable manifest strategy
4. **[C5-D2]** Scope of PrivateAssets application (conservative vs. aggressive)

### MEDIUM Priority (affects implementation approach):
5. **[C4-D3]** Centralized vs. per-project test exclusion patterns
6. **[C5-D3]** Handling existing PrivateAssets values
7. **[C3-D5]** Per-EXE vs. shared manifest file organization
8. **[C6-D4]** AnyCPU policy for FieldWorks (likely x64-only)

### LOW Priority (edge cases and optimizations):
9. **[C3-D4]** Unique COM dependencies beyond standard patterns
10. **[C4-D2]** Mixed test and non-test code projects (if any exist)
11. **[C6-D2]** Edge cases requiring explicit PlatformTarget
12. **[C6-D3]** Platform vs. PlatformTarget cleanup scope

---

## Recommendation for Proceeding

**Option 1 (Recommended)**: Provide guidance on HIGH priority items now, proceed with research phase for those convergences, defer MEDIUM/LOW items to research.md findings.

**Option 2**: Provide guidance on all items now for comprehensive planning.

**Option 3**: Make reasonable defaults for each clarification (documented in this file), proceed with implementation, adjust based on validation results.

Which option do you prefer?
