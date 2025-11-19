# Convergence Path Analysis: PrivateAssets on Test Packages

**Priority**: ⚠️ **MEDIUM**
**Framework**: Uses [CONVERGENCE-FRAMEWORK.md](CONVERGENCE-FRAMEWORK.md)
**Current State**: Inconsistent use of PrivateAssets attribute on test packages
**Impact**: Test dependencies may leak to consuming projects, unnecessary package downloads

---

## Clarifications

### Session 2025-11-14
- Q: Should we apply PrivateAssets="All" to all packages in test projects or only known test frameworks? → A: Apply it only to the mixed LCM test-utility assembly that shares reusable helpers, and leave every other project untouched unless a later build failure proves additional scope is necessary.

---

## Current State Analysis

### Statistics
```
Total Test Projects inspected: ~46
In-scope subset (references SIL.LCModel.*.Tests packages): 12 projects
Out-of-scope subset (no SIL.LCModel.*.Tests reference): Remainder, leave unchanged
```

### Problem Statement
Test-only packages (NUnit, Moq, TestUtilities) should use `PrivateAssets="All"` to prevent:
- Test frameworks appearing as dependencies of production code
- Transitive test dependencies flowing to consuming projects
- Unnecessary package downloads for library consumers
- NU1102 warnings about missing test packages

**Current Issue**: Only some test projects referencing the shared LCM helper packages use this attribute, creating inconsistency and leaking helper-specific dependencies downstream.

---

## Convergence Path Options

### **Path A: Targeted PrivateAssets** ✅ **RECOMMENDED**

**Philosophy**: Only the mixed LCM test-utility assemblies (`SIL.LCModel.*.Tests` packages) require immediate enforcement, matching clarification guidance.

**Strategy**:
```xml
<!-- Standard pattern for the targeted LCM helper packages -->
<PackageReference Include="SIL.LCModel.Core.Tests" Version="12.0.0-*" PrivateAssets="All" />
<PackageReference Include="SIL.LCModel.Tests" Version="12.0.0-*" PrivateAssets="All" />
<PackageReference Include="SIL.LCModel.Utils.Tests" Version="12.0.0-*" PrivateAssets="All" />
```

**Test Package List (in-scope)**:
- `SIL.LCModel.Core.Tests`
- `SIL.LCModel.Tests`
- `SIL.LCModel.Utils.Tests`

**Explicitly out-of-scope for this convergence**: NUnit, adapters, Moq, Microsoft.NET.Test.Sdk, and other third-party packages. They may be revisited in a later convergence if leakage evidence emerges.

**Effort**: 3-4 hours | **Risk**: LOW

---

### **Path B: Directory.Build.props Approach**

**Philosophy**: Centralize test package definitions

**Strategy**:
```xml
<!-- Directory.Build.props -->
<ItemGroup Condition="'$(IsTestProject)'=='true'">
  <PackageReference Include="NUnit" Version="4.4.0" PrivateAssets="All" />
  <PackageReference Include="Moq" Version="4.20.70" PrivateAssets="All" />
</ItemGroup>
```

**Pros**: ✅ Central definition, less duplication
**Cons**: ❌ Inflexible (not all tests use all packages), harder to override

**Effort**: 5-6 hours | **Risk**: MEDIUM

---

### **Path C: MSBuild Automatic Attribution**

**Philosophy**: Automatically add PrivateAssets via build targets

**Strategy**:
```xml
<!-- Build/TestPackages.targets -->
<Target Name="AddPrivateAssetsToTestPackages" BeforeTargets="CollectPackageReferences">
  <ItemGroup>
    <PackageReference Update="NUnit" PrivateAssets="All" />
    <PackageReference Update="Moq" PrivateAssets="All" />
  </ItemGroup>
</Target>
```

**Effort**: 6-7 hours | **Risk**: MEDIUM

---

## Recommendation: Path A

**Rationale**: Simple, explicit, works immediately without complex infrastructure while staying narrowly focused on the helper packages that actually ship reusable assets.

---

## User Stories

### US1 (Priority P1): LCM helper packages remain private
**Statement**: As a FieldWorks developer, I need every `SIL.LCModel.*.Tests` PackageReference inside managed test projects to declare `PrivateAssets="All"` so consumers of those reusable helpers never inherit our internal test frameworks.

**Acceptance Criteria**:
1. `private_assets_audit.csv` lists zero rows for `SIL.LCModel.*.Tests` packages after conversion.
2. Git diffs show only the targeted PackageReferences were updated; other packages remain untouched.

### US2 (Priority P2): Validation+documentation guardrail
**Statement**: As a release engineer, I need automated validation (Convergence `validate` + MSBuild NU1102 scan) and updated quickstart guidance so future teams can re-run the workflow confidently.

