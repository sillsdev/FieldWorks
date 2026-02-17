from __future__ import annotations

from datetime import datetime, timezone
from pathlib import Path
from typing import Iterable

from .models import ProjectScanResult, ValidationIssueType


class EscalationWriter:
    """Persist mixed-code escalations for manual follow-up."""

    def __init__(self, repo_root: Path) -> None:
        self.repo_root = repo_root

    def write_outputs(
        self,
        results: Iterable[ProjectScanResult],
        json_path: Path,
        templates_dir: Path,
    ) -> None:
        mixed_projects = [summary for summary in self._iter_mixed_projects(results)]
        payload = {
            "generatedAt": datetime.now(timezone.utc).isoformat(),
            "count": len(mixed_projects),
            "projects": mixed_projects,
        }
        json_path.parent.mkdir(parents=True, exist_ok=True)
        json_path.write_text(_to_json(payload), encoding="utf-8")

        templates_dir.mkdir(parents=True, exist_ok=True)
        for project in mixed_projects:
            template_path = templates_dir / f"{_slugify(project['name'])}.md"
            template_path.write_text(_render_template(project), encoding="utf-8")

    def _iter_mixed_projects(self, results: Iterable[ProjectScanResult]):
        for result in results:
            mixed_issues = [
                issue
                for issue in result.issues
                if issue.issue_type == ValidationIssueType.MIXED_CODE
            ]
            if not mixed_issues:
                continue
            project = result.project
            yield {
                "name": project.name,
                "relativePath": project.relative_path,
                "patternType": project.pattern_type.value,
                "issues": [issue.to_dict() for issue in mixed_issues],
            }


def _to_json(payload: dict) -> str:
    import json

    return json.dumps(payload, indent=2)


def _render_template(project_payload: dict) -> str:
    bullets = "\n".join(f"- {issue['details']}" for issue in project_payload["issues"])
    return (
        f"# Mixed Test Code Escalation – {project_payload['name']}\n\n"
        f"**Project**: {project_payload['relativePath']}\n\n"
        "## Evidence\n"
        f"{bullets}\n\n"
        "## Required actions\n"
        "1. Split test helpers into a dedicated *Tests project.\n"
        "2. Remove mixed-code files from the production assembly.\n"
        "3. Re-run the audit CLI and attach the updated report before closing this escalation.\n"
    )


def _slugify(value: str) -> str:
    safe = [ch if ch.isalnum() or ch in ("-", "_") else "-" for ch in value]
    slug = "".join(safe).strip("-")
    return slug or "project"
