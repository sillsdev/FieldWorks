#!/usr/bin/env python3
"""Utility helpers for computing deterministic Src/<Folder> tree hashes.

The goal is to capture the set of tracked files under a folder (excluding the
folder's COPILOT.md) and produce a stable digest that represents the code/data
state that documentation was written against.

We hash the list of files paired with their git blob SHAs at the specified ref
(default HEAD). The working tree is not considered; callers should ensure they
run these helpers on a clean tree or handle dirty-state warnings separately.
"""
from __future__ import annotations

import hashlib
import subprocess
from pathlib import Path
from typing import Iterable, Tuple

__all__ = [
    "compute_folder_tree_hash",
    "list_tracked_blobs",
]


def run(cmd: Iterable[str], cwd: Path) -> str:
    """Run a subprocess and return stdout decoded as UTF-8."""
    return subprocess.check_output(cmd, cwd=str(cwd), stderr=subprocess.STDOUT).decode(
        "utf-8", errors="replace"
    )


def list_tracked_blobs(
    root: Path, folder: Path, ref: str = "HEAD"
) -> Iterable[Tuple[str, str]]:
    """Yield (relative_path, blob_sha) for tracked files under ``folder``.

    ``ref`` defaults to ``HEAD``. ``folder`` must be inside ``root``.
    ``COPILOT.md`` is excluded by design so the hash reflects code/data only.
    """

    rel = folder.relative_to(root).as_posix()
    if not rel.startswith("Src/"):
        raise ValueError(f"Folder must reside under Src/: {rel}")

    try:
        output = run(
            [
                "git",
                "ls-tree",
                "-r",
                "--full-tree",
                ref,
                "--",
                rel,
            ],
            cwd=root,
        )
    except subprocess.CalledProcessError as exc:
        raise RuntimeError(
            f"Failed to list tracked files for {rel}: {exc.output.decode('utf-8', errors='replace')}"
        ) from exc

    for line in output.splitlines():
        parts = line.split()
        if len(parts) < 4:
            continue
        _, obj_type, blob_sha, *rest = parts
        if obj_type != "blob":
            continue
        path = rest[-1]
        if path.endswith("/COPILOT.md") or path == "COPILOT.md":
            continue
        yield path, blob_sha


def compute_folder_tree_hash(root: Path, folder: Path, ref: str = "HEAD") -> str:
    """Compute a stable sha256 digest representing ``folder`` at ``ref``.

    The digest is the sha256 of ``"{relative_path}:{blob_sha}\n"`` for each
    tracked file (sorted lexicographically) underneath ``folder`` excluding the
    COPILOT.md documentation. When a folder has no tracked files besides
    COPILOT.md the digest is the sha256 of the empty string.
    """

    items = sorted(list_tracked_blobs(root, folder, ref))
    digest = hashlib.sha256()
    for rel_path, blob_sha in items:
        digest.update(f"{rel_path}:{blob_sha}\n".encode("utf-8"))
    return digest.hexdigest()
