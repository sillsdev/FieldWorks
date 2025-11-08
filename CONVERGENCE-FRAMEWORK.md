# SDK Migration Convergence Framework

**Purpose**: Unified framework for addressing divergent approaches identified during SDK migration

**Architecture**: DRY (Don't Repeat Yourself) with shared tooling, processes, and templates

---

## Overview

This document provides the **shared framework** for all convergence efforts. Each specific convergence (GenerateAssemblyInfo, RegFree COM, etc.) uses this framework to avoid duplication.

### Framework Components

```
CONVERGENCE-FRAMEWORK.md (this file)
├── Shared Processes
│   ├── Analysis Phase Template
│   ├── Implementation Phase Template
│   ├── Validation Phase Template
│   └── Documentation Phase Template
├── Shared Tooling
│   ├── Base Audit Script (audit_framework.py)
│   ├── Base Conversion Script (convert_framework.py)
│   └── Base Validation Script (validate_framework.py)
└── Convergence-Specific Docs
    ├── CONVERGENCE-1-GenerateAssemblyInfo.md (uses framework)
    ├── CONVERGENCE-2-RegFreeCOM.md (uses framework)
    ├── CONVERGENCE-3-TestExclusionPatterns.md (uses framework)
    ├── CONVERGENCE-4-PrivateAssets.md (uses framework)
    └── CONVERGENCE-5-PlatformTarget.md (uses framework)
```

---

## Shared Process Template

### Universal 5-Phase Approach

All convergence efforts follow this consistent structure:

#### Phase 1: Analysis (20% of effort)
**Inputs**: Current codebase  
**Outputs**: Audit report with current state + recommendations  
**Activities**:
1. Scan repository for current patterns
2. Categorize approaches (A, B, C, etc.)
3. Count occurrences
4. Identify risks and issues
5. Recommend convergence path

**Template Checklist**:
- [ ] Run audit script → generate CSV
- [ ] Analyze statistics
- [ ] Document patterns found
- [ ] Identify risks
- [ ] Recommend path (with rationale)

---

#### Phase 2: Implementation (40% of effort)
**Inputs**: Audit report + convergence decision  
**Outputs**: Modified project files  
**Activities**:
1. Apply convergence pattern to projects
2. Handle edge cases manually
3. Create backups before changes
4. Validate changes incrementally

**Template Checklist**:
- [ ] Review audit CSV, mark projects for conversion
- [ ] Run conversion script (batch where safe)
- [ ] Handle edge cases manually
- [ ] Build each modified project
- [ ] Fix any errors

---

#### Phase 3: Validation (20% of effort)
**Inputs**: Modified projects  
**Outputs**: Validation report  
**Activities**:
1. Build all projects (Debug + Release)
2. Run validation script
3. Execute test suites
4. Check for regressions

**Template Checklist**:
- [ ] Full clean build
- [ ] Run validation script
- [ ] Check for specific errors (varies by convergence)
- [ ] Run affected tests
- [ ] Compare before/after metrics

---

#### Phase 4: Documentation (10% of effort)
**Inputs**: Completed changes  
**Outputs**: Updated documentation  
**Activities**:
1. Update main docs (SDK-MIGRATION.md, etc.)
2. Add guidelines for future projects
3. Update templates
4. Add examples

**Template Checklist**:
- [ ] Update SDK-MIGRATION.md with completion status
- [ ] Add pattern to Directory.Build.props (if applicable)
- [ ] Update .github/instructions/*.md
- [ ] Create/update project templates
- [ ] Add examples to docs

---

#### Phase 5: Ongoing Maintenance (10% of effort)
**Inputs**: Established pattern  
**Outputs**: Enforcement mechanisms  
**Activities**:
1. Add validation to CI
2. Create pre-commit hooks
3. Update code review checklist

**Template Checklist**:
- [ ] Add CI validation step
- [ ] Create pre-commit hook (optional)
- [ ] Update code review checklist
- [ ] Document troubleshooting

---

## Shared Python Tooling Architecture

### Base Classes (audit_framework.py)

```python
#!/usr/bin/env python3
"""
Shared framework for SDK migration convergence efforts.
Provides base classes that specific convergences extend.
"""

import os
import re
import xml.etree.ElementTree as ET
import csv
from pathlib import Path
from abc import ABC, abstractmethod

class ConvergenceAuditor(ABC):
    """Base class for auditing current state"""
    
    def __init__(self, repo_root):
        self.repo_root = Path(repo_root)
        self.projects = []
        self.results = []
    
    def find_projects(self, pattern="**/*.csproj"):
        """Find all project files matching pattern"""
        self.projects = list(self.repo_root.glob(pattern))
        return self.projects
    
    @abstractmethod
    def analyze_project(self, project_path):
        """Analyze a single project - implement in subclass"""
        pass
    
    def run_audit(self):
        """Run audit on all projects"""
        self.find_projects()
        for project in self.projects:
            result = self.analyze_project(project)
            if result:
                self.results.append(result)
        return self.results
    
    def export_csv(self, output_path):
        """Export results to CSV"""
        if not self.results:
            return
        
        keys = self.results[0].keys()
        with open(output_path, 'w', newline='') as f:
            writer = csv.DictWriter(f, fieldnames=keys)
            writer.writeheader()
            writer.writerows(self.results)
        
        print(f"✓ Audit results exported to {output_path}")
    
    def print_summary(self):
        """Print summary statistics"""
        if not self.results:
            print("No results to summarize")
            return
        
        print(f"\n{'='*60}")
        print(f"AUDIT SUMMARY")
        print(f"{'='*60}")
        print(f"Total Projects Analyzed: {len(self.results)}")
        self.print_custom_summary()
    
    @abstractmethod
    def print_custom_summary(self):
        """Print convergence-specific summary - implement in subclass"""
        pass


class ConvergenceConverter(ABC):
    """Base class for converting projects"""
    
    def __init__(self, repo_root):
        self.repo_root = Path(repo_root)
        self.backup_dir = Path("/tmp/convergence_backups")
        self.backup_dir.mkdir(exist_ok=True)
    
    def backup_file(self, file_path):
        """Create backup of file before modification"""
        backup_path = self.backup_dir / file_path.name
        import shutil
        shutil.copy2(file_path, backup_path)
        return backup_path
    
    @abstractmethod
    def convert_project(self, project_path, **kwargs):
        """Convert a single project - implement in subclass"""
        pass
    
    def run_conversions(self, projects_csv, dry_run=False):
        """Run conversions based on CSV decisions"""
        with open(projects_csv, 'r') as f:
            reader = csv.DictReader(f)
            for row in reader:
                if row.get('Action') == 'Convert':
                    project_path = Path(row['ProjectPath'])
                    if not dry_run:
                        self.backup_file(project_path)
                        self.convert_project(project_path, **row)
                    else:
                        print(f"[DRY RUN] Would convert: {project_path.name}")


class ConvergenceValidator(ABC):
    """Base class for validating convergence"""
    
    def __init__(self, repo_root):
        self.repo_root = Path(repo_root)
        self.violations = []
    
    @abstractmethod
    def validate_project(self, project_path):
        """Validate a single project - implement in subclass"""
        pass
    
    def run_validation(self):
        """Run validation on all projects"""
        projects = list(self.repo_root.glob("**/*.csproj"))
        for project in projects:
            violations = self.validate_project(project)
            if violations:
                self.violations.extend(violations)
        return self.violations
    
    def print_report(self):
        """Print validation report"""
        if not self.violations:
            print("\n✅ All projects pass validation!")
            return 0
        
        print(f"\n❌ Found {len(self.violations)} violation(s):")
        for violation in self.violations:
            print(f"  - {violation}")
        return 1
    
    def export_report(self, output_path):
        """Export validation report to file"""
        with open(output_path, 'w') as f:
            if not self.violations:
                f.write("✅ All projects pass validation!\n")
            else:
                f.write(f"❌ Found {len(self.violations)} violation(s):\n\n")
                for violation in self.violations:
                    f.write(f"  - {violation}\n")


# Utility functions shared across convergences

def parse_csproj(project_path):
    """Parse .csproj XML file"""
    tree = ET.parse(project_path)
    return tree

def update_csproj(project_path, tree):
    """Save modified .csproj XML"""
    tree.write(project_path, encoding='utf-8', xml_declaration=True)

def find_property_value(tree, property_name):
    """Find property value in csproj XML"""
    root = tree.getroot()
    # Handle namespace
    ns = {'': 'http://schemas.microsoft.com/developer/msbuild/2003'}
    elem = root.find(f".//PropertyGroup/{property_name}", ns)
    if elem is not None:
        return elem.text
    # Try without namespace (SDK-style projects)
    elem = root.find(f".//PropertyGroup/{property_name}")
    return elem.text if elem is not None else None

def set_property_value(tree, property_name, value):
    """Set property value in csproj XML"""
    root = tree.getroot()
    # Find or create PropertyGroup
    prop_group = root.find(".//PropertyGroup")
    if prop_group is None:
        prop_group = ET.SubElement(root, "PropertyGroup")
    
    # Find or create property
    prop = prop_group.find(property_name)
    if prop is None:
        prop = ET.SubElement(prop_group, property_name)
    
    prop.text = value

def build_project(project_path, configuration="Debug", platform="x64"):
    """Build a project and return success status"""
    import subprocess
    cmd = [
        "msbuild",
        str(project_path),
        f"/p:Configuration={configuration}",
        f"/p:Platform={platform}",
        "/v:quiet"
    ]
    result = subprocess.run(cmd, capture_output=True, text=True)
    return result.returncode == 0, result.stderr
```

---

### Specific Convergence Implementation Pattern

Each convergence extends the base classes:

```python
#!/usr/bin/env python3
"""
Example: GenerateAssemblyInfo Convergence
Uses shared framework classes
"""

from audit_framework import ConvergenceAuditor, ConvergenceConverter, ConvergenceValidator
from audit_framework import parse_csproj, find_property_value, set_property_value

class GenerateAssemblyInfoAuditor(ConvergenceAuditor):
    """Audit GenerateAssemblyInfo settings"""
    
    def analyze_project(self, project_path):
        """Analyze GenerateAssemblyInfo setting"""
        tree = parse_csproj(project_path)
        value = find_property_value(tree, "GenerateAssemblyInfo")
        
        # Check for AssemblyInfo.cs
        assembly_info_path = project_path.parent / "AssemblyInfo.cs"
        has_assembly_info = assembly_info_path.exists()
        
        # Analyze custom attributes if exists
        custom_attrs = []
        if has_assembly_info:
            custom_attrs = self._find_custom_attributes(assembly_info_path)
        
        return {
            'ProjectPath': str(project_path),
            'ProjectName': project_path.stem,
            'GenerateAssemblyInfo': value or 'default(true)',
            'HasAssemblyInfoCs': has_assembly_info,
            'CustomAttributes': ','.join(custom_attrs),
            'RecommendedAction': self._recommend_action(value, has_assembly_info, custom_attrs)
        }
    
    def _find_custom_attributes(self, assembly_info_path):
        """Find custom attributes in AssemblyInfo.cs"""
        with open(assembly_info_path, 'r') as f:
            content = f.read()
        
        custom = []
        if 'AssemblyCompany' in content:
            custom.append('Company')
        if 'AssemblyCopyright' in content:
            custom.append('Copyright')
        if 'AssemblyTrademark' in content:
            custom.append('Trademark')
        # ... more checks
        
        return custom
    
    def _recommend_action(self, value, has_file, custom_attrs):
        """Recommend action based on analysis"""
        if value == 'false' and not custom_attrs:
            return 'ConvertToTrue'
        elif value == 'true' and has_file:
            return 'DeleteAssemblyInfo'
        elif value == 'false' and custom_attrs:
            return 'KeepFalse'
        else:
            return 'NoAction'
    
    def print_custom_summary(self):
        """Print summary specific to GenerateAssemblyInfo"""
        actions = {}
        for result in self.results:
            action = result['RecommendedAction']
            actions[action] = actions.get(action, 0) + 1
        
        print("\nRecommended Actions:")
        for action, count in actions.items():
            print(f"  {action}: {count} projects")


class GenerateAssemblyInfoConverter(ConvergenceConverter):
    """Convert projects to standardized GenerateAssemblyInfo"""
    
    def convert_project(self, project_path, **kwargs):
        """Convert project to GenerateAssemblyInfo=true"""
        action = kwargs.get('RecommendedAction')
        
        if action == 'ConvertToTrue':
            self._convert_to_true(project_path)
        elif action == 'DeleteAssemblyInfo':
            self._delete_assembly_info(project_path)
        # ... handle other actions


# Usage:
if __name__ == '__main__':
    auditor = GenerateAssemblyInfoAuditor('/path/to/repo')
    auditor.run_audit()
    auditor.export_csv('generate_assembly_info_audit.csv')
    auditor.print_summary()
```

---

## Shared Documentation Templates

### Path Analysis Template

**Reusable structure for all convergence path documents**:

```markdown
# Convergence Path Analysis: [Name]

**Priority**: [HIGH/MEDIUM/LOW]
**Current State**: [Description]
**Impact**: [Description]

## Current State Analysis
[Statistics, problem statement, root cause]

## Convergence Path Options

### Path A: [Name] [✅ RECOMMENDED / ⚠️ / ❌]
**Philosophy**: [One sentence]
**Strategy**: [Code example]
**Pros**: [List]
**Cons**: [List]
**Effort**: [Hours]
**Risk**: [LOW/MEDIUM/HIGH]

### Path B: [Name]
[Same structure]

### Path C: [Name]
[Same structure]

## Recommendation: Path [A/B/C]
**Rationale**: [Why chosen]

## Implementation Checklist
[Uses shared 5-phase template from framework]

## Python Script Recommendations
**Script 1**: Audit - extends ConvergenceAuditor
**Script 2**: Convert - extends ConvergenceConverter  
**Script 3**: Validate - extends ConvergenceValidator

## Success Metrics
**Before**: [List]
**After**: [List]

## Timeline
[Uses shared phase durations]
```

---

## Convergence Tracking Dashboard

### Master Tracking Document

Create `CONVERGENCE-STATUS.md` to track all convergences:

```markdown
# SDK Migration Convergence Status

| # | Convergence | Priority | Status | Owner | ETA |
|---|-------------|----------|--------|-------|-----|
| 1 | GenerateAssemblyInfo | HIGH | 🔴 Not Started | TBD | TBD |
| 2 | RegFree COM Coverage | HIGH | 🔴 Not Started | TBD | TBD |
| 3 | Test Exclusion Patterns | MEDIUM | 🔴 Not Started | TBD | TBD |
| 4 | PrivateAssets on Tests | MEDIUM | 🔴 Not Started | TBD | TBD |
| 5 | PlatformTarget Redundancy | LOW | 🔴 Not Started | TBD | TBD |

**Legend**:
- 🔴 Not Started
- 🟡 In Progress (Phase X/5)
- 🟢 Complete
- ⚪ Blocked

## Summary Statistics
- Total Convergences: 5
- Completed: 0
- In Progress: 0
- Not Started: 5
- Total Estimated Effort: 30-40 hours
```

---

## Unified CLI Tool

### Convergence Command-Line Interface

Create `convergence.py` - single entry point for all convergences:

```python
#!/usr/bin/env python3
"""
SDK Migration Convergence Tool
Unified CLI for all convergence efforts
"""

import argparse
import sys

# Import specific convergences
from convergence_generate_assembly_info import GenerateAssemblyInfoAuditor, GenerateAssemblyInfoConverter, GenerateAssemblyInfoValidator
from convergence_regfree_com import RegFreeComAuditor, RegFreeComConverter, RegFreeComValidator
from convergence_test_exclusions import TestExclusionAuditor, TestExclusionConverter, TestExclusionValidator

CONVERGENCES = {
    'generate-assembly-info': {
        'auditor': GenerateAssemblyInfoAuditor,
        'converter': GenerateAssemblyInfoConverter,
        'validator': GenerateAssemblyInfoValidator,
        'description': 'Standardize GenerateAssemblyInfo settings'
    },
    'regfree-com': {
        'auditor': RegFreeComAuditor,
        'converter': RegFreeComConverter,
        'validator': RegFreeComValidator,
        'description': 'Complete RegFree COM coverage'
    },
    'test-exclusions': {
        'auditor': TestExclusionAuditor,
        'converter': TestExclusionConverter,
        'validator': TestExclusionValidator,
        'description': 'Standardize test exclusion patterns'
    }
}

def main():
    parser = argparse.ArgumentParser(
        description='SDK Migration Convergence Tool'
    )
    parser.add_argument(
        'convergence',
        choices=list(CONVERGENCES.keys()) + ['all'],
        help='Which convergence to process'
    )
    parser.add_argument(
        'action',
        choices=['audit', 'convert', 'validate'],
        help='Action to perform'
    )
    parser.add_argument(
        '--repo',
        default='.',
        help='Repository root path'
    )
    parser.add_argument(
        '--decisions',
        help='CSV file with conversion decisions (for convert action)'
    )
    parser.add_argument(
        '--dry-run',
        action='store_true',
        help='Show what would be done without making changes'
    )
    
    args = parser.parse_args()
    
    if args.convergence == 'all':
        convergences = CONVERGENCES.keys()
    else:
        convergences = [args.convergence]
    
    for convergence_name in convergences:
        print(f"\n{'='*60}")
        print(f"Processing: {convergence_name}")
        print(f"Action: {args.action}")
        print(f"{'='*60}\n")
        
        convergence = CONVERGENCES[convergence_name]
        
        if args.action == 'audit':
            auditor = convergence['auditor'](args.repo)
            auditor.run_audit()
            auditor.export_csv(f'{convergence_name}_audit.csv')
            auditor.print_summary()
        
        elif args.action == 'convert':
            if not args.decisions:
                print("Error: --decisions CSV file required for convert action")
                sys.exit(1)
            converter = convergence['converter'](args.repo)
            converter.run_conversions(args.decisions, dry_run=args.dry_run)
        
        elif args.action == 'validate':
            validator = convergence['validator'](args.repo)
            violations = validator.run_validation()
            exit_code = validator.print_report()
            validator.export_report(f'{convergence_name}_validation.txt')
            if exit_code != 0:
                sys.exit(exit_code)

if __name__ == '__main__':
    main()
```

**Usage**:
```bash
# Audit all convergences
python convergence.py all audit

# Audit specific convergence
python convergence.py generate-assembly-info audit

# Convert based on decisions
python convergence.py generate-assembly-info convert --decisions decisions.csv

# Validate convergence
python convergence.py test-exclusions validate

# Dry run conversion
python convergence.py regfree-com convert --decisions decisions.csv --dry-run
```

---

## Benefits of This Architecture

### 1. **DRY (Don't Repeat Yourself)**
- Shared base classes eliminate duplication
- Common processes defined once
- Reusable utilities for all convergences

### 2. **Consistency**
- All convergences follow same structure
- Same phase approach (Audit → Convert → Validate)
- Uniform documentation format

### 3. **Maintainability**
- Single place to fix bugs (base classes)
- Easy to add new convergences
- Clear separation of concerns

### 4. **Testability**
- Base classes can be unit tested
- Each convergence tested independently
- Dry-run mode for safe testing

### 5. **User Experience**
- Single CLI tool for all convergences
- Consistent command structure
- Unified reporting format

---

## File Organization

```
/FieldWorks/
├── CONVERGENCE-FRAMEWORK.md (this file - master doc)
├── CONVERGENCE-STATUS.md (tracking dashboard)
├── convergence/
│   ├── __init__.py
│   ├── audit_framework.py (base classes)
│   ├── convergence.py (unified CLI)
│   ├── convergence_generate_assembly_info.py
│   ├── convergence_regfree_com.py
│   ├── convergence_test_exclusions.py
│   ├── convergence_private_assets.py
│   └── convergence_platform_target.py
└── docs/convergence/
    ├── CONVERGENCE-1-GenerateAssemblyInfo.md (refactored - references framework)
    ├── CONVERGENCE-2-RegFreeCOM.md (refactored)
    ├── CONVERGENCE-3-TestExclusionPatterns.md (refactored)
    ├── CONVERGENCE-4-PrivateAssets.md (new - uses framework)
    └── CONVERGENCE-5-PlatformTarget.md (new - uses framework)
```

---

## Migration of Existing Convergence Docs

### Refactor Existing Docs to Use Framework

Each existing convergence doc should be refactored to:
1. Reference framework for shared processes
2. Focus only on convergence-specific details
3. Remove duplicated phase descriptions
4. Use shared script architecture

**Example Refactored Structure**:

```markdown
# Convergence Path Analysis: GenerateAssemblyInfo

[Inherits from CONVERGENCE-FRAMEWORK.md]

## Current State Analysis
[Convergence-specific details only]

## Path Options
[Convergence-specific paths only]

## Implementation
**See**: [CONVERGENCE-FRAMEWORK.md#shared-process-template](CONVERGENCE-FRAMEWORK.md#shared-process-template) for standard 5-phase approach

### Convergence-Specific Checklist Additions
[Only additions/modifications to standard template]

## Python Scripts
**Extends**: ConvergenceAuditor, ConvergenceConverter, ConvergenceValidator from framework

### Convergence-Specific Logic
[Only unique logic, not shared utilities]
```

---

## Next Steps

1. **Create convergence/ directory structure**
2. **Implement base classes** (audit_framework.py)
3. **Create unified CLI** (convergence.py)
4. **Refactor existing convergence docs** to reference framework
5. **Create remaining 2 convergence docs** (PrivateAssets, PlatformTarget) using framework
6. **Create tracking dashboard** (CONVERGENCE-STATUS.md)
7. **Test framework** with one convergence end-to-end

---

*Framework Version: 1.0*  
*Last Updated: 2025-11-08*  
*Status: Design Complete - Ready for Implementation*
