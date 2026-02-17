from __future__ import annotations

import json
import shutil
from pathlib import Path

import pytest

from scripts.test_exclusions import validator
import validate_test_exclusions as validator_cli


def _create_project(
    repo: Path, name: str, content: str, test_folder: str | None = None
) -> Path:
    path = repo / "Src" / name / f"{name}.csproj"
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(content, encoding="utf-8")
    if test_folder:
        (path.parent / test_folder).mkdir(parents=True, exist_ok=True)
        # Add a dummy file to ensure folder is detected
        (path.parent / test_folder / "Test.cs").write_text("// Test", encoding="utf-8")
    return path


def test_validator_passes_clean_repo(tmp_path: Path):
    repo = tmp_path / "repo"
    repo.mkdir()

    # Pattern A
    _create_project(
        repo,
        "Valid",
        """<Project Sdk="Microsoft.NET.Sdk">
  <ItemGroup>
    <Compile Remove="ValidTests/**" />
    <None Remove="ValidTests/**" />
  </ItemGroup>
</Project>""",
        "ValidTests",
    )

    v = validator.Validator(repo)
    summary = v.validate_repo()

    assert summary.total_projects == 1
    assert summary.passed_projects == 1
    assert summary.failed_projects == 0
    assert summary.error_count == 0


def test_validator_fails_wildcard(tmp_path: Path):
    repo = tmp_path / "repo"
    repo.mkdir()

    # Pattern B
    _create_project(
        repo,
        "Wildcard",
        """<Project Sdk="Microsoft.NET.Sdk">
  <ItemGroup>
    <Compile Remove="*Tests/**" />
  </ItemGroup>
</Project>""",
        "WildcardTests",
    )

    v = validator.Validator(repo)
    summary = v.validate_repo()

    assert summary.failed_projects == 1
    assert summary.error_count == 1
    assert summary.issues[0].issue_type.value == "WildcardDetected"


def test_validator_fails_missing_exclusion(tmp_path: Path):
    repo = tmp_path / "repo"
    repo.mkdir()

    # None
    _create_project(
        repo,
        "Missing",
        """<Project Sdk="Microsoft.NET.Sdk">
</Project>""",
        "MissingTests",
    )

    v = validator.Validator(repo)
    summary = v.validate_repo()

    assert summary.failed_projects == 1
    assert summary.error_count >= 1
    types = {i.issue_type.value for i in summary.issues}
    assert "MissingExclusion" in types


def test_validator_cli_json_output(tmp_path: Path):
    repo = tmp_path / "repo"
    repo.mkdir()
    _create_project(
        repo,
        "Valid",
        """<Project Sdk="Microsoft.NET.Sdk">
  <ItemGroup>
    <Compile Remove="ValidTests/**" />
  </ItemGroup>
</Project>""",
        "ValidTests",
    )  # Pattern A

    json_path = tmp_path / "report.json"

    exit_code = validator_cli.main(
        ["--repo-root", str(repo), "--json-report", str(json_path)]
    )

    assert exit_code == 0
    assert json_path.exists()
    data = json.loads(json_path.read_text(encoding="utf-8"))
    assert data["passedProjects"] == 1
