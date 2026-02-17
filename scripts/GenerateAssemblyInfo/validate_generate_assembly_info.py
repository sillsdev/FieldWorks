"""Validation script to assert CommonAssemblyInfoTemplate compliance."""

from __future__ import annotations

import logging
import sys
import json
import subprocess
import xml.etree.ElementTree as ET
from pathlib import Path
from typing import List, Optional, Dict, Any

from . import cli_args
from .models import ManagedProject, ValidationFinding, RestoreInstruction
from .project_scanner import scan_projects, COMMON_INCLUDE_TOKEN

LOGGER = logging.getLogger(__name__)
MSBUILD_NS = "http://schemas.microsoft.com/developer/msbuild/2003"


def main() -> None:
    parser = cli_args.build_common_parser(
        "Validate managed projects for CommonAssemblyInfoTemplate compliance."
    )
    args = parser.parse_args()

    logging.basicConfig(level=args.log_level)
    repo_root: Path = args.repo_root.resolve()
    output_dir = cli_args.resolve_output_path(args, "validation_report.txt").parent

    LOGGER.info("Scanning projects for validation...")
    projects = scan_projects(repo_root, enable_history=False)

    findings: List[ValidationFinding] = []

    # Load restore map if provided
    restore_map: List[RestoreInstruction] = []
    if args.restore_map and args.restore_map.exists():
        try:
            with args.restore_map.open("r", encoding="utf-8") as f:
                data = json.load(f)
                restore_map = [RestoreInstruction(**item) for item in data]
        except Exception as e:
            LOGGER.error("Failed to load restore map: %s", e)

    # 1. Structural Validation
    for project in projects:
        project_findings = _validate_project(project, restore_map, repo_root)
        findings.extend(project_findings)

    # 2. MSBuild Validation (Optional)
    if args.run_build:
        LOGGER.info("Running MSBuild validation...")
        build_findings = _run_msbuild_validation(repo_root, output_dir)
        findings.extend(build_findings)

        LOGGER.info("Running Reflection validation...")
        reflection_findings = _run_reflection_validation(repo_root, projects, output_dir)
        findings.extend(reflection_findings)

    _report_findings(findings, output_dir)

    if findings:
        LOGGER.error("Validation failed with %d findings.", len(findings))
        sys.exit(1)
    else:
        LOGGER.info("Validation passed. All %d projects are compliant.", len(projects))
        sys.exit(0)


def _validate_project(
    project: ManagedProject,
    restore_map: List[RestoreInstruction],
    repo_root: Path
) -> List[ValidationFinding]:
    findings = []

    # 1. Check GenerateAssemblyInfo is false
    if project.generate_assembly_info_value is not False:
        findings.append(
            ValidationFinding(
                project_id=project.project_id,
                finding_code="GenerateAssemblyInfoTrue",
                severity="Error",
                details=f"GenerateAssemblyInfo is {project.generate_assembly_info_value}, expected false.",
            )
        )

    # 2. Check CommonAssemblyInfo.cs is linked
    if not project.template_imported:
        findings.append(
            ValidationFinding(
                project_id=project.project_id,
                finding_code="MissingTemplateImport",
                severity="Error",
                details="CommonAssemblyInfo.cs is not linked.",
            )
        )

    # 3. Check for duplicate links and duplicate AssemblyInfo includes
    dup_findings = _check_duplicates(project)
    findings.extend(dup_findings)

    # 4. Check restored files
    # Find restore instructions for this project
    # We need to map project path to restore instructions
    # RestoreInstruction has relative_path.
    # We can check if the relative path starts with the project directory relative to repo root.

    project_rel_dir = Path(project.project_id).parent

    for instr in restore_map:
        instr_path = Path(instr.relative_path)
        try:
            # Check if instr_path is inside project_dir
            # This is a simple check, might need refinement if projects are nested
            if project_rel_dir in instr_path.parents:
                # Check if file exists
                abs_path = repo_root / instr_path
                if not abs_path.exists():
                    findings.append(
                        ValidationFinding(
                            project_id=project.project_id,
                            finding_code="MissingAssemblyInfoFile",
                            severity="Error",
                            details=f"Expected restored file {instr.relative_path} is missing.",
                        )
                    )
        except ValueError:
            pass

    return findings


def _check_duplicates(project: ManagedProject) -> List[ValidationFinding]:
    findings = []
    try:
        tree = ET.parse(project.path)
        root = tree.getroot()
        ns = "{" + MSBUILD_NS + "}" if root.tag.startswith("{") else ""

        seen_includes = set()

        for compile_item in root.findall(f".//{ns}Compile"):
            include = compile_item.get("Include", "")
            if not include:
                continue

            norm_include = include.replace("/", "\\").lower()

            if "assemblyinfo" in norm_include:
                if norm_include in seen_includes:
                    findings.append(
                        ValidationFinding(
                            project_id=project.project_id,
                            finding_code="DuplicateCompileEntry",
                            severity="Error",
                            details=f"Duplicate compile entry for {include}",
                        )
                    )
                seen_includes.add(norm_include)

    except Exception as e:
        LOGGER.warning("Failed to parse %s for duplicates: %s", project.path, e)

    return findings


