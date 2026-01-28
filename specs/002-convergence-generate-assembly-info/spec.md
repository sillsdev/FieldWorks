# Convergence Path Analysis: GenerateAssemblyInfo Standardization

**Priority**: ⚠️ **HIGH**
**Divergent Approach**: Mixed true/false settings without documented criteria
**Current State**: 52 projects use `false`, 63 use `true` or default
**Impact**: Confusion for developers, inconsistent build behavior, maintenance burden

---

## Current State Analysis

### Statistics
```
Total Projects Analyzed: 115 SDK-style projects
- GenerateAssemblyInfo=true: 35 projects (30%)

- No documented decision criteria for when to use `true` vs `false`
- Some projects with `false` don't have custom attributes (unnecessary setting)
- Some projects with `true` lost custom attributes during migration
- CS0579 duplicate attribute errors occurred during migration due to this inconsistency

### Root Cause
During the initial SDK conversion (commit 2: f1995dac9), the script set `GenerateAssemblyInfo=false` for ALL projects as a conservative approach. Later (commit 7: 053900d3b), some projects were manually changed to `true` to fix CS0579 errors, but without establishing clear criteria.
Per `CLARIFICATIONS-NEEDED.md`, we are no longer pursuing the SDK-first direction. Instead, the convergence target is:

1. **Use `CommonAssemblyInfoTemplate` everywhere.** Every managed project must import the shared template so that common attributes (product, company, copyright, trademark, version placeholders) live in one location.
2. **Disable SDK auto-generation when the template/custom files are present.** Set `<GenerateAssemblyInfo>false</GenerateAssemblyInfo>` (with an explanatory XML comment) to ensure the SDK does not emit duplicate attributes.
3. **Preserve and restore project-specific `AssemblyInfo` files.** Any project that owned custom attributes before the migration must keep that file. If the file was deleted during the SDK move, restore it from git history and ensure it still compiles.
4. **Document exceptions.** If a project truly needs no project-specific attributes, explicitly state that decision inside the project file so future edits have context.

This recommendation supersedes the earlier Path A guidance and keeps the benefits of a centralized template while protecting bespoke metadata.

---

## Clarifications

### Session 2025-11-14
- Q: How should projects consume `CommonAssemblyInfoTemplate` to keep the shared attributes in sync? → A: Link the generated `Src/CommonAssemblyInfo.cs` into each project via `<Compile Include="..\\..\\CommonAssemblyInfo.cs" Link="Properties\\CommonAssemblyInfo.cs" />` so tooling keeps a single authoritative file.

---

## Clarification Requirements Summary

---

## Implementation Checklist

### Phase 1: Analysis (2 hours)
- [ ] **Task 1.1**: Inventory every managed project and capture:
  - Whether it currently imports `CommonAssemblyInfoTemplate`
  - Whether a project-specific `AssemblyInfo*.cs` file exists
  - Current `GenerateAssemblyInfo` value and any inline comments

- [ ] **Task 1.2**: Diff the inventory against pre-migration history (e.g., `git log -- src/.../AssemblyInfo.cs`) to identify files that were deleted and must be restored.

- [ ] **Task 1.3**: Categorize projects for remediation:
  - Category T: Template import present, no custom file ever existed
  - Category C: Template import present and custom file exists/needs restoration
  - Category G: Template missing or GenerateAssemblyInfo currently `true` (needs correction)

- [ ] **Task 1.4**: Review any ambiguous cases with the team (e.g., projects that swapped to SDK attributes intentionally) before editing.

**Recommended Tool**: Create audit script
```python
# audit_generate_assembly_info.py
# Scans all projects, detects template imports, and compares against git history
# Outputs CSV: Project, TemplateImported, HasAssemblyInfoCs, WasAssemblyInfoDeleted, GenerateAssemblyInfoValue
```

### Phase 2: Template Reintegration & Restoration (3-4 hours)
- [ ] **Task 2.1**: Ensure every project imports `CommonAssemblyInfoTemplate`. Add the `Import` if missing and confirm the relative path works for all project locations.
- [ ] **Task 2.2**: Force `<GenerateAssemblyInfo>false</GenerateAssemblyInfo>` (with a short XML comment referencing the template) in any project that imports the template or ships a custom AssemblyInfo file.
- [ ] **Task 2.3**: Restore deleted custom `AssemblyInfo*.cs` files from pre-migration history. Keep original namespaces, `[assembly: ...]` declarations, and conditional compilation blocks.
- [ ] **Task 2.4**: For fresh projects that never had custom attributes, evaluate whether the template alone is sufficient. If so, keep only the template import; if not, add a minimal per-project AssemblyInfo file with the delta attributes.
- [ ] **Task 2.5**: Normalize `Compile Include` / `Link` entries so every custom AssemblyInfo file is compiled exactly once (e.g., via `Compile Include="Properties\\AssemblyInfo.Project.cs"`). Always link the generated `Src\\CommonAssemblyInfo.cs` into each project using `<Compile Include="..\\..\\CommonAssemblyInfo.cs" Link="Properties\\CommonAssemblyInfo.cs" />` to keep the shared template consistent.

**Recommended Tool**: Create conversion + restoration script
```python
# convert_generate_assembly_info.py
# New responsibilities:
#   - Insert the CommonAssemblyInfoTemplate import when missing
#   - Flip GenerateAssemblyInfo to false with explanatory comments
#   - Restore deleted AssemblyInfo files via `git show <sha>:<path>`
#   - Ensure restored files are part of the Compile item group
```
- [ ] **Task 3.1**: Update `Directory.Build.props` with explicit guidance on template usage, version stamping responsibilities, and the requirement to keep GenerateAssemblyInfo disabled when importing the template.
- [ ] **Task 3.2**: Update `.github/instructions/managed.instructions.md` to describe the “template + custom file” policy and the process for restoring deleted files.
### Phase 4: Validation (1-2 hours)
- [ ] **Task 4.1**: Run Debug/Release builds to confirm no CS0579 duplicate attribute warnings remain.
- [ ] **Task 4.2**: Write a quick validation script that enumerates all project files and asserts:
  - `CommonAssemblyInfoTemplate` import exists
  - `GenerateAssemblyInfo` equals `false`
  - Any project referencing a custom AssemblyInfo file has that file on disk
- [ ] **Task 4.3**: Spot-check restored assemblies with reflection (`GetCustomAttributes`) to ensure custom metadata reappears.
- [ ] **Task 4.4**: Execute the standard test suite to confirm nothing in tooling/test harnesses broke due to restored files.

### Phase 5: Review and Merge (1 hour)
- [ ] **Task 5.1**: During code review, verify template imports, `GenerateAssemblyInfo` settings, and restored files per project.
- [ ] **Task 5.2**: Capture final counts (number of projects with template-only vs. template+custom) in this spec for future tracking.
- [ ] **Task 5.3**: File follow-up issues for any projects still pending manual decisions (e.g., unresolved conflicts between old and new attributes).

---

## Python Script Recommendations

### Script 1: Audit Script
**File**: `audit_generate_assembly_info.py`

**Purpose**: Analyze all projects and their AssemblyInfo.cs files

**Inputs**: None (scans repository)

**Outputs**: CSV file with columns:
- ProjectPath
- ProjectName
- GenerateAssemblyInfo (current value)
- HasAssemblyInfoCs (bool)
- CustomAttributes (list)
- RecommendedAction (ConvertToTrue, KeepFalse, ManualReview)
- Reason

**Key Logic**:
```python
def analyze_assembly_info(assembly_info_path):
    """Parse AssemblyInfo.cs and identify custom attributes"""
    with open(assembly_info_path, 'r') as f:
        content = f.read()

    custom_attrs = []

    # Check for custom Company/Copyright/Trademark
    if 'AssemblyCompany' in content and 'SIL' not in content:
        custom_attrs.append('CustomCompany')
    if 'AssemblyCopyright' in content and 'SIL' not in content:
        custom_attrs.append('CustomCopyright')
    if 'AssemblyTrademark' in content:
        custom_attrs.append('CustomTrademark')

    # Check for conditional compilation
    if '#if' in content or '#ifdef' in content:
        custom_attrs.append('ConditionalCompilation')

    # Check for custom CLSCompliant
    if 'CLSCompliant(false)' in content:
        custom_attrs.append('CustomCLSCompliant')

    return custom_attrs

