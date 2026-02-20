from __future__ import annotations

import fnmatch
from pathlib import Path
from typing import Iterable, List, Sequence, Tuple

from . import msbuild_parser
from .models import (
    ExclusionRule,
    PatternType,
    Project,
    ProjectScanResult,
    ProjectStatus,
    TestFolder,
    ValidationIssue,
    ValidationIssueType,
    ValidationSeverity,
)

_TEST_SUFFIX = "Tests"


def scan_repository(repo_root: Path) -> List[ProjectScanResult]:
    """Scan every SDK-style project under `Src/`.

    Parameters
    ----------
    repo_root:
        The FieldWorks repository root.
    """

    src_root = repo_root / "Src"
    if not src_root.exists():
        return []

    results: List[ProjectScanResult] = []
    for project_path in sorted(src_root.rglob("*.csproj")):
        results.append(scan_project(repo_root, project_path))
    return results


def scan_project(repo_root: Path, project_path: Path) -> ProjectScanResult:
    project_name = project_path.stem
    rules = msbuild_parser.read_exclusion_rules(project_path)
    project_dir = project_path.parent
    test_folders = _discover_test_folders(project_dir)
    issues: List[ValidationIssue] = []

    pattern_type = _classify_pattern(project_name, rules)

    # Skip mixed code detection and missing exclusion checks for Test projects
    is_test_project = project_name.endswith("Tests") or project_name.endswith("Test")

    if is_test_project:
        has_mixed_code = False
        mixed_folder = ""
    else:
        has_mixed_code, mixed_folder = _detect_mixed_code(project_dir, test_folders)

    if has_mixed_code:
        issues.append(
            ValidationIssue(
                project_name=project_name,
                issue_type=ValidationIssueType.MIXED_CODE,
                severity=ValidationSeverity.ERROR,
                details=f"Mixed production/test code detected under {mixed_folder}",
            )
        )

    if pattern_type == PatternType.NONE and test_folders and not is_test_project:
        issues.append(
            ValidationIssue(
                project_name=project_name,
                issue_type=ValidationIssueType.MISSING_EXCLUSION,
                severity=ValidationSeverity.ERROR,
                details="Test folders exist but no exclusion pattern was found.",
            )
        )

    project = Project(
        name=project_name,
        relative_path=project_path.relative_to(repo_root).as_posix(),
        pattern_type=pattern_type,
        has_mixed_code=has_mixed_code,
        status=ProjectStatus.PENDING,
    )

    folder_models = _build_folder_models(project_name, project_dir, test_folders, rules)
    return ProjectScanResult(
        project=project,
        test_folders=folder_models,
        exclusion_rules=rules,
        issues=issues,
    )


def find_csproj_files(repo_root: Path) -> List[Path]:
    return sorted((repo_root / "Src").rglob("*.csproj"))


def _discover_test_folders(project_dir: Path) -> List[Path]:
    candidates: List[Path] = []
    for folder in project_dir.rglob("*"):
        if not folder.is_dir():
            continue
        try:
            rel_parts = folder.relative_to(project_dir).parts
        except ValueError:
            continue
        if any(part.lower() in {"bin", "obj"} for part in rel_parts):
            continue
        if folder.name.endswith(_TEST_SUFFIX):
            candidates.append(folder)
    return sorted(candidates)


def _classify_pattern(project_name: str, rules: Iterable[ExclusionRule]) -> PatternType:
    if not rules:
        return PatternType.NONE

    explicit_target = f"{project_name}{_TEST_SUFFIX}/**"
    has_explicit = any(rule.pattern == explicit_target for rule in rules)
    has_wildcard = any(rule.pattern.startswith("*") for rule in rules)

    if has_wildcard:
        return PatternType.PATTERN_B
    if has_explicit and all(not rule.pattern.startswith("*") for rule in rules):
        return PatternType.PATTERN_A
    return PatternType.PATTERN_C


def _build_folder_models(
    project_name: str,
    project_dir: Path,
    folders: Sequence[Path],
    rules: List[ExclusionRule],
) -> List[TestFolder]:
    folder_models: List[TestFolder] = []
    for folder in folders:
        rel_path = folder.relative_to(project_dir).as_posix()
        excluded = _is_excluded(rel_path, rules)
        contains_source = any(
            file.suffix.lower() == ".cs" for file in folder.rglob("*.cs")
        )
        depth = len(folder.relative_to(project_dir).parts)
        folder_models.append(
            TestFolder(
                project_name=project_name,
                relative_path=rel_path,
                depth=depth,
                contains_source=contains_source,
                excluded=excluded,
            )
        )
    return folder_models


def _to_posix(path: Path) -> str:
    return path.as_posix().replace("\\", "/")


def _is_excluded(folder: str, rules: List[ExclusionRule]) -> bool:
    folder = folder.replace("\\", "/")
    for rule in rules:
        pattern = rule.pattern.rstrip("/")
        pattern = pattern.replace("\\", "/")
        base = pattern.replace("/**", "")
        if "*" in base:
            if fnmatch.fnmatch(folder, base):
                return True
        else:
            if folder == base or folder.startswith(f"{base}/"):
                return True
    return False


def _detect_mixed_code(project_dir: Path, folders: Sequence[Path]) -> Tuple[bool, str]:
    folder_paths = [folder.resolve() for folder in folders]
    for source_file in project_dir.rglob("*.cs"):
        file_path = source_file.resolve()

        # Check if file is inside any of the test folders
        is_in_test_folder = False
        for folder in folder_paths:
            # Check if folder is a parent of file_path
            try:
                file_path.relative_to(folder)
                is_in_test_folder = True
                break
            except ValueError:
                continue

        if is_in_test_folder:
            continue

        stem = source_file.stem
        if stem.endswith("Test") or stem.endswith("Tests"):
            rel = _to_posix(source_file.relative_to(project_dir))
            return True, rel
    return False, ""
