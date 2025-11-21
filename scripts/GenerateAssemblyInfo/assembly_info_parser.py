"""Helpers for reading AssemblyInfo*.cs files and extracting metadata."""

from __future__ import annotations

import re
from pathlib import Path
from typing import List

from .models import AssemblyInfoFile

ATTRIBUTE_PATTERN = re.compile(r"\[assembly:\s*(?P<name>[A-Za-z0-9_\.]+)")
CONDITIONAL_PATTERN = re.compile(r"#\s*(if|elif|else|endif)")


def parse_assembly_info_files(
    project_id: str, project_dir: Path, repo_root: Path
) -> List[AssemblyInfoFile]:
    """Parse every AssemblyInfo*.cs under the given project directory."""

    files = sorted(project_dir.glob("**/AssemblyInfo*.cs"))
    assembly_infos: List[AssemblyInfoFile] = []
    for file_path in files:
        if _is_common_template_link(file_path):
            continue
        if _belongs_to_nested_project(file_path, project_dir):
            continue
        assembly_infos.append(_parse_single_file(project_id, file_path, repo_root))
    return assembly_infos


def _belongs_to_nested_project(file_path: Path, project_root: Path) -> bool:
    """Return True if the file resides in a subdirectory that has its own .csproj."""
    current = file_path.parent
    # Traverse up until we hit the project root
    while current != project_root:
        # If we hit the filesystem root or go above project_root (shouldn't happen with glob), stop
        if current == current.parent:
            break
        # If this directory contains a .csproj, the file belongs to that nested project
        if any(current.glob("*.csproj")):
            return True
        current = current.parent
    return False


def _parse_single_file(
    project_id: str, file_path: Path, repo_root: Path
) -> AssemblyInfoFile:
    content = file_path.read_text(encoding="utf-8", errors="ignore")
    attributes = sorted(set(ATTRIBUTE_PATTERN.findall(content)))
    has_conditionals = bool(CONDITIONAL_PATTERN.search(content))
    return AssemblyInfoFile(
        project_id=project_id,
        path=file_path,
        relative_path=file_path.relative_to(repo_root).as_posix(),
        custom_attributes=attributes,
        has_conditional_blocks=has_conditionals,
    )


def _is_common_template_link(file_path: Path) -> bool:
    normalized = file_path.name.lower()
    return normalized == "commonassemblyinfo.cs"