def recommend_action(has_assembly_info, custom_attrs, current_value):
    """Determine recommended action based on analysis"""
    if not has_assembly_info and current_value == 'false':
        return 'ConvertToTrue', 'No AssemblyInfo.cs file present'

    if has_assembly_info and len(custom_attrs) == 0:
        return 'ConvertToTrue', 'No custom attributes found'

    if len(custom_attrs) > 0:
        return 'KeepFalse', f'Custom attributes: {", ".join(custom_attrs)}'

    return 'ManualReview', 'Uncertain - needs human review'
```

**Key flags**:
- `--release-ref origin/release/9.3` (default) controls which baseline branch/tag is compared when deciding whether a project's custom AssemblyInfo files existed before the migration.
- `--skip-history` disables git lookups when you only need a structural scan.

**Outputs**:
- `generate_assembly_info_audit.csv` now includes `release_ref_has_custom_files`, `latest_custom_commit_date`, `latest_custom_commit_sha`, and `assembly_info_details` (semicolon-delimited entries such as `Properties/AssemblyInfo.cs|release=present|commit=abcd1234@2025-11-15|author=J. Dev`).
- Optional `generate_assembly_info_audit.json` mirrors these properties via the `assembly_info_files` payload, enabling downstream automation to reason about provenance.

**Usage**:
```bash
python audit_generate_assembly_info.py
# Outputs: generate_assembly_info_audit.csv
# Review CSV, adjust recommendations, save as decisions.csv
```

---

### Script 2: Restoration Script
**File**: `convert_generate_assembly_info.py`

**Purpose**: Enforce the template policy, toggle GenerateAssemblyInfo, and restore missing files.

**Inputs**: `decisions.csv` (from Script 1) plus optional map of `<Project, DeletedAssemblyInfoPath, RestoreSha>` entries.

**Outputs**: Updated `.csproj` files and recovered `AssemblyInfo` sources.

**Key Logic**:
```python
def ensure_template_import(csproj_xml):
    if 'CommonAssemblyInfoTemplate' not in csproj_xml:
        insert_index = csproj_xml.index('</Project>')
        include = '\n  <Import Project="$(MSBuildThisFileDirectory)..\\CommonAssemblyInfoTemplate.props" />\n'
        return csproj_xml[:insert_index] + include + csproj_xml[insert_index:]
    return csproj_xml

