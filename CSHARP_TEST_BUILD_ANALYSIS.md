# C# Test Build Analysis

This document analyzes the 168+ compilation errors in test projects and identifies patterns, root causes, and remediation strategies based on prior work done for non-test projects.

## ⚠️ CRITICAL: DO NOT IGNORE OR REMOVE TESTS

**All tests must build and run.** The following approaches are **PROHIBITED**:

1. ❌ **DO NOT** add `[Ignore]` attributes to skip failing tests
2. ❌ **DO NOT** exclude test projects from the build (e.g., removing from `FieldWorks.proj`)
3. ❌ **DO NOT** delete test files or test methods
4. ❌ **DO NOT** comment out test code to make it compile

**Instead, fix the actual issues:**
- Migrate RhinoMocks/NMock patterns to Moq
- Add missing package references
- Fix API incompatibilities by updating the test code
- Resolve duplicate assembly attributes properly

---

## Executive Summary

The test projects suffer from the same issues that were addressed in non-test projects during the SDK migration and GenerateAssemblyInfo convergence work. The key remediation strategies are:

1. **CS0579 (Duplicate Assembly Attributes)** - Projects include both `CommonAssemblyInfo.cs` AND have `GenerateAssemblyInfo` enabled or include `Properties/AssemblyInfo.cs` with duplicates
2. **Missing Assembly References** - Test projects missing required dependencies for `AssemblyInfoForTests.cs`
3. **CS01xx Warnings as Errors** - Unused variables/fields that were warnings but are now errors due to `TreatWarningsAsErrors=true`
4. **Mock Framework Mismatch** - Some tests use RhinoMocks syntax but project references Moq
5. **Missing NuGet Packages** - NUnit.Extensions.Forms and NMock not available

---

## Issue Categories

### 1. CS0579: Duplicate Assembly Attributes (Critical)

**Affected Projects:**
- `ViewsInterfacesTests`
- `xCoreInterfacesTests`
- `DiscourseTests`
- `LexEdDllTests`
- `MorphologyEditorDllTests`
- `Sfm2XmlTests`

**Root Cause:**
Projects have `GenerateAssemblyInfo=false` BUT include **both**:
- `Src/CommonAssemblyInfo.cs` (linked)
- `Src/AssemblyInfoForTests.cs` (auto-included via Directory.Build.props)
- AND their own `Properties/AssemblyInfo.cs` with duplicate attributes

The SDK generates assembly attributes AND the linked files provide them, causing duplicates.

**Evidence from Prior Fix (commit 34c2cbb21):**
```xml
<!-- In Directory.Build.props -->
<GenerateAssemblyInfo>false</GenerateAssemblyInfo>
```

**Solution Pattern (from spec 002):**
1. Set `<GenerateAssemblyInfo>false</GenerateAssemblyInfo>` in the project
2. Remove duplicate attributes from any per-project `AssemblyInfo.cs`
3. Ensure only ONE of: CommonAssemblyInfo.cs OR GenerateAssemblyInfo=true

**Python Helper Script:** `scripts/GenerateAssemblyInfo/convert_generate_assembly_info.py`

```bash
# Audit current state
python -m scripts.GenerateAssemblyInfo.audit_generate_assembly_info --repo-root .

# Convert and fix issues
python -m scripts.GenerateAssemblyInfo.convert_generate_assembly_info --repo-root . --dry-run
```

---

### 2. Missing Dependencies for AssemblyInfoForTests.cs (Critical)

**Affected Projects:**
- `Sfm2XmlTests`
- `ScrChecksTests`

**Root Cause:**
`Directory.Build.props` automatically includes `AssemblyInfoForTests.cs` for all test projects:

```xml
<ItemGroup Condition="'$(IsTestProject)' == 'true' AND '$(UseUiIndependentTestAssemblyInfo)' != 'true'">
  <Compile Include="$(FwRoot)Src\AssemblyInfoForTests.cs" Link="Properties\AssemblyInfoForTests.cs" />
</ItemGroup>
```

But `AssemblyInfoForTests.cs` requires references to:
- `SIL.LCModel.Core.Attributes` → from `SIL.LCModel.Core` package
- `SIL.FieldWorks.Common.FwUtils.Attributes` → from `FwUtils` project
- `SIL.LCModel.Utils.Attributes` → from `SIL.LCModel.Utils` package
- `SIL.TestUtilities` → from `SIL.TestUtilities` package

