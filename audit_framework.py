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
        # Ensure directory exists
        Path(output_path).parent.mkdir(parents=True, exist_ok=True)

        with open(output_path, "w", newline="") as f:
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
        self.backup_dir = Path("convergence_backups").resolve()
        self.backup_dir.mkdir(exist_ok=True)

    def backup_file(self, file_path):
        """Create backup of file before modification"""
        # Simple backup to same dir with .bak extension for Windows compatibility
        backup_path = file_path.with_suffix(file_path.suffix + ".bak")
        import shutil

        shutil.copy2(file_path, backup_path)
        return backup_path

    @abstractmethod
    def convert_project(self, project_path, **kwargs):
        """Convert a single project - implement in subclass"""
        pass

    def run_conversions(self, projects_csv, dry_run=False):
        """Run conversions based on CSV decisions"""
        with open(projects_csv, "r") as f:
            reader = csv.DictReader(f)
            for row in reader:
                if (
                    row.get("Action") == "Remove" or row.get("Action") == "Convert"
                ):  # Handle both conventions
                    project_path = Path(row["ProjectPath"])
                    if not dry_run:
                        # self.backup_file(project_path) # Skip backup for now to avoid clutter
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
        # Ensure directory exists
        Path(output_path).parent.mkdir(parents=True, exist_ok=True)

        with open(output_path, "w", encoding="utf-8") as f:
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
    tree.write(project_path, encoding="utf-8", xml_declaration=True)


def find_property_value(tree, property_name):
    """Find property value in csproj XML"""
    root = tree.getroot()
    # Handle namespace
    ns = {"": "http://schemas.microsoft.com/developer/msbuild/2003"}
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
        "/v:quiet",
    ]
    result = subprocess.run(cmd, capture_output=True, text=True)
    return result.returncode == 0, result.stderr