def enforce_generate_false(tree):
    prop = tree.find('.//GenerateAssemblyInfo')
    if prop is None:
        prop = ET.SubElement(tree.find('.//PropertyGroup'), 'GenerateAssemblyInfo')
    prop.text = 'false'
    prop.addprevious(ET.Comment('Using CommonAssemblyInfoTemplate; prevent SDK duplication'))

def restore_assembly_info(path_on_disk, git_sha):
    if path_on_disk.exists():
        return
    restored = subprocess.check_output(['git', 'show', f'{git_sha}:{path_on_disk.as_posix()}'])
    path_on_disk.write_bytes(restored)
```

**Usage**:
```bash
python convert_generate_assembly_info.py decisions.csv --restore-map restore.json
# Adds missing imports, flips GenerateAssemblyInfo, restores deleted files, and reports any projects still lacking metadata
```

---

### Script 3: Validation Script
**File**: `validate_generate_assembly_info.py`

**Purpose**: Assert that every project now complies with the template policy.

**Inputs**: None (scans repository).

**Outputs**: `validation_report.txt` summarizing violations.

**Checks**:
1. `CommonAssemblyInfoTemplate` import present in each managed `.csproj`.
2. `<GenerateAssemblyInfo>false</GenerateAssemblyInfo>` exists with an adjacent comment referencing the template.
3. Projects enumerated in the audit as having custom attributes have on-disk `AssemblyInfo*.cs` files and corresponding `<Compile Include>` entries.
4. No project keeps `GenerateAssemblyInfo=true` unless explicitly approved (should be zero).
5. Build log free of CS0579 warnings.

**Usage**:
```bash
python validate_generate_assembly_info.py
# Outputs: validation_report.txt and returns non-zero if any violation is detected
```

---

## Success Metrics

**Before**:
- ❌ `CommonAssemblyInfoTemplate` imported inconsistently (mixed SDK/manual generation)
- ❌ Custom AssemblyInfo files deleted during migration
- ❌ Conflicting `GenerateAssemblyInfo` values leading to CS0579 duplicates
- ❌ Limited traceability for why certain projects deviated
- **Counts (Audit)**: Template-only: 25, Template+Custom: 76, NeedsFix: 0 (Baseline scan)

**After**:
- ✅ Every managed project imports the template (single source of common attributes)
- ✅ `GenerateAssemblyInfo=false` everywhere the template/custom files apply, with inline explanation
- ✅ All historic custom AssemblyInfo files restored or explicitly documented as intentionally absent
- ✅ CS0579 duplicate attribute errors eliminated
- ✅ Audit spreadsheet + validation script provide ongoing compliance signal

**Validation Artifacts**:
- [Validation Report](Output/GenerateAssemblyInfo/validation_report.txt)
- [Build Metrics](Output/GenerateAssemblyInfo/build-metrics.json)
- [Reflection Log](Output/GenerateAssemblyInfo/reflection.log)
- [Test Results](Output/GenerateAssemblyInfo/tests/)

---

## Risk Mitigation

### Risk 1: Version Stamping Breaks
**Mitigation**: Test installer build, verify version appears correctly

### Risk 2: Missing Assembly Attributes
**Mitigation**: Validate with reflection script, check all critical attributes present

### Risk 3: CI/CD Pipeline Failures
**Mitigation**: Test in CI before merging, have rollback plan

### Risk 4: Developer Confusion
**Mitigation**: Clear documentation, examples, code review checklist

---

## Timeline

**Total Effort**: 8-10 hours over 2-3 days

| Phase                                       | Duration  | Status             |
| ------------------------------------------- | --------- | ------------------ |
| Phase 1: Analysis (inventory + history)     | 2 hours   | **Complete**       |
| Phase 2: Template reintegration/restoration | 3-4 hours | **Complete**       |
| Phase 3: Documentation updates              | 1 hour    | **In Progress**    |
| Phase 4: Validation suite                   | 1-2 hours | **Complete**       |
| Phase 5: Review & reporting                 | 1 hour    | Pending            |

**Suggested Schedule**:
- Day 1 Morning: Phase 1 (Analysis)
- Day 1 Afternoon: Phase 2 (Conversion) + Phase 3 (Documentation)
- Day 2 Morning: Phase 4 (Validation)
- Day 2 Afternoon: Phase 5 (Review and Merge)

---

## Related Documents

- [SDK-MIGRATION.md](SDK-MIGRATION.md) - Main migration documentation
- [.github/instructions/managed.instructions.md](.github/instructions/managed.instructions.md) - Managed code guidelines
- [Build Challenges Deep Dive](SDK-MIGRATION.md#build-challenges-deep-dive) - Original analysis

---

*Document Version: 1.0*
*Last Updated: 2025-11-14*
*Status: Ready for Implementation*