**Projects Missing These References:**
| Project | Missing References |
|---------|-------------------|
| `Sfm2XmlTests` | `SIL.LCModel.Core`, `SIL.LCModel.Utils`, `FwUtils`, `FwUtilsTests` |
| `ScrChecksTests` | `FwUtilsTests`, `SIL.TestUtilities` |

**Solution Options:**

**Option A: Add missing package/project references:**
```xml
<ItemGroup>
  <PackageReference Include="SIL.LCModel.Core" Version="11.0.0-*" />
  <PackageReference Include="SIL.LCModel.Core.Tests" Version="11.0.0-*" PrivateAssets="All" />
  <PackageReference Include="SIL.LCModel.Utils" Version="11.0.0-*" />
  <PackageReference Include="SIL.LCModel.Utils.Tests" Version="11.0.0-*" PrivateAssets="All" />
  <PackageReference Include="SIL.TestUtilities" Version="17.0.0-*" />
  <ProjectReference Include="../../FwUtils/FwUtilsTests/FwUtilsTests.csproj" />
</ItemGroup>
```

**Option B: Use UI-independent test assembly info:**
```xml
<PropertyGroup>
  <UseUiIndependentTestAssemblyInfo>true</UseUiIndependentTestAssemblyInfo>
</PropertyGroup>
```

This uses `AssemblyInfoForUiIndependentTests.cs` instead, which has fewer dependencies.

---

### 3. CS01xx Warnings Treated as Errors (Medium)

**Affected Projects:**
- `WidgetsTests` (CS0169 - unused field)
- `FwControlsTests` (CS0219 - assigned but never used)
- `FrameworkTests` (CS0168 - declared but never used)
- `LexTextControlsTests`
- `xWorksTests`
- `Paratext8PluginTests` (CS0649 - never assigned)

**Root Cause:**
`Directory.Build.props` sets:
```xml
<TreatWarningsAsErrors>true</TreatWarningsAsErrors>
```

But test projects have old code with unused variables/fields.

**Solution Options:**

**Option A: Suppress specific warnings in test projects (Recommended for legacy code):**
```xml
<PropertyGroup>
  <NoWarn>$(NoWarn);CS0168;CS0169;CS0219;CS0649</NoWarn>
</PropertyGroup>
```

**Option B: Fix the code (Recommended for new code):**
- CS0168/CS0219: Remove unused variable declarations
- CS0169/CS0649: Remove unused fields or add `#pragma warning disable`

**Example Fix (from prior FwUtilsTests fix):**
```csharp
// Before (CS0168):
catch (Exception e)
{
    // logging without using e
}

// After:
catch (Exception)
{
    // discarding exception intentionally
}
```

---

### 4. Mock Framework Mismatch (Medium)

**Affected Projects:**
- `FrameworkTests` - FwEditingHelperTests.cs
- `MorphologyEditorDllTests` - RespellingTests.cs
- `FlexPathwayPluginTests` - Uses NMock (migrated to Moq)

**Root Cause:**
Code uses RhinoMocks/NMock syntax but project references Moq:
```csharp
// RhinoMocks syntax (NOT supported):
selHelper.Setup(selH => selH.Selection).Returns(selection);
selection.GetArgumentsForCallsMadeOn(sel => sel.SetTypingProps(null));
MockRepository.GenerateStub<T>()

// NMock syntax (old):
using NMock;
new DynamicMock(typeof(IHelpTopicProvider));
helpProvider.MockInstance
```

**⚠️ REQUIRED: Migrate to Moq - DO NOT IGNORE TESTS**

**Moq Equivalent Syntax:**
```csharp
// Moq syntax (current standard):
var mockSelHelper = new Mock<SelectionHelper>();
mockSelHelper.Setup(selH => selH.Selection).Returns(selection);
mockSelHelper.Object.Selection; // to get the actual object

// NMock DynamicMock → Moq Mock<T>
var helpProvider = new Mock<IHelpTopicProvider>();
helpProvider.Object  // instead of MockInstance

// MockRepository.GenerateStub<T>() → new Mock<T>()
var mockTextList = new Mock<InterestingTextList>(args);
mockTextList.Setup(tl => tl.InterestingTexts).Returns(new IStText[0]);
```