def _run_msbuild_validation(repo_root: Path, output_dir: Path) -> List[ValidationFinding]:
    findings = []
    log_path = output_dir / "msbuild-validation.log"

    # Command: msbuild FieldWorks.sln /m /p:Configuration=Debug /fl /flp:logfile=...
    cmd = [
        "msbuild",
        "FieldWorks.sln",
        "/m",
        "/p:Configuration=Debug",
        f"/flp:logfile={log_path};verbosity=normal"
    ]

    LOGGER.info("Executing: %s", " ".join(cmd))
    try:
        result = subprocess.run(
            cmd,
            cwd=repo_root,
            capture_output=True,
            text=True
        )

        if result.returncode != 0:
            findings.append(
                ValidationFinding(
                    project_id="FieldWorks.sln",
                    finding_code="GenerateAssemblyInfoTrue", # Reusing code or add new one
                    severity="Error",
                    details="MSBuild failed. Check msbuild-validation.log.",
                )
            )

        # Parse log for CS0579
        if log_path.exists():
            content = log_path.read_text(encoding="utf-8", errors="replace")
            if "CS0579" in content:
                findings.append(
                    ValidationFinding(
                        project_id="FieldWorks.sln",
                        finding_code="DuplicateCompileEntry",
                        severity="Error",
                        details="Found CS0579 (Duplicate Attribute) warnings in build log.",
                    )
                )

    except Exception as e:
        LOGGER.error("MSBuild execution failed: %s", e)
        findings.append(
            ValidationFinding(
                project_id="FieldWorks.sln",
                finding_code="GenerateAssemblyInfoTrue",
                severity="Error",
                details=f"MSBuild execution exception: {e}",
            )
        )

    return findings


def _run_reflection_validation(
    repo_root: Path,
    projects: List[ManagedProject],
    output_dir: Path
) -> List[ValidationFinding]:
    findings = []
    log_path = output_dir / "reflection.log"
    script_path = repo_root / "scripts" / "GenerateAssemblyInfo" / "reflect_attributes.ps1"

    if not script_path.exists():
        LOGGER.warning("Reflection script not found at %s", script_path)
        return []

    # Gather output assemblies
    # Assuming Debug build
    assemblies = []
    for p in projects:
        # Heuristic for assembly path: Output/Debug/ProjectName.dll or .exe
        # This is fragile, but sufficient for validation if we check existence
        name = p.path.stem
        dll_path = repo_root / "Output" / "Debug" / f"{name}.dll"
        exe_path = repo_root / "Output" / "Debug" / f"{name}.exe"

        if dll_path.exists():
            assemblies.append(str(dll_path))
        elif exe_path.exists():
            assemblies.append(str(exe_path))

    if not assemblies:
        LOGGER.warning("No assemblies found in Output/Debug. Did you run build?")
        return []

    # Run PowerShell script
    cmd = [
        "powershell",
        "-NoProfile",
        "-ExecutionPolicy", "Bypass",
        "-File", str(script_path),
        "-Output", str(log_path),
        "-Assemblies"
    ] + assemblies

    LOGGER.info("Running reflection harness on %d assemblies...", len(assemblies))
    try:
        result = subprocess.run(cmd, capture_output=True, text=True)

        if result.returncode != 0:
            findings.append(
                ValidationFinding(
                    project_id="ReflectionHarness",
                    finding_code="MissingTemplateImport", # Proxy for attribute missing
                    severity="Error",
                    details="Reflection harness failed. Check reflection.log.",
                )
            )

    except Exception as e:
        LOGGER.error("Reflection harness failed: %s", e)

    return findings


def _report_findings(findings: List[ValidationFinding], output_dir: Path) -> None:
    output_dir.mkdir(parents=True, exist_ok=True)
    report_path = output_dir / "validation_report.txt"

    with report_path.open("w", encoding="utf-8") as f:
        if not findings:
            f.write("Validation Passed: No issues found.\n")
            return

        f.write(f"Validation Failed: {len(findings)} issues found.\n\n")
        for finding in findings:
            f.write(
                f"[{finding.severity}] {finding.project_id}: {finding.finding_code}\n"
            )
            f.write(f"    {finding.details}\n")

    LOGGER.info("Validation report written to %s", report_path)


if __name__ == "__main__":
    main()