**Acceptance Criteria**:
1. `python convergence.py private-assets validate` succeeds with artifacts captured under `specs/005-convergence-private-assets/validation/`.
2. `msbuild FieldWorks.sln /m /p:Configuration=Debug` completes with zero NU1102 warnings; log evidence stored.
3. `quickstart.md` lists the exact commands and artifact locations used.

---

## Implementation

**Process**: See [CONVERGENCE-FRAMEWORK.md](CONVERGENCE-FRAMEWORK.md#shared-process-template) for standard 5-phase approach. Scope this convergence strictly to the LCM mixed test-utility assemblies listed above. Other test projects remain unchanged unless future failures require expanding coverage.

### Convergence-Specific Additions

#### Phase 1: Audit
```bash
python convergence.py private-assets audit
# Outputs: private_assets_audit.csv
```

**Specific Checks**:
- Identify all test projects (name ends in "Tests" or "Tests.csproj")
- For each test project, check PackageReferences
- Flag test packages without PrivateAssets="All"

#### Phase 2: Implementation
```bash
python convergence.py private-assets convert --decisions private_assets_decisions.csv
```

- For each `SIL.LCModel.*.Tests` PackageReference without `PrivateAssets`
- Add `PrivateAssets="All"` attribute
- Preserve all other attributes (Version, Include, etc.)

#### Phase 3: Validation
```bash
python convergence.py private-assets validate
```

**Validation Checks**:
1. All `SIL.LCModel.*.Tests` PackageReferences have `PrivateAssets="All"`
2. No NU1102 warnings in build
3. Test projects still build successfully
4. Tests still run successfully

---

## Python Scripts

**Extends**: Framework base classes from [CONVERGENCE-FRAMEWORK.md](CONVERGENCE-FRAMEWORK.md#shared-python-tooling-architecture)

### Convergence-Specific Implementation

```python
from audit_framework import ConvergenceAuditor, ConvergenceConverter, ConvergenceValidator

class PrivateAssetsAuditor(ConvergenceAuditor):
    """Audit PrivateAssets on test packages"""

    TEST_PACKAGES = [
        'SIL.LCModel.Core.Tests',
        'SIL.LCModel.Tests',
        'SIL.LCModel.Utils.Tests',
    ]

    def analyze_project(self, project_path):
        """Check if project is test project and has proper PrivateAssets"""
        # Only analyze test projects
        if not ('Tests' in project_path.stem or 'Test' in project_path.stem):
            return None

        tree = parse_csproj(project_path)
        root = tree.getroot()

        # Find all PackageReferences
        missing_private_assets = []
        for package_ref in root.findall('.//PackageReference'):
            include = package_ref.get('Include', '')
            private_assets = package_ref.get('PrivateAssets', '')

            if include in self.TEST_PACKAGES and private_assets != 'All':
                missing_private_assets.append(include)

        if missing_private_assets:
            return {
                'ProjectPath': str(project_path),
                'ProjectName': project_path.stem,
                'MissingPrivateAssets': ','.join(missing_private_assets),
                'Action': 'AddPrivateAssets'
            }

        return None

class PrivateAssetsConverter(ConvergenceConverter):
    """Add PrivateAssets="All" to test packages"""

    def convert_project(self, project_path, **kwargs):
        """Add PrivateAssets to test packages"""
        packages = kwargs.get('MissingPrivateAssets', '').split(',')

        tree = parse_csproj(project_path)
        root = tree.getroot()

        # Update each package
        for package_ref in root.findall('.//PackageReference'):
            if package_ref.get('Include') in packages:
                package_ref.set('PrivateAssets', 'All')

        update_csproj(project_path, tree)
        print(f"✓ Added PrivateAssets to {project_path.name}")
```

---

## Success Metrics

**Before**:
- ❌ 12 in-scope test projects reference `SIL.LCModel.*.Tests` without `PrivateAssets` (Initial Estimate)
- ❌ Helper consumers inherit unnecessary dependencies
- ❌ NU1102 warnings possible when helpers transitively pull NUnit/Moq

**After**:
- ✅ Every `SIL.LCModel.*.Tests` reference (all three packages across in-scope projects) declares `PrivateAssets="All"`
- ✅ Helper packages publish clean dependency graphs
- ✅ NU1102 warnings eliminated for the targeted packages

**Actual Outcomes (2025-11-19)**:
- Audit confirmed 100% compliance (0 violations found).
- Validation passed with zero NU1102 warnings.
- No code changes were required.

---

## Timeline

**Total Effort**: 3-4 hours over 0.5 day

| Phase          | Duration  |
| -------------- | --------- |
| Audit          | 1 hour    |
| Implementation | 1-2 hours |
| Validation     | 1 hour    |
| Documentation  | 0.5 hour  |

---

*Uses: [CONVERGENCE-FRAMEWORK.md](CONVERGENCE-FRAMEWORK.md)*
*Last Updated: 2025-11-08*
*Status: Ready for Implementation*