**Migration Pattern for RhinoMocks `.Stub()`:**
```csharp
// Before (RhinoMocks):
xmlCache.Stub(c => c.get_IntProp(paraT.Hvo, CmObjectTags.kflidClass)).Return(ScrTxtParaTags.kClassId);

// After (Moq):
mockXmlCache.Setup(c => c.get_IntProp(paraT.Hvo, CmObjectTags.kflidClass)).Returns(ScrTxtParaTags.kClassId);
```

**Migration Pattern for `GetArgumentsForCallsMadeOn`:**
```csharp
// Before (RhinoMocks):
selection.GetArgumentsForCallsMadeOn(sel => sel.SetTypingProps(null));

// After (Moq) - use Verify with callback:
ITsTextProps capturedProps = null;
mockSelection.Setup(s => s.SetTypingProps(It.IsAny<ITsTextProps>()))
    .Callback<ITsTextProps>(p => capturedProps = p);
// ... run test ...
mockSelection.Verify(s => s.SetTypingProps(It.IsAny<ITsTextProps>()), Times.Once());
// Then assert on capturedProps
```

---

### 5. Missing/Incompatible NuGet Packages (Requires Migration)

**Affected Projects:**
- `MessageBoxExLibTests` (NUnit.Extensions.Forms / NUnitForms)

**Root Cause:**
The NUnitForms package (1.3.1) exists on NuGet but has API incompatibilities:
- `NUnitFormTest.SetUp()` and `TearDown()` are protected, not public
- `ExpectModal()` method is protected, not accessible from composition

**⚠️ REQUIRED: Fix API Usage - DO NOT IGNORE TESTS**

**Solution: Inherit from NUnitFormTest instead of composing it:**
```csharp
// Before (broken - uses composition):
public class MessageBoxTests
{
    private NUnitFormTest m_FormTest;

    [SetUp]
    public void Setup()
    {
        m_FormTest = new NUnitFormTest();
        m_FormTest.SetUp();  // ERROR: SetUp is protected
    }

    [Test]
    public void TimeoutOfNewBox()
    {
        m_FormTest.ExpectModal(name, DoNothing, true);  // ERROR: ExpectModal is protected
    }
}

// After (correct - uses inheritance):
public class MessageBoxTests : NUnitFormTest
{
    // SetUp and TearDown are inherited from NUnitFormTest
    // Override if needed:
    public override void Setup()
    {
        base.Setup();
        // additional setup
    }

    [Test]
    public void TimeoutOfNewBox()
    {
        ExpectModal(name, DoNothing, true);  // Now accessible as inherited protected member
    }
}
```

**Package Reference (already added):**
```xml
<PackageReference Include="NUnitForms" Version="1.3.1" />
```

---

## Prior Work Reference

### Commit 34c2cbb21 - GenerateAssemblyInfo Spec Implementation

This commit addressed the same issues for non-test projects:
- Updated 150+ project files
- Standardized `GenerateAssemblyInfo=false`
- Created/updated `CommonAssemblyInfo.cs` linking
- Removed duplicate attributes from per-project AssemblyInfo.cs files

### Python Scripts Available

| Script | Purpose |
|--------|---------|
| `scripts/GenerateAssemblyInfo/audit_generate_assembly_info.py` | Audit projects for compliance |
| `scripts/GenerateAssemblyInfo/convert_generate_assembly_info.py` | Auto-fix GenerateAssemblyInfo issues |
| `scripts/GenerateAssemblyInfo/validate_generate_assembly_info.py` | Validate compliance |
| `Build/convertToSDK.py` | SDK format conversion with reference mapping |

---

## Remediation Plan

### ⚠️ Guiding Principle: FIX, DON'T IGNORE

Every test must compile and run. If a test fails at runtime, that's acceptable (it indicates a real issue to fix). But tests must not be:
- Ignored with `[Ignore]` attributes
- Excluded from the build
- Deleted or commented out

### Phase 1: Quick Wins (CS0579 - Duplicate Attributes)

