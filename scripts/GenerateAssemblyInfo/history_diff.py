"""Detect deleted AssemblyInfo files and produce restore_map.json."""

from __future__ import annotations

import re
import subprocess
from pathlib import Path
from typing import Dict, List

from .models import RestoreInstruction
from .reporting import write_restore_map

COMMIT_PATTERN = re.compile(r"^[0-9a-f]{7,40}$")


def build_restore_map(repo_root: Path) -> List[RestoreInstruction]:
    """Return restore instructions for AssemblyInfo files missing at HEAD."""

    git_output = _run_git_log(repo_root)
    deleted_paths: Dict[str, RestoreInstruction] = {}
    current_commit = None
    for line in git_output.splitlines():
        line = line.strip()
        if not line:
            continue
        if COMMIT_PATTERN.match(line):
            current_commit = line
            continue
        if current_commit is None:
            continue
        if not _looks_like_assembly_info(line):
            continue
        normalized = line.replace("\\", "/")
        deleted_paths.setdefault(
            normalized,
            RestoreInstruction(
                project_id=_project_id_from_path(normalized),
                relative_path=normalized,
                commit_sha=current_commit,
            ),
        )
    instructions = [
        entry
        for entry in deleted_paths.values()
        if _is_missing(repo_root, entry.relative_path)
    ]
    return sorted(instructions, key=lambda item: item.relative_path)


def write_restore_map_file(
    repo_root: Path, output_path: Path
) -> List[RestoreInstruction]:
    instructions = build_restore_map(repo_root)
    write_restore_map(instructions, output_path)
    return instructions


def _run_git_log(repo_root: Path) -> str:
    command = [
        "git",
        "-C",
        str(repo_root),
        "log",
        "--diff-filter=D",
        "--name-only",
        "--pretty=format:%H",
        "--",
        "Src",
    ]
    result = subprocess.run(  # noqa: S603,S607
        command,
        check=False,
        capture_output=True,
        text=True,
        encoding="utf-8",
    )
    if result.returncode != 0:
        raise RuntimeError(f"git log for history diff failed: {result.stderr}")
    return result.stdout


def _looks_like_assembly_info(relative_path: str) -> bool:
    name = Path(relative_path).name.lower()
    return "assemblyinfo" in name and name.endswith(".cs")


def _is_missing(repo_root: Path, relative_path: str) -> bool:
    return not (repo_root / relative_path).exists()


def _project_id_from_path(relative_path: str) -> str:
    return relative_path.replace("\\", "/")
