"""Git utilities for restoring deleted AssemblyInfo files."""

from __future__ import annotations

import subprocess
from pathlib import Path
from typing import Optional


class GitRestoreError(RuntimeError):
    """Raised when git operations required for restoration fail."""


def ensure_git_available(repo_root: Path) -> None:
    _run_git(repo_root, ["--version"], check=True, capture_output=False)


def restore_file(repo_root: Path, target_path: Path, commit_sha: str) -> None:
    """Restore a file from git history at the provided commit."""

    relative = _relative_to_repo(repo_root, target_path)
    content = _run_git(
        repo_root,
        ["show", f"{commit_sha}:{relative.as_posix()}"],
        capture_output=True,
    )
    target_path.parent.mkdir(parents=True, exist_ok=True)
    target_path.write_bytes(content)


def file_exists_in_history(
    repo_root: Path, relative_path: Path, commit_sha: str
) -> bool:
    """Return True if the path exists at the specified commit."""

    try:
        _run_git(
            repo_root,
            ["cat-file", "-e", f"{commit_sha}:{relative_path.as_posix()}"],
            capture_output=False,
        )
        return True
    except GitRestoreError:
        return False


def _relative_to_repo(repo_root: Path, target_path: Path) -> Path:
    try:
        return target_path.relative_to(repo_root)
    except ValueError:
        raise GitRestoreError(f"{target_path} is outside {repo_root}") from None


def _run_git(
    repo_root: Path,
    args: list[str],
    *,
    check: bool = True,
    capture_output: bool = True,
) -> bytes:
    command = ["git", "-C", str(repo_root), *args]
    result = subprocess.run(  # noqa: S603,S607
        command,
        check=False,
        capture_output=capture_output,
    )
    if check and result.returncode != 0:
        stderr = result.stderr.decode("utf-8", errors="ignore") if result.stderr else ""
        raise GitRestoreError(f"git {' '.join(args)} failed: {stderr}")
    return result.stdout if capture_output else b""
