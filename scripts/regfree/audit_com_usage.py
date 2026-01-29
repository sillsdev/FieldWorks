"""
Audit COM usage in FieldWorks projects.
Scans C# source code for indicators of COM interop to identify executables needing RegFree manifests.
"""

import os
import re
import csv
import logging
from dataclasses import dataclass
from pathlib import Path
from typing import List, Tuple, Dict

# Import from common.py in the same directory
try:
    from scripts.regfree.common import iter_executables, REPO_ROOT
except ImportError:
    # Fallback for running directly or from tests without package installation
    import sys

    sys.path.append(str(Path(__file__).resolve().parents[2]))
    from scripts.regfree.common import iter_executables, REPO_ROOT

ARTIFACTS_DIR = (
    REPO_ROOT / "specs" / "003-convergence-regfree-com-coverage" / "artifacts"
)


@dataclass
class ComIndicators:
    dll_import_ole32: int = 0
    com_import_attribute: int = 0
    fw_kernel_reference: int = 0
    views_reference: int = 0
    project_reference_views: int = 0
    package_reference_lcmodel: int = 0

    @property
    def uses_com(self) -> bool:
        return (
            self.dll_import_ole32 > 0
            or self.com_import_attribute > 0
            or self.fw_kernel_reference > 0
            or self.views_reference > 0
            or self.project_reference_views > 0
            or self.package_reference_lcmodel > 0
        )

    def __add__(self, other):
        if not isinstance(other, ComIndicators):
            return NotImplemented
        return ComIndicators(
            self.dll_import_ole32 + other.dll_import_ole32,
            self.com_import_attribute + other.com_import_attribute,
            self.fw_kernel_reference + other.fw_kernel_reference,
            self.views_reference + other.views_reference,
            self.project_reference_views + other.project_reference_views,
            self.package_reference_lcmodel + other.package_reference_lcmodel,
        )


class ProjectAnalyzer:
    def __init__(self):
        self.cache: Dict[Path, ComIndicators] = {}
        self.dependency_cache: Dict[Path, List[Path]] = {}
        self.details_cache: Dict[Path, List[str]] = {}

    def get_project_references(self, csproj_path: Path) -> List[Path]:
        if csproj_path in self.dependency_cache:
            return self.dependency_cache[csproj_path]

        refs = []
        try:
            content = csproj_path.read_text(encoding="utf-8", errors="ignore")
            # Match <ProjectReference Include="..." />
            matches = re.findall(r'<ProjectReference\s+Include="([^"]+)"', content)
            for match in matches:
                # Resolve relative path
                # match is like "../Common/FwUtils/FwUtils.csproj"
                # csproj_path is like "Src/UnicodeCharEditor/UnicodeCharEditor.csproj"
                # Handle windows backslashes in match
                match_path = match.replace("\\", "/")
                ref_path = (csproj_path.parent / match_path).resolve()
                if ref_path.exists():
                    refs.append(ref_path)
        except Exception:
            pass

        self.dependency_cache[csproj_path] = refs
        return refs

    def get_direct_usage(self, csproj_path: Path) -> Tuple[ComIndicators, List[str]]:
        if csproj_path in self.cache:
            return self.cache[csproj_path], self.details_cache[csproj_path]

        indicators, details = scan_project_for_com_usage(csproj_path)
        self.cache[csproj_path] = indicators
        self.details_cache[csproj_path] = details
        return indicators, details

    def analyze_project(
        self, csproj_path: Path, visited: set = None
    ) -> Tuple[ComIndicators, List[str]]:
        if visited is None:
            visited = set()

        if csproj_path in visited:
            return ComIndicators(), []  # Break cycles

        visited.add(csproj_path)

        # 1. Direct usage in this project
        total_indicators, total_details = self.get_direct_usage(csproj_path)

        # 2. Transitive usage
        for ref_path in self.get_project_references(csproj_path):
            ref_indicators, ref_details = self.analyze_project(ref_path, visited)
            if ref_indicators.uses_com:
                total_indicators += ref_indicators
                # We don't necessarily want all the details from dependencies,
                # but we might want to know WHICH dependency caused it.
                if ref_indicators.uses_com:
                    total_details.append(f"Dependency {ref_path.name} uses COM")

        return total_indicators, total_details


