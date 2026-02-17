"""Git history helpers for GenerateAssemblyInfo automation."""

from __future__ import annotations

import logging
import subprocess
from dataclasses import dataclass
from pathlib import Path
from typing import Optional, Set

LOGGER = logging.getLogger(__name__)


def gather_baseline_paths(
    repo_root: Path, release_ref: Optional[str]
) -> Optional[Set[str]]:
    """Return AssemblyInfo-relative paths present on the given release ref."""

    if not release_ref:
        return None
    if not _git_ref_exists(repo_root, release_ref):
        LOGGER.warning(
            "Release ref %s not found; skipping baseline comparison", release_ref
        )
        return None
    command = [
        "git",
        "-C",
        str(repo_root),
        "ls-tree",
        "-r",
        "--name-only",
        release_ref,
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
        LOGGER.warning(
            "Unable to list AssemblyInfo files at %s: %s",
            release_ref,
            result.stderr.strip(),
        )
        return None
    baseline: Set[str] = set()
    for line in result.stdout.splitlines():
        line = line.strip()
        if not line:
            continue
        lowered = line.lower()
        if "assemblyinfo" not in lowered or not lowered.endswith(".cs"):
            continue
        baseline.add(line.replace("\\", "/"))
    return baseline


@dataclass
class CommitMetadata:
    sha: str
    date: str
    author: str


def read_last_commit(repo_root: Path, relative_path: Path) -> Optional[CommitMetadata]:
    """Return metadata for the most recent commit touching the file, if any."""

    command = [
        "git",
        "-C",
        str(repo_root),
        "log",
        "-n",
        "1",
        "--format=%H\t%cs\t%cn",
        "--",
        relative_path.as_posix(),
    ]
    result = subprocess.run(  # noqa: S603,S607
        command,
        check=False,
        capture_output=True,
        text=True,
        encoding="utf-8",
    )
    if result.returncode != 0 or not result.stdout.strip():
        return None
    parts = result.stdout.strip().split("\t")
    if len(parts) != 3:
        return None
    return CommitMetadata(sha=parts[0], date=parts[1], author=parts[2])


def _git_ref_exists(repo_root: Path, ref: str) -> bool:
    command = ["git", "-C", str(repo_root), "rev-parse", "--verify", ref]
    result = subprocess.run(  # noqa: S603,S607
        command,
        check=False,
        capture_output=True,
        text=True,
        encoding="utf-8",
    )
    return result.returncode == 0
