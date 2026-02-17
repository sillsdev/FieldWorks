# Convergence Path Analysis: Registration-Free COM Coverage

**Priority**: ⚠️ **HIGH**
**Divergent Approach**: Incomplete COM manifest coverage across executables
**Current State**: Only FieldWorks.exe has complete manifest generation
**Impact**: Other EXEs may fail on systems without COM registration, incomplete self-contained deployment

---

## Current State Analysis

### Statistics
```
Total EXE/EXE Projects: 10 identified
- With RegFree manifest: 1 (FieldWorks.exe) - **VERIFIED & FIXED**
- With test manifest: 1 (ComManifestTestHost.exe)
- Without manifests: 8 (user-facing + utility tools)
- Confirmed no COM: 0 (needs verification)
```

### Problem Statement
The migration to registration-free COM (commits 44, 47, 90) implemented manifest generation for FieldWorks.exe.
**Update (2025-11-20):** A regression was identified where the .NET SDK embedded a default manifest that overrode the RegFree manifest, causing startup crashes. This has been fixed by adding `<NoWin32Manifest>true</NoWin32Manifest>` to `FieldWorks.csproj` and ensuring managed COM assemblies are explicitly listed in `BuildInclude.targets`.

Remaining issues:
- Other EXE projects haven't been audited for COM usage
- The former LexTextExe (Flex.exe) stub has been removed; FieldWorks.exe is now the sole launcher with a manifest
- Utility EXEs (LCMBrowser, UnicodeCharEditor, MigrateSqlDbs, FixFwData, etc.) may use COM but have no manifests
- Without manifests, these EXEs will fail on clean systems
- Incomplete implementation of self-contained deployment goal

### Root Cause
The RegFree COM implementation was done incrementally. The recent crash in `FieldWorks.exe` was due to a conflict between SDK-generated manifests and our custom RegFree manifests, which is now resolved. No systematic COM usage audit was performed for other EXEs.

---

## EXE Projects Requiring Analysis

### Confirmed EXE Projects (10 total)

1. **FieldWorks.exe** ✅ COMPLETE
   - Location: `Src/Common/FieldWorks/FieldWorks.csproj`
   - Status: Manifest generated, tested, working
   - COM Usage: Extensive (Views, FwKernel, multiple COM components)

2. **ComManifestTestHost.exe** ✅ TEST HOST
   - Location: `Src/Utilities/ComManifestTestHost/ComManifestTestHost.csproj`
   - Status: Manifest for testing purposes
   - COM Usage: Test scenarios only

3. **LCMBrowser.exe** ⚠️ NEEDS MANIFEST
  - Location: `Src/LCMBrowser/LCMBrowser.csproj`
  - Status: No manifest
  - COM Usage: Likely (browses LCModel data)
  - Priority: HIGH

4. **UnicodeCharEditor.exe** ⚠️ NEEDS MANIFEST
  - Location: `Src/UnicodeCharEditor/UnicodeCharEditor.csproj`
  - Status: No manifest
  - COM Usage: Likely (shares infrastructure with FieldWorks)
  - Priority: HIGH

5. **MigrateSqlDbs.exe** ❓ LIKELY USES COM
  - Location: `Src/MigrateSqlDbs/MigrateSqlDbs.csproj`
  - Status: No manifest
  - COM Usage: Likely (database operations may use COM)
  - Priority: MEDIUM

6. **FixFwData.exe** ❓ LIKELY USES COM
  - Location: `Src/Utilities/FixFwData/FixFwData.csproj`
  - Status: No manifest
  - COM Usage: Likely (FLEx data manipulation)
  - Priority: MEDIUM

7. **FxtExe.exe** ❓ UNKNOWN
  - Location: `Src/FXT/FxtExe/FxtExe.csproj`
  - Status: No manifest
  - COM Usage: Unknown - needs audit
  - Priority: MEDIUM

8. **ConvertSFM.exe** ❓ UNLIKELY
  - Location: `Src/Utilities/SfmToXml/ConvertSFM/ConvertSFM.csproj`
  - Status: No manifest
  - COM Usage: Unlikely (file conversion utility)
  - Priority: LOW

9. **SfmStats.exe** ❓ UNLIKELY
  - Location: `Src/Utilities/SfmStats/SfmStats.csproj`
  - Status: No manifest
  - COM Usage: Unlikely (statistics utility)
  - Priority: LOW