1. Run the audit script:
   ```bash
   python -m scripts.GenerateAssemblyInfo.audit_generate_assembly_info --repo-root . --only-tests
   ```

2. For each affected project, ensure:
   - `<GenerateAssemblyInfo>false</GenerateAssemblyInfo>` is set
   - Only ONE of `CommonAssemblyInfo.cs` or `Properties/AssemblyInfo.cs` is included
   - No duplicate attributes exist

### Phase 2: Missing Dependencies

1. For `Sfm2XmlTests` and `ScrChecksTests`, either:
   - Add required package references, OR
   - Set `<UseUiIndependentTestAssemblyInfo>true</UseUiIndependentTestAssemblyInfo>`

### Phase 3: Warnings as Errors

1. Add standard NoWarn for test projects in Directory.Build.props:
   ```xml
   <PropertyGroup Condition="'$(IsTestProject)' == 'true'">
     <NoWarn>$(NoWarn);CS0168;CS0169;CS0219;CS0649</NoWarn>
   </PropertyGroup>
   ```

2. Or fix individual warnings in code (cleaner but more work)

### Phase 4: Mock Framework Migration (REQUIRED - DO NOT SKIP)

**RhinoMocks → Moq Migration:**

1. **RespellingTests.cs** - Migrate `MockRepository.GenerateStub<T>()` and `.Setup()/.Stub()` patterns
2. **FwEditingHelperTests.cs** - Migrate `.Setup()`, `.Stub()`, `GetArgumentsForCallsMadeOn` patterns
3. **FlexPathwayPluginTests.cs** - Already migrated from NMock to Moq

**Key Migration Patterns:**
| RhinoMocks | Moq |
|------------|-----|
| `MockRepository.GenerateStub<T>()` | `new Mock<T>()` |
| `mock.Stub(x => x.Method()).Return(value)` | `mock.Setup(x => x.Method()).Returns(value)` |
| `mock.GetArgumentsForCallsMadeOn(...)` | `mock.Setup(...).Callback<T>(capture)` + `mock.Verify(...)` |

### Phase 5: NUnitForms API Fix (REQUIRED - DO NOT SKIP)

**MessageBoxExLibTests.cs** - Change from composition to inheritance:
```csharp
// Change class to inherit from NUnitFormTest
public class MessageBoxTests : NUnitFormTest
{
    // Remove m_FormTest field and Setup/Teardown that call it
    // Use inherited ExpectModal directly
}
```

---

## Quick Reference: Error to Solution Map

| Error | Affected Projects | Solution |
|-------|-------------------|----------|
| CS0579 (Duplicate attribute) | ViewsInterfacesTests, xCoreInterfacesTests, etc. | Set GenerateAssemblyInfo=false, remove duplicate AssemblyInfo |
| CS0246 (Type not found) for AssemblyInfo attributes | Sfm2XmlTests, ScrChecksTests | Add missing package refs or use UseUiIndependentTestAssemblyInfo |
| CS0168/CS0169/CS0219/CS0649 | Many test projects | Add to NoWarn or fix code |
| CS1061 (Setup/GetArgumentsForCallsMadeOn) | FrameworkTests, MorphologyEditorDllTests | **Migrate RhinoMocks → Moq** (see Phase 4) |
| CS0117 (MockRepository.GenerateStub) | MorphologyEditorDllTests | **Migrate RhinoMocks → Moq** (see Phase 4) |
| CS0122 (ExpectModal protected) | MessageBoxExLibTests | **Inherit from NUnitFormTest** (see Phase 5) |
| CS1061 (SetUp/TearDown protected) | MessageBoxExLibTests | **Inherit from NUnitFormTest** (see Phase 5) |

---

## Next Steps

1. [ ] Run audit script to get exact list of affected test projects
2. [ ] Apply GenerateAssemblyInfo fixes (Phase 1)
3. [ ] Fix missing dependencies (Phase 2)
4. [ ] Add NoWarn for common test warnings (Phase 3)
5. [ ] **Migrate RhinoMocks tests to Moq** (Phase 4) - DO NOT IGNORE
6. [ ] **Fix NUnitForms inheritance** (Phase 5) - DO NOT IGNORE
7. [ ] Build and run ALL tests to verify fixes
