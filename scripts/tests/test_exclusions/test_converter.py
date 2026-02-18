from __future__ import annotations

import shutil
from pathlib import Path

import pytest

from scripts.test_exclusions import msbuild_parser
from scripts.test_exclusions.converter import Converter
from scripts.test_exclusions.models import PatternType, Project, TestFolder


def _create_project(repo: Path, name: str, content: str) -> Path:
    path = repo / "Src" / name / f"{name}.csproj"
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(content, encoding="utf-8")
    return path


def test_converter_removes_wildcards_and_adds_explicit(tmp_path: Path):
    repo = tmp_path / "repo"
    repo.mkdir()

    # Create Pattern B project
    csproj_content = """<Project Sdk="Microsoft.NET.Sdk">
  <ItemGroup>
    <Compile Remove="*Tests/**" />
    <None Remove="*Tests/**" />
  </ItemGroup>
</Project>"""
    proj_path = _create_project(repo, "PatternB", csproj_content)

    project = Project(
        name="PatternB",
        relative_path="Src/PatternB/PatternB.csproj",
        pattern_type=PatternType.PATTERN_B,
    )
    test_folders = [
        TestFolder(
            project_name="PatternB",
            relative_path="PatternBTests",
            depth=1,
            contains_source=True,
            excluded=True,
        )
    ]

    converter = Converter(repo)
    changed = converter.convert_project(project, test_folders, verify=False)

    assert changed

    # Verify content
    rules = msbuild_parser.read_exclusion_rules(proj_path)
    patterns = {r.pattern for r in rules}
    assert "*Tests/**" not in patterns
    assert "PatternBTests/**" in patterns


def test_converter_dry_run_does_not_modify(tmp_path: Path):
    repo = tmp_path / "repo"
    repo.mkdir()

    csproj_content = """<Project Sdk="Microsoft.NET.Sdk">
  <ItemGroup>
    <Compile Remove="*Tests/**" />
  </ItemGroup>
</Project>"""
    proj_path = _create_project(repo, "DryRun", csproj_content)

    project = Project(
        name="DryRun",
        relative_path="Src/DryRun/DryRun.csproj",
        pattern_type=PatternType.PATTERN_B,
    )
    test_folders = [
        TestFolder(
            project_name="DryRun",
            relative_path="DryRunTests",
            depth=1,
            contains_source=True,
            excluded=True,
        )
    ]

    converter = Converter(repo)
    changed = converter.convert_project(
        project, test_folders, dry_run=True, verify=False
    )

    assert changed

    # Verify content UNCHANGED
    rules = msbuild_parser.read_exclusion_rules(proj_path)
    patterns = {r.pattern for r in rules}
    assert "*Tests/**" in patterns
    assert "DryRunTests/**" not in patterns


def test_converter_adds_nested_folders(tmp_path: Path):
    repo = tmp_path / "repo"
    repo.mkdir()

    csproj_content = """<Project Sdk="Microsoft.NET.Sdk">
</Project>"""
    proj_path = _create_project(repo, "Nested", csproj_content)

    project = Project(
        name="Nested",
        relative_path="Src/Nested/Nested.csproj",
        pattern_type=PatternType.NONE,
    )
    test_folders = [
        TestFolder(
            project_name="Nested",
            relative_path="NestedTests",
            depth=1,
            contains_source=True,
            excluded=False,
        ),
        TestFolder(
            project_name="Nested",
            relative_path="Component/ComponentTests",
            depth=2,
            contains_source=True,
            excluded=False,
        ),
    ]

    converter = Converter(repo)
    changed = converter.convert_project(project, test_folders, verify=False)

    assert changed

    rules = msbuild_parser.read_exclusion_rules(proj_path)
    patterns = {r.pattern for r in rules}
    assert "NestedTests/**" in patterns
    assert "Component/ComponentTests/**" in patterns


def test_converter_backup_restore_on_failure(tmp_path: Path, monkeypatch):
    repo = tmp_path / "repo"
    repo.mkdir()

    csproj_content = """<Project Sdk="Microsoft.NET.Sdk">
</Project>"""
    proj_path = _create_project(repo, "Fail", csproj_content)

    project = Project(
        name="Fail",
        relative_path="Src/Fail/Fail.csproj",
        pattern_type=PatternType.NONE,
    )
    test_folders = []

    converter = Converter(repo)

    # Mock verify_build to fail
    def mock_verify(p):
        return False

    converter.verify_build = mock_verify

    with pytest.raises(RuntimeError, match="Build verification failed"):
        converter.convert_project(project, test_folders, verify=True)

    # Verify file restored (should be same as original)
    assert proj_path.exists()
    assert not proj_path.with_suffix(".csproj.bak").exists()
    # Content should be original
    assert "FailTests/**" not in proj_path.read_text(encoding="utf-8")
