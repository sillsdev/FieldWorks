from __future__ import annotations

from datetime import datetime
from pathlib import Path

from scripts.test_exclusions import msbuild_parser, repo_scanner
from scripts.test_exclusions.models import (
    ConversionJob,
    ExclusionScope,
    PatternType,
    Project,
    ProjectStatus,
    TestFolder,
    ValidationIssueType,
)


def _write_project(
    temp_repo: Path, csproj_writer, name: str, item_groups: str = ""
) -> Path:
    project_dir = temp_repo / "Src" / name
    project_dir.mkdir(parents=True)
    csproj_path = project_dir / f"{name}.csproj"
    csproj_writer(csproj_path, item_groups=item_groups)
    return project_dir


def test_models_round_trip() -> None:
    project = Project(
        name="FwUtils",
        relative_path="Src/Common/FwUtils/FwUtils.csproj",
        pattern_type=PatternType.PATTERN_A,
        has_mixed_code=False,
        status=ProjectStatus.CONVERTED,
        last_validated=datetime(2025, 1, 1, 12, 0, 0),
    )
    folder = TestFolder(
        project_name="FwUtils",
        relative_path="FwUtilsTests",
        depth=1,
        contains_source=True,
        excluded=True,
    )
    job = ConversionJob(
        job_id="job-123",
        initiated_by="dev",
        project_list=["FwUtils"],
        script_version="0.1.0",
        start_time=datetime(2025, 1, 1, 12, 0, 0),
        end_time=datetime(2025, 1, 1, 12, 5, 0),
        result="Success",
    )

    assert project.to_dict()["patternType"] == "A"
    assert folder.to_dict()["excluded"] is True
    assert job.to_dict()["result"] == "Success"


def test_msbuild_parser_inserts_explicit_rule(tmp_path: Path, csproj_writer) -> None:
    project_path = tmp_path / "Sample.csproj"
    csproj_writer(project_path)
    msbuild_parser.ensure_explicit_exclusion(project_path, "SampleTests/**")

    rules = msbuild_parser.read_exclusion_rules(project_path)
    assert any(
        rule.pattern == "SampleTests/**" and rule.scope == ExclusionScope.BOTH
        for rule in rules
    )

    # Calling again should not duplicate entries.
    msbuild_parser.ensure_explicit_exclusion(project_path, "SampleTests/**")
    rules_again = msbuild_parser.read_exclusion_rules(project_path)
    assert len(rules_again) == 1


def test_repo_scanner_detects_patterns(temp_repo: Path, csproj_writer) -> None:
    explicit_dir = _write_project(
        temp_repo,
        csproj_writer,
        "Explicit",
        item_groups="""
        <ItemGroup>
          <Compile Remove=\"ExplicitTests/**\" />
          <None Remove=\"ExplicitTests/**\" />
        </ItemGroup>
        """,
    )
    (explicit_dir / "ExplicitTests").mkdir()
    (explicit_dir / "ExplicitTests" / "FooTest.cs").write_text("class FooTest { }")

    wildcard_dir = _write_project(
        temp_repo,
        csproj_writer,
        "Wildcard",
        item_groups="""
        <ItemGroup>
          <Compile Remove=\"*Tests/**\" />
          <None Remove=\"*Tests/**\" />
        </ItemGroup>
        """,
    )
    (wildcard_dir / "WildcardTests").mkdir()
    (wildcard_dir / "WildcardTests" / "BarTest.cs").write_text("class BarTest { }")
    # Mixed code marker outside a *Tests folder.
    (wildcard_dir / "Helpers").mkdir()
    (wildcard_dir / "Helpers" / "HelperTests.cs").write_text("class HelperTests { }")

    missing_dir = _write_project(temp_repo, csproj_writer, "Missing")
    (missing_dir / "MissingTests").mkdir()
    (missing_dir / "MissingTests" / "BazTest.cs").write_text("class BazTest { }")

    results = repo_scanner.scan_repository(temp_repo)
    assert len(results) == 3

    explicit = next(result for result in results if result.project.name == "Explicit")
    wildcard = next(result for result in results if result.project.name == "Wildcard")
    missing = next(result for result in results if result.project.name == "Missing")

    assert explicit.project.pattern_type == PatternType.PATTERN_A
    assert explicit.test_folders[0].excluded is True

    assert wildcard.project.pattern_type == PatternType.PATTERN_B
    assert any(
        issue.issue_type == ValidationIssueType.MIXED_CODE for issue in wildcard.issues
    )

    assert missing.project.pattern_type == PatternType.NONE
    assert any(
        issue.issue_type == ValidationIssueType.MISSING_EXCLUSION
        for issue in missing.issues
    )
