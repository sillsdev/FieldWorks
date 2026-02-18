"""Utilities to enumerate managed projects and capture baseline metadata."""

from __future__ import annotations

import logging
import xml.etree.ElementTree as ET
from dataclasses import replace
from pathlib import Path
from typing import Iterable, List, Optional

from .git_metadata import gather_baseline_paths, read_last_commit
from .models import AssemblyInfoFile, ManagedProject
from .assembly_info_parser import parse_assembly_info_files

LOGGER = logging.getLogger(__name__)
COMMON_ASSEMBLY_FILENAME = "CommonAssemblyInfo.cs"
COMMON_INCLUDE_TOKEN = "CommonAssemblyInfo"


def scan_projects(
    repo_root: Path,
    release_ref: Optional[str] = None,
    enable_history: bool = True,
) -> List[ManagedProject]:
    """Discover every managed .csproj under Src/ and summarize its metadata."""

    csproj_files = sorted((repo_root / "Src").rglob("*.csproj"))
    projects: List[ManagedProject] = []
    baseline_paths = (
        gather_baseline_paths(repo_root, release_ref) if enable_history else None
    )

    for csproj_path in csproj_files:
        try:
            project = _analyze_project(
                repo_root,
                csproj_path,
                baseline_paths,
                release_ref,
                enable_history,
            )
        except Exception as exc:  # pylint: disable=broad-except
            LOGGER.exception("Failed to analyze %s", csproj_path)
            project = ManagedProject(
                project_id=_relative_project_id(repo_root, csproj_path),
                path=csproj_path,
                category="G",
                template_imported=False,
                has_custom_assembly_info=False,
                generate_assembly_info_value=None,
                remediation_state="NeedsRemediation",
                notes=f"analysis-error: {exc}",
            )
        projects.append(project)

    return projects


def _analyze_project(
    repo_root: Path,
    csproj_path: Path,
    baseline_paths,
    release_ref: Optional[str],
    enable_history: bool,
) -> ManagedProject:
    tree = ET.parse(csproj_path)
    root = tree.getroot()

    template_imported = _has_template_import(root)
    generate_value = _read_generate_assembly_info(root)
    assembly_files = parse_assembly_info_files(
        _relative_project_id(repo_root, csproj_path), csproj_path.parent, repo_root
    )
    release_flag = None
    latest_sha = None
    latest_date = None
    if enable_history:
        _annotate_history(repo_root, assembly_files, baseline_paths, release_ref)
        release_flag = _project_release_flag(assembly_files, baseline_paths)
        latest_sha, latest_date = _latest_commit(assembly_files)
    has_custom_assembly = bool(assembly_files)

    category = _classify(template_imported, has_custom_assembly)
    notes = _build_notes(template_imported, has_custom_assembly, generate_value)

    return ManagedProject(
        project_id=_relative_project_id(repo_root, csproj_path),
        path=csproj_path,
        category=category,
        template_imported=template_imported,
        has_custom_assembly_info=has_custom_assembly,
        generate_assembly_info_value=generate_value,
        assembly_info_files=assembly_files,
        notes=notes,
        release_ref_has_custom_files=release_flag,
        latest_custom_commit_sha=latest_sha,
        latest_custom_commit_date=latest_date,
    )


def _relative_project_id(repo_root: Path, csproj_path: Path) -> str:
    return csproj_path.relative_to(repo_root).as_posix()


def _has_template_import(root: ET.Element) -> bool:
    for compile_item in root.findall(".//{*}Compile"):
        include = compile_item.get("Include", "")
        if COMMON_INCLUDE_TOKEN in include:
            return True
    for import_node in root.findall(".//{*}Import"):
        proj_attr = import_node.get("Project", "")
        if COMMON_INCLUDE_TOKEN in proj_attr:
            return True
    return False


def _read_generate_assembly_info(root: ET.Element) -> Optional[bool]:
    node = root.find(".//{*}GenerateAssemblyInfo")
    if node is None or not node.text:
        return None
    text = node.text.strip().lower()
    if text in {"true", "1", "yes"}:
        return True
    if text in {"false", "0", "no"}:
        return False
    return None


def _classify(template_imported: bool, has_custom_assembly: bool) -> str:
    if template_imported and not has_custom_assembly:
        return "T"
    if template_imported and has_custom_assembly:
        return "C"
    return "G"


def _build_notes(
    template_imported: bool, has_custom_assembly: bool, generate_value: Optional[bool]
) -> str:
    reasons: List[str] = []
    if not template_imported:
        reasons.append("missing-template-import")
    if generate_value is not False:
        reasons.append("generateassemblyinfo-not-false")
    if has_custom_assembly and not template_imported:
        reasons.append("custom-file-without-template")
    return ";".join(reasons)


def summarize_categories(projects: Iterable[ManagedProject]) -> dict:
    summary = {"T": 0, "C": 0, "G": 0}
    for project in projects:
        summary[project.category] = summary.get(project.category, 0) + 1
    return summary


def update_project_assembly_files(
    project: ManagedProject, assembly_files: List[AssemblyInfoFile]
) -> ManagedProject:
    """Return a copy of the ManagedProject with refreshed AssemblyInfo data."""

    return replace(
        project,
        assembly_info_files=assembly_files,
        has_custom_assembly_info=bool(assembly_files),
    )


def _annotate_history(
    repo_root: Path,
    assembly_files: List[AssemblyInfoFile],
    baseline_paths,
    release_ref: Optional[str],
) -> None:
    for assembly_file in assembly_files:
        if baseline_paths is not None:
            assembly_file.release_ref_name = release_ref
            assembly_file.present_in_release_ref = (
                assembly_file.relative_path in baseline_paths
            )
        metadata = read_last_commit(repo_root, Path(assembly_file.relative_path))
        if metadata is None:
            continue
        assembly_file.last_commit_sha = metadata.sha
        assembly_file.last_commit_date = metadata.date
        assembly_file.last_commit_author = metadata.author


def _project_release_flag(
    assembly_files: List[AssemblyInfoFile], baseline_paths
) -> Optional[bool]:
    if baseline_paths is None:
        return None
    if not assembly_files:
        return False
    any_true = False
    for assembly_file in assembly_files:
        if assembly_file.present_in_release_ref:
            any_true = True
        elif assembly_file.present_in_release_ref is None:
            return None
    return any_true


def _latest_commit(
    assembly_files: List[AssemblyInfoFile],
) -> tuple[Optional[str], Optional[str]]:
    latest_sha = None
    latest_date = None
    for assembly_file in assembly_files:
        if assembly_file.last_commit_date is None:
            continue
        if latest_date is None or assembly_file.last_commit_date > latest_date:
            latest_date = assembly_file.last_commit_date
            latest_sha = assembly_file.last_commit_sha
    return latest_sha, latest_date
