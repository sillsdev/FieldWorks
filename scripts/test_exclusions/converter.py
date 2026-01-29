from __future__ import annotations

import shutil
import subprocess
from pathlib import Path
from typing import List, Set

from . import msbuild_parser
from .models import Project, TestFolder


class Converter:
    """Handles the conversion of projects to Pattern A."""

    def __init__(self, repo_root: Path) -> None:
        self.repo_root = repo_root

    def convert_project(
        self,
        project: Project,
        test_folders: List[TestFolder],
        dry_run: bool = False,
        verify: bool = False,
    ) -> bool:
        """Convert a single project to Pattern A.

        Returns True if changes were made (or would be made in dry-run).
        """
        project_path = self.repo_root / project.relative_path
        if not project_path.exists():
            print(f"Project not found: {project_path}")
            return False

        # 1. Identify rules to remove (wildcards)
        current_rules = msbuild_parser.read_exclusion_rules(project_path)
        to_remove = [r.pattern for r in current_rules if r.pattern.startswith("*")]

        # 2. Identify rules to add
        # Always add ProjectNameTests/**
        to_add: Set[str] = {f"{project.name}Tests/**"}
        # Add detected test folders
        for folder in test_folders:
            # folder.relative_path is relative to project dir
            pattern = f"{folder.relative_path}/**"
            to_add.add(pattern)

        existing_patterns = {r.pattern for r in current_rules}
        needed_adds = to_add - existing_patterns

        if not to_remove and not needed_adds:
            return False

        if dry_run:
            print(f"Dry run: {project.name}")
            for p in to_remove:
                print(f"  - Remove: {p}")
            for p in needed_adds:
                print(f"  + Add:    {p}")
            return True

        # Backup
        backup_path = project_path.with_suffix(".csproj.bak")
        shutil.copy2(project_path, backup_path)

        try:
            for p in to_remove:
                msbuild_parser.remove_exclusion(project_path, p)

            for p in to_add:
                msbuild_parser.ensure_explicit_exclusion(project_path, p)

            if verify:
                if not self.verify_build(project):
                    raise RuntimeError("Build verification failed")

            return True
        except Exception as e:
            print(f"Error converting {project.name}: {e}")
            print("Restoring backup...")
            if backup_path.exists():
                shutil.move(str(backup_path), str(project_path))
            raise
        finally:
            if backup_path.exists():
                backup_path.unlink()

    def verify_build(self, project: Project) -> bool:
        """Run a build for the project to verify no regressions."""
        project_path = self.repo_root / project.relative_path
        # We use -target:Build to ensure it actually builds.
        # We assume dependencies are already built or msbuild can handle it.
        # Using /p:Configuration=Debug /p:Platform=x64 as standard.
        cmd = [
            "msbuild",
            str(project_path),
            "/p:Configuration=Debug",
            "/p:Platform=x64",
            "/v:minimal",
            "/nologo",
        ]
        try:
            subprocess.run(cmd, check=True, capture_output=True, text=True)
            return True
        except subprocess.CalledProcessError as e:
            print(f"Build failed for {project.name}:")
            print(e.stdout)
            print(e.stderr)
            return False