def scan_file_for_com_usage(file_path: Path) -> ComIndicators:
    indicators = ComIndicators()
    try:
        content = file_path.read_text(encoding="utf-8", errors="ignore")
    except Exception:
        return indicators

    if file_path.suffix == ".cs":
        # Check for DllImport("ole32.dll")
        if re.search(r'DllImport\s*\(\s*"ole32\.dll"', content, re.IGNORECASE):
            indicators.dll_import_ole32 += 1

        # Check for [ComImport]
        if re.search(r"\[\s*ComImport\s*\]", content):
            indicators.com_import_attribute += 1

        # Check for FwKernel references
        if "FwKernel" in content or "SIL.FieldWorks.FwKernel" in content:
            indicators.fw_kernel_reference += 1

        # Check for Views references
        if "Views" in content or "SIL.FieldWorks.Common.COMInterfaces" in content:
            indicators.views_reference += 1

    elif file_path.suffix == ".csproj":
        # Check for ProjectReference to ViewsInterfaces
        if re.search(r'Include=".*ViewsInterfaces\.csproj"', content):
            indicators.project_reference_views += 1

        # Check for PackageReference to SIL.LCModel
        if re.search(r'Include="SIL\.LCModel"', content):
            indicators.package_reference_lcmodel += 1

    return indicators


def scan_project_for_com_usage(project_path: Path) -> Tuple[ComIndicators, List[str]]:
    total_indicators = ComIndicators()
    details = []

    if not project_path.exists():
        return total_indicators, ["Project path not found"]

    # Walk through the project directory
    # We assume the project path is the directory containing the .csproj
    # If it's a file, use its parent
    search_dir = project_path if project_path.is_dir() else project_path.parent

    # First, check the .csproj file itself if it exists in the search_dir
    for file in search_dir.glob("*.csproj"):
        file_indicators = scan_file_for_com_usage(file)
        if file_indicators.uses_com:
            total_indicators += file_indicators
            details.append(f"{file.name}: {file_indicators}")

    for root, _, files in os.walk(search_dir):
        for file in files:
            if file.endswith(".cs"):
                file_path = Path(root) / file
                file_indicators = scan_file_for_com_usage(file_path)

                if file_indicators.uses_com:
                    total_indicators += file_indicators
                    rel_path = file_path.relative_to(search_dir)
                    details.append(f"{rel_path}: {file_indicators}")

    return total_indicators, details


def main():
    logging.basicConfig(level=logging.INFO, format="%(message)s")
    logger = logging.getLogger("ComAudit")

    ARTIFACTS_DIR.mkdir(parents=True, exist_ok=True)

    csv_path = ARTIFACTS_DIR / "com_usage_report.csv"
    log_path = ARTIFACTS_DIR / "com_usage_detailed.log"

    logger.info(f"Starting COM usage audit...")
    logger.info(f"Artifacts will be saved to {ARTIFACTS_DIR}")

    results = []
    detailed_logs = []

    analyzer = ProjectAnalyzer()

    for exe in iter_executables():
        logger.info(f"Scanning {exe.id} ({exe.project_path})...")

        # The project_path in ExecutableInfo is relative to REPO_ROOT and points to the .csproj file
        full_project_path = REPO_ROOT / exe.project_path

        indicators, details = analyzer.analyze_project(full_project_path)

        results.append(
            {
                "Executable": exe.output_name,
                "Project": exe.id,
                "Priority": exe.priority,
                "UsesCOM": indicators.uses_com,
                "DllImport_Ole32": indicators.dll_import_ole32,
                "ComImport": indicators.com_import_attribute,
                "FwKernel_Ref": indicators.fw_kernel_reference,
                "Views_Ref": indicators.views_reference,
                "ProjRef_Views": indicators.project_reference_views,
                "PkgRef_LCModel": indicators.package_reference_lcmodel,
            }
        )

        if details:
            detailed_logs.append(f"=== {exe.id} ===")
            detailed_logs.extend(details)
            detailed_logs.append("")

    # Write CSV
    with open(csv_path, "w", newline="", encoding="utf-8") as f:
        fieldnames = [
            "Executable",
            "Project",
            "Priority",
            "UsesCOM",
            "DllImport_Ole32",
            "ComImport",
            "FwKernel_Ref",
            "Views_Ref",
            "ProjRef_Views",
            "PkgRef_LCModel",
        ]
        writer = csv.DictWriter(f, fieldnames=fieldnames)
        writer.writeheader()
        writer.writerows(results)

    # Write Log
    with open(log_path, "w", encoding="utf-8") as f:
        f.write("\n".join(detailed_logs))

    logger.info(f"Audit complete. Found {len(results)} executables.")
    logger.info(f"Report: {csv_path}")
    logger.info(f"Log: {log_path}")


if __name__ == "__main__":
    main()