10. **Converter.exe / ConverterConsole.exe** ❓ UNKNOWN
   - Location: `Lib/src/Converter/Converter/Converter.csproj` & `Lib/src/Converter/ConvertConsole/ConverterConsole.csproj`
   - Status: No manifests
   - COM Usage: Unknown (depends on Views/FwKernel usage)
   - Priority: LOW

---

## Convergence Path Options

### **Path A: Complete Coverage Approach** ✅ **RECOMMENDED**

**Philosophy**: Audit all EXEs, add manifests to all that use COM

**Strategy**:
1. Systematic COM usage audit for all 6 unknown EXEs
2. Add RegFree.targets to all COM-using EXEs
3. Generate and test manifests for each
4. Document which EXEs don't need manifests and why

**Pros**:
- ✅ Complete self-contained deployment
- ✅ No surprises on clean systems
- ✅ Future-proof (all bases covered)
- ✅ Clear documentation of COM usage

**Cons**:
- ⚠️ More testing required (6 EXEs to validate)
- ⚠️ Larger installer (more manifest files)
- ⚠️ Time investment (6-8 hours)

**Effort**: 6-8 hours (audit + implement + test)

**Risk**: LOW - Known working pattern (FieldWorks.exe)

---

### **Path B: Priority-Based Approach**

**Philosophy**: Add manifests only to high-priority EXEs

**Strategy**:
1. Audit the remaining user-facing tools first (LCMBrowser, UnicodeCharEditor)
2. Add manifests to those EXEs
3. Document other EXEs as "deferred" with rationale
4. Plan follow-up work for the utilities (MigrateSqlDbs, FixFwData, FxtExe, etc.)

**Tier 1 (Must Have)**:
- LCMBrowser.exe
- UnicodeCharEditor.exe

**Tier 2 (Should Have)**:
- MigrateSqlDbs.exe
- FixFwData.exe

**Tier 3 (Nice to Have)**:
- FxtExe.exe
- ConverterConsole.exe

