from __future__ import annotations

import csv
import json
from dataclasses import asdict
from datetime import datetime, timezone
from pathlib import Path
from typing import Iterable, List

from .models import ProjectScanResult


class ReportWriter:
    """Persist audit results in JSON + CSV formats."""

    def __init__(self, repo_root: Path) -> None:
        self.repo_root = repo_root

    def write_reports(
        self,
        results: Iterable[ProjectScanResult],
        json_path: Path,
        csv_path: Path | None = None,
    ) -> None:
        materialized = list(results)
        json_payload = self._build_json_payload(materialized)
        self._write_json(json_path, json_payload)
        if csv_path:
            self._write_csv(csv_path, materialized)

    def _build_json_payload(self, results: List[ProjectScanResult]) -> dict:
        return {
            "generatedAt": datetime.now(timezone.utc).isoformat(),
            "projectCount": len(results),
            "projects": [result.summary() for result in results],
        }

    def _write_json(self, path: Path, payload: dict) -> None:
        path.parent.mkdir(parents=True, exist_ok=True)
        path.write_text(json.dumps(payload, indent=2), encoding="utf-8")

    def _write_csv(self, path: Path, results: List[ProjectScanResult]) -> None:
        path.parent.mkdir(parents=True, exist_ok=True)
        fieldnames = [
            "projectName",
            "relativePath",
            "patternType",
            "hasMixedCode",
            "issueCount",
        ]
        with path.open("w", encoding="utf-8", newline="") as fp:
            writer = csv.DictWriter(fp, fieldnames=fieldnames)
            writer.writeheader()
            for result in results:
                project = result.project
                writer.writerow(
                    {
                        "projectName": project.name,
                        "relativePath": project.relative_path,
                        "patternType": project.pattern_type.value,
                        "hasMixedCode": project.has_mixed_code,
                        "issueCount": len(result.issues),
                    }
                )
