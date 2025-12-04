"""Output helpers for audit/convert/validate artifacts."""

from __future__ import annotations

import csv
import json
from pathlib import Path
from typing import Iterable, Sequence

from .models import ManagedProject, ValidationFinding, RestoreInstruction


def write_managed_projects_csv(
    projects: Sequence[ManagedProject], output_path: Path
) -> None:
    output_path.parent.mkdir(parents=True, exist_ok=True)
    fieldnames = [
        "project_id",
        "category",
        "template_imported",
        "has_custom_assembly_info",
        "generate_assembly_info_value",
        "remediation_state",
        "notes",
        "release_ref_has_custom_files",
        "latest_custom_commit_date",
        "latest_custom_commit_sha",
        "assembly_info_details",
    ]
    with output_path.open("w", newline="", encoding="utf-8") as handle:
        writer = csv.DictWriter(handle, fieldnames=fieldnames)
        writer.writeheader()
        for project in projects:
            writer.writerow(
                {
                    "project_id": project.project_id,
                    "category": project.category,
                    "template_imported": project.template_imported,
                    "has_custom_assembly_info": project.has_custom_assembly_info,
                    "generate_assembly_info_value": project.generate_assembly_info_value,
                    "remediation_state": project.remediation_state,
                    "notes": project.notes,
                    "release_ref_has_custom_files": project.release_ref_has_custom_files,
                    "latest_custom_commit_date": project.latest_custom_commit_date,
                    "latest_custom_commit_sha": project.latest_custom_commit_sha,
                    "assembly_info_details": _format_assembly_details(project),
                }
            )


def write_restore_map(entries: Iterable[RestoreInstruction], output_path: Path) -> None:
    data = [entry.to_dict() for entry in entries]
    _write_json(output_path, data)


def write_findings_report(
    findings: Iterable[ValidationFinding], output_path: Path
) -> None:
    data = [finding.to_dict() for finding in findings]
    _write_json(output_path, data)


def write_projects_json(projects: Iterable[ManagedProject], output_path: Path) -> None:
    data = [project.to_dict() for project in projects]
    _write_json(output_path, data)


def _format_assembly_details(project: ManagedProject) -> str:
    details = []
    for assembly_file in project.assembly_info_files:
        release_flag = "unknown"
        if assembly_file.present_in_release_ref is True:
            release_flag = "present"
        elif assembly_file.present_in_release_ref is False:
            release_flag = "absent"
        commit_chunk = None
        if assembly_file.last_commit_sha and assembly_file.last_commit_date:
            commit_chunk = (
                f"{assembly_file.last_commit_sha}@{assembly_file.last_commit_date}"
            )
        elif assembly_file.last_commit_sha:
            commit_chunk = assembly_file.last_commit_sha
        segments = [assembly_file.relative_path, f"release={release_flag}"]
        if commit_chunk:
            segments.append(f"commit={commit_chunk}")
        if assembly_file.last_commit_author:
            segments.append(f"author={assembly_file.last_commit_author}")
        details.append("|".join(segments))
    return ";".join(details)


def _write_json(output_path: Path, data) -> None:  # type: ignore[no-untyped-def]
    output_path.parent.mkdir(parents=True, exist_ok=True)
    with output_path.open("w", encoding="utf-8") as handle:
        json.dump(data, handle, indent=2, sort_keys=True)
        handle.write("\n")
