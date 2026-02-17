from __future__ import annotations

import csv
import json
import shutil
from pathlib import Path

import pytest

import audit_test_exclusions as audit_cli

FIXTURE_ROOT = Path(__file__).resolve().parents[1] / "fixtures" / "audit"


def _copy_fixture_repo(tmp_path: Path) -> Path:
    repo = tmp_path / "repo"
    shutil.copytree(FIXTURE_ROOT, repo)
    return repo


def test_audit_cli_generates_json_csv_and_escalations(tmp_path: Path) -> None:
    repo = _copy_fixture_repo(tmp_path)
    json_path = tmp_path / "report.json"
    csv_path = tmp_path / "report.csv"
    mixed_json = tmp_path / "mixed.json"
    escalation_dir = tmp_path / "escalations"

    exit_code = audit_cli.main(
        [
            "--repo-root",
            str(repo),
            "--output",
            str(json_path),
            "--csv-output",
            str(csv_path),
            "--mixed-code-json",
            str(mixed_json),
            "--escalations-dir",
            str(escalation_dir),
        ]
    )
    assert exit_code == 0

    payload = json.loads(json_path.read_text(encoding="utf-8"))
    assert payload["projectCount"] == 3
    names = [project["project"]["name"] for project in payload["projects"]]
    assert {"Explicit", "Wildcard", "Missing"} == set(names)

    wildcard_entry = next(
        project
        for project in payload["projects"]
        if project["project"]["name"] == "Wildcard"
    )
    assert wildcard_entry["project"]["patternType"] == "B"
    assert any(issue["issueType"] == "MixedCode" for issue in wildcard_entry["issues"])

    with csv_path.open(encoding="utf-8") as fp:
        rows = list(csv.DictReader(fp))
    assert len(rows) == 3
    explicit_row = next(row for row in rows if row["projectName"] == "Explicit")
    assert explicit_row["patternType"] == "A"
    missing_row = next(row for row in rows if row["projectName"] == "Missing")
    assert missing_row["issueCount"] == "1"

    mixed_payload = json.loads(mixed_json.read_text(encoding="utf-8"))
    assert mixed_payload["count"] == 1
    assert mixed_payload["projects"][0]["name"] == "Wildcard"

    template_path = escalation_dir / "Wildcard.md"
    assert template_path.exists()
    template = template_path.read_text(encoding="utf-8")
    assert "Mixed Test Code Escalation" in template
    assert "Wildcard" in template


def test_audit_cli_defaults_use_repo_root(
    tmp_path: Path, monkeypatch: pytest.MonkeyPatch
) -> None:
    repo = _copy_fixture_repo(tmp_path)
    output_dir = repo / "Output" / "test-exclusions"

    monkeypatch.chdir(repo)
    exit_code = audit_cli.main(
        [
            "--repo-root",
            str(repo),
        ]
    )
    assert exit_code == 0
    assert (output_dir / "report.json").exists()
    assert (output_dir / "report.csv").exists()
    assert (output_dir / "mixed-code.json").exists()
    escalations_dir = output_dir / "escalations"
    assert escalations_dir.exists()
    assert any(escalations_dir.iterdir())