**Tier 4 (Probably Don't Need)**:
- ConvertSFM.exe
- SfmStats.exe

**Pros**:
- ✅ Faster initial implementation (2-3 hours)
- ✅ Focuses on critical path
- ✅ Can defer low-priority work

**Cons**:
- ❌ Incomplete coverage leaves gaps
- ❌ May need rework later
- ❌ Uncertainty about Tier 2/3 EXEs

**Effort**: 2-3 hours initially, 4-5 hours for follow-up

**Risk**: MEDIUM - May miss COM usage in lower tiers

---

### **Path C: Lazy Evaluation Approach**

**Philosophy**: Add manifests only when failures occur

**Strategy**:
1. Document current state (FieldWorks.exe has manifest)
2. Add manifests reactively when EXEs fail on clean systems
3. Keep track of which EXEs are known to work without manifests

**Pros**:
- ✅ Minimal upfront effort
- ✅ No over-engineering
- ✅ Learn from actual usage patterns

**Cons**:
- ❌ Users may hit failures in production
- ❌ Unprofessional (reactive vs. proactive)
- ❌ Doesn't meet self-contained deployment goal
- ❌ Harder to test (need clean systems to reproduce)

**Effort**: 0-1 hours initially, unknown ongoing

**Risk**: HIGH - Production failures possible

---

## Recommendation: Path A (Complete Coverage)

**Rationale**:
1. **Goal Alignment**: Completes the self-contained deployment vision
2. **Professional**: Proactive vs. reactive approach
3. **Known Pattern**: We've already solved this for FieldWorks.exe
4. **Low Risk**: Pattern is proven, just needs replication

**Priority Order**:
1. **LCMBrowser** (HIGH) - UI browser for LCModel data, user-facing
2. **UnicodeCharEditor** (HIGH) - User-facing tool that shares FieldWorks infrastructure
3. **MigrateSqlDbs** (MEDIUM) - Database utility, likely uses COM
4. **FixFwData** (MEDIUM) - Data manipulation, likely uses COM
5. **FxtExe** (MEDIUM) - Unknown, needs audit
6. **ConverterConsole/Converter** (LOW) - File conversion utilities, likely minimal COM
7. **ConvertSFM** (LOW) - File utility, unlikely needs COM
8. **SfmStats** (LOW) - Statistics, unlikely needs COM

---

## Implementation Checklist

### Phase 1: COM Usage Audit (2-3 hours)
- [ ] **Task 1.1**: Audit LCMBrowser for COM usage
  ```bash
  cd Src/LCMBrowser
  grep -r "DllImport.*ole32\|ComImport\|CoClass\|IDispatch" *.cs
  ```
  Expected: **USES COM** (navigates LCModel data)

- [ ] **Task 1.2**: Audit UnicodeCharEditor for COM usage
  ```bash
  cd Src/UnicodeCharEditor
  grep -r "DllImport.*ole32\|ComImport\|CoClass\|IDispatch" *.cs
  ```
  Expected: **LIKELY** (shares FieldWorks infrastructure)

- [ ] **Task 1.3**: Audit MigrateSqlDbs for COM usage
  ```bash
  cd Src/MigrateSqlDbs
  grep -r "DllImport.*ole32\|ComImport\|CoClass" *.cs
  grep -r "FwKernel\|Views\|LgIcu" *.cs
  ```
  Expected: **MAY USE COM** (depends on FW components used)

- [ ] **Task 1.4**: Audit FixFwData for COM usage
  ```bash
  cd Src/Utilities/FixFwData
  grep -r "DllImport.*ole32\|ComImport\|CoClass" *.cs
  grep -r "FwKernel\|Views\|LgIcu" *.cs
  ```
  Expected: **MAY USE COM** (data manipulation likely uses COM)

- [ ] **Task 1.5**: Audit FxtExe for COM usage
  ```bash
  cd Src/FXT/FxtExe
  grep -r "DllImport.*ole32\|ComImport\|CoClass" *.cs
  ```
  Expected: **UNKNOWN** (needs manual review)

- [ ] **Task 1.6**: Audit ConverterConsole/Converter for COM usage
  ```bash
  cd Lib/src/Converter
  grep -r "DllImport.*ole32\|ComImport\|CoClass" *.cs
  ```
  Expected: **UNKNOWN** (depends on Views infrastructure)

- [ ] **Task 1.7**: Audit ConvertSFM for COM usage
  ```bash
  cd Src/Utilities/SfmToXml/ConvertSFM
  grep -r "DllImport.*ole32\|ComImport\|CoClass" *.cs
  ```
  Expected: **UNLIKELY** (file conversion utility)

- [ ] **Task 1.8**: Audit SfmStats for COM usage
  ```bash
  cd Src/Utilities/SfmStats
  grep -r "DllImport.*ole32\|ComImport\|CoClass" *.cs
  ```
  Expected: **UNLIKELY** (statistics utility)

- [ ] **Task 1.9**: Create audit summary spreadsheet
  - Columns: EXE Name, Uses COM (Y/N), COM Components Used, Priority, Action
  - Document findings with evidence (grep output)

**Recommended Tool**: Create COM usage audit script
```python
# audit_com_usage.py
# Scans all EXE projects for COM usage indicators
# Outputs detailed report with evidence
```

### Phase 2: Manifest Implementation (2-3 hours)
For each EXE identified as using COM in Phase 1:

- [ ] **Task 2.1**: Add BuildInclude.targets file
  ```xml
  <!-- Src/<Project>/BuildInclude.targets -->
  <Project>
    <Import Project="..\..\Build\RegFree.targets" />
  </Project>
  ```

- [ ] **Task 2.2**: Import BuildInclude.targets in .csproj
  ```xml
  <!-- Add near end of <Project> element -->
  <Import Project="BuildInclude.targets" />
  ```

- [ ] **Task 2.3**: Set EnableRegFreeCom property
  ```xml
  <PropertyGroup>
    <EnableRegFreeCom>true</EnableRegFreeCom>
  </PropertyGroup>
  ```

- [ ] **Task 2.4**: Build and verify manifest generated
  ```powershell
  msbuild <Project>.csproj /p:Configuration=Debug /p:Platform=x64
  # Check Output/Debug/<ExeName>.exe.manifest exists
  ```

**Per-EXE Checklist**:

#### LCMBrowser.exe (if uses COM)
- [ ] Add `Src/LCMBrowser/BuildInclude.targets`
- [ ] Update `LCMBrowser.csproj` to import BuildInclude.targets
- [ ] Set `<EnableRegFreeCom>true</EnableRegFreeCom>`
- [ ] Build and verify manifest generated
- [ ] Test on clean VM

#### UnicodeCharEditor.exe (if uses COM)
- [ ] Add `Src/UnicodeCharEditor/BuildInclude.targets`
- [ ] Update `UnicodeCharEditor.csproj` to import BuildInclude.targets
- [ ] Set `<EnableRegFreeCom>true</EnableRegFreeCom>`
- [ ] Build and verify manifest generated
- [ ] Test on clean VM

#### MigrateSqlDbs.exe (if uses COM)
- [ ] Add `Src/MigrateSqlDbs/BuildInclude.targets`
- [ ] Update `MigrateSqlDbs.csproj` to import BuildInclude.targets
- [ ] Set `<EnableRegFreeCom>true</EnableRegFreeCom>`
- [ ] Build and verify manifest generated
- [ ] Test on clean VM

#### FixFwData.exe (if uses COM)
- [ ] Add `Src/Utilities/FixFwData/BuildInclude.targets`
- [ ] Update `FixFwData.csproj` to import BuildInclude.targets
- [ ] Set `<EnableRegFreeCom>true</EnableRegFreeCom>`
- [ ] Build and verify manifest generated
- [ ] Test on clean VM

#### FxtExe.exe (if uses COM)
- [ ] Add `Src/FXT/FxtExe/BuildInclude.targets`
- [ ] Update `FxtExe.csproj` to import BuildInclude.targets
- [ ] Set `<EnableRegFreeCom>true</EnableRegFreeCom>`
- [ ] Build and verify manifest generated
- [ ] Test on clean VM

#### ConverterConsole/Converter.exe (if uses COM)
- [ ] Add `Lib/src/Converter/BuildInclude.targets`
- [ ] Update `ConverterConsole.csproj` / `Converter.csproj` to import BuildInclude.targets
- [ ] Set `<EnableRegFreeCom>true</EnableRegFreeCom>`
- [ ] Build and verify manifest generated
- [ ] Test on clean VM

**Recommended Tool**: Create manifest implementation script
```python
# add_regfree_manifest.py
# Input: List of EXEs from Phase 1 audit
# For each EXE:
#   - Create BuildInclude.targets
#   - Update .csproj
#   - Set EnableRegFreeCom property
```

### Phase 3: Testing and Validation (2-3 hours)
- [ ] **Task 3.1**: For each EXE with new manifest:
  - [ ] Inspect manifest file
    ```powershell
    Get-Content Output/Debug/<ExeName>.exe.manifest
    ```
  - [ ] Verify COM classes present
  - [ ] Verify dependency assemblies listed

- [ ] **Task 3.2**: Test on clean VM (no COM registration)
  - [ ] Set up Windows VM without FieldWorks installed
  - [ ] Copy EXE + manifest + dependencies
  - [ ] Run EXE, verify no `REGDB_E_CLASSNOTREG` errors
  - [ ] Test basic functionality

- [ ] **Task 3.3**: Test on dev machine (with COM registration)
  - [ ] Verify EXE still works (backward compatibility)
  - [ ] Manifest should take precedence over registry

- [ ] **Task 3.4**: Installer integration
  - [ ] Verify manifests included in installer
  - [ ] Test installation on clean VM
  - [ ] Verify all EXEs work post-install

**Validation Script**:
```powershell
# validate_regfree_manifests.ps1
# For each EXE:
#   1. Check manifest exists
#   2. Parse manifest XML
#   3. Verify COM classes present
#   4. List any missing components
```

### Phase 4: Documentation (1 hour)
- [ ] **Task 4.1**: Update SDK-MIGRATION.md
  - Add completion status for RegFree COM coverage
  - List all EXEs with manifests
  - Document EXEs that don't need manifests and why

- [ ] **Task 4.2**: Update Docs/64bit-regfree-migration.md
  - Mark COM manifest generation as complete
  - Add testing procedures
  - Add troubleshooting section

- [ ] **Task 4.3**: Create COM usage reference document
  ```markdown
  # COM Usage by EXE

  | EXE                   | Uses COM | Manifest | COM Components Used  |
  | --------------------- | -------- | -------- | -------------------- |
  | FieldWorks.exe        | Yes      | ✅        | FwKernel, Views, ... |
  | LCMBrowser.exe        | TBD      | ❌        | TBA                  |
  | UnicodeCharEditor.exe | TBD      | ❌        | TBA                  |
  | MigrateSqlDbs.exe     | TBD      | ❌        | TBA                  |
  | FixFwData.exe         | TBD      | ❌        | TBA                  |
  | FxtExe.exe            | TBD      | ❌        | TBA                  |
  | ConvertSFM.exe        | TBD      | ❌        | TBA                  |
  ```

### Phase 5: Installer Updates (1 hour)
- [ ] **Task 5.1**: Update FLExInstaller to include new manifests
  ```xml
  <!-- FLExInstaller/CustomComponents.wxi -->
  <Component Id="LCMBrowserManifest" Guid="...">
    <File Source="$(var.OutputPath)\LCMBrowser.exe.manifest" />
  </Component>
  ```

- [ ] **Task 5.2**: Build base installer
  ```powershell
  msbuild Build/Orchestrator.proj /t:BuildBaseInstaller /p:config=release
  ```

- [ ] **Task 5.3**: Test installer on clean VM
  - Install FieldWorks
  - Verify all manifests present in install directory
  - Run each EXE, verify no COM errors

---

## Python Script Recommendations

### Script 1: COM Usage Audit Script
**File**: `audit_com_usage.py`

**Purpose**: Systematically scan all EXE projects for COM usage

**Inputs**: None (scans repository)

**Outputs**:
- `com_usage_report.csv` with columns: EXE, Path, UsesCOM, Evidence, Priority
- `com_usage_detailed.txt` with grep output for each EXE

**Key Logic**:
```python
def detect_com_usage(project_dir):
    """Scan C# files for COM usage indicators"""
    indicators = {
        'DllImport_Ole32': 0,
        'ComImport_Attribute': 0,
        'CoClass_Attribute': 0,
        'IDispatch_Interface': 0,
        'RCW_Usage': 0,
        'Interop_Namespace': 0,
        'FwKernel_Reference': 0,
        'Views_Reference': 0
    }

    for cs_file in glob.glob(f"{project_dir}/**/*.cs", recursive=True):
        with open(cs_file, 'r') as f:
            content = f.read()

        if 'DllImport' in content and 'ole32' in content:
            indicators['DllImport_Ole32'] += 1
        if '[ComImport]' in content or '[ComImport(' in content:
            indicators['ComImport_Attribute'] += 1
        if '[CoClass' in content:
            indicators['CoClass_Attribute'] += 1
        if 'IDispatch' in content:
            indicators['IDispatch_Interface'] += 1
        if 'RCW' in content or 'Runtime Callable Wrapper' in content:
            indicators['RCW_Usage'] += 1
        if 'using System.Runtime.InteropServices' in content:
            indicators['Interop_Namespace'] += 1
        if 'FwKernel' in content:
            indicators['FwKernel_Reference'] += 1
        if 'Views.' in content or 'using Views' in content:
            indicators['Views_Reference'] += 1

    # Determine if COM is used
    uses_com = (
        indicators['DllImport_Ole32'] > 0 or
        indicators['ComImport_Attribute'] > 0 or
        indicators['CoClass_Attribute'] > 0 or
        (indicators['FwKernel_Reference'] > 5 and indicators['Views_Reference'] > 5)
    )

    return uses_com, indicators
```

**Usage**:
```bash
python audit_com_usage.py
# Outputs: com_usage_report.csv, com_usage_detailed.txt
# Review report, prioritize EXEs for manifest implementation
```

---

### Script 2: Manifest Implementation Script
**File**: `add_regfree_manifest.py`

**Purpose**: Automate addition of RegFree manifest support

**Inputs**: `com_usage_decisions.csv` (from Script 1, with manual "add manifest" column)

**Outputs**: Modified project files

**Key Logic**:
```python
def add_regfree_support(csproj_path):
    """Add RegFree COM support to project"""
    project_dir = os.path.dirname(csproj_path)

    # 1. Create BuildInclude.targets
    build_include_path = os.path.join(project_dir, 'BuildInclude.targets')
    with open(build_include_path, 'w') as f:
        f.write('''<?xml version="1.0" encoding="utf-8"?>
<Project xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
  <!-- Import RegFree COM manifest generation -->
  <Import Project="..\..\Build\RegFree.targets" />
</Project>
''')

    # 2. Update .csproj to import BuildInclude.targets
    with open(csproj_path, 'r') as f:
        content = f.read()

    # Add EnableRegFreeCom property
    if '<EnableRegFreeCom>' not in content:
        # Find PropertyGroup to add to
        property_group_match = re.search(r'(<PropertyGroup[^>]*>)', content)
        if property_group_match:
            insert_pos = content.find('</PropertyGroup>', property_group_match.end())
            property_line = '    <EnableRegFreeCom>true</EnableRegFreeCom>\n  '
            content = content[:insert_pos] + property_line + content[insert_pos:]

    # Add Import for BuildInclude.targets
    if '<Import Project="BuildInclude.targets"' not in content:
        # Add before closing </Project>
        insert_pos = content.rfind('</Project>')
        import_line = '\n  <Import Project="BuildInclude.targets" />\n'
        content = content[:insert_pos] + import_line + content[insert_pos:]

    with open(csproj_path, 'w') as f:
        f.write(content)

    print(f"✓ Added RegFree support to {os.path.basename(csproj_path)}")
```

**Usage**:
```bash
python add_regfree_manifest.py com_usage_decisions.csv
# For each EXE marked "add manifest":
#   - Creates BuildInclude.targets
#   - Updates .csproj
#   - Backs up original files
```

---

### Script 3: Manifest Validation Script
**File**: `validate_regfree_manifests.py`

**Purpose**: Verify manifest generation and completeness

**Inputs**: List of EXE paths

**Outputs**: Validation report

**Key Logic**:
```python
def validate_manifest(exe_path):
    """Validate that manifest exists and contains expected elements"""
    manifest_path = exe_path + '.manifest'

    if not os.path.exists(manifest_path):
        return False, "Manifest file not found"

    # Parse XML
    tree = ET.parse(manifest_path)
    root = tree.getroot()

    # Check for required elements
    checks = {
        'has_assembly_identity': len(root.findall('.//{urn:schemas-microsoft-com:asm.v1}assemblyIdentity')) > 0,
        'has_file_elements': len(root.findall('.//{urn:schemas-microsoft-com:asm.v1}file')) > 0,
        'has_com_classes': len(root.findall('.//{urn:schemas-microsoft-com:asm.v1}comClass')) > 0,
        'has_typelibs': len(root.findall('.//{urn:schemas-microsoft-com:asm.v1}typelib')) > 0
    }

    if all(checks.values()):
        return True, "Manifest valid"
    else:
        missing = [k for k, v in checks.items() if not v]
        return False, f"Missing elements: {', '.join(missing)}"
```

**Usage**:
```bash
python validate_regfree_manifests.py Output/Debug/*.exe
# Checks each EXE for manifest
# Reports any issues found
```

---

## Success Metrics

**Before**:
- ❌ 1/8 EXEs have manifests (FieldWorks.exe only)
- ❌ Unknown COM usage for 6 EXEs
- ❌ Incomplete self-contained deployment
- ❌ Potential failures on clean systems

**After**:
- ✅ All COM-using EXEs have manifests
- ✅ Complete COM usage audit documented
- ✅ Self-contained deployment achieved
- ✅ All EXEs tested on clean VM
- ✅ Installer includes all manifests

---

## Risk Mitigation

### Risk 1: Missed COM Usage
**Mitigation**: Comprehensive audit with multiple detection methods (grep, reference analysis, runtime testing)

### Risk 2: Manifest Generation Failures
**Mitigation**: Use proven RegFree.targets pattern, test each EXE individually

### Risk 3: Performance Impact
**Mitigation**: Manifests have negligible performance impact, same as registry lookups

### Risk 4: Installer Size Increase
**Mitigation**: Manifest files are small (typically 5-20KB each), minimal size impact

---

## Timeline

**Total Effort**: 6-8 hours over 2 days

| Phase                   | Duration  | Can Parallelize    |
| ----------------------- | --------- | ------------------ |
| Phase 1: COM Audit      | 2-3 hours | Yes (per EXE)      |
| Phase 2: Implementation | 2-3 hours | Yes (per EXE)      |
| Phase 3: Testing        | 2-3 hours | No (requires VM)   |
| Phase 4: Documentation  | 1 hour    | Yes (with Phase 3) |
| Phase 5: Installer      | 1 hour    | No (after Phase 2) |

**Suggested Schedule**:
- Day 1 Morning: Phase 1 (Audit)
- Day 1 Afternoon: Phase 2 (Implementation) + Phase 4 (Documentation)
- Day 2 Morning: Phase 3 (Testing) + Phase 5 (Installer)

---

## Related Documents

- [SDK-MIGRATION.md](SDK-MIGRATION.md) - Main migration documentation
- [Docs/64bit-regfree-migration.md](Docs/64bit-regfree-migration.md) - RegFree COM plan
- [Build Challenges Deep Dive](SDK-MIGRATION.md#build-challenges-deep-dive) - Original analysis

---

*Document Version: 1.0*
*Last Updated: 2025-11-08*
*Status: Ready for Implementation*
