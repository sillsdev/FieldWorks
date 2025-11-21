from __future__ import annotations

from dataclasses import dataclass, field
from pathlib import Path
from typing import List

from . import repo_scanner
from .models import (
    PatternType,
    ValidationIssue,
    ValidationIssueType,
    ValidationSeverity,
)


@dataclass
class ValidationSummary:
    total_projects: int = 0
    passed_projects: int = 0
    failed_projects: int = 0
    issues: List[ValidationIssue] = field(default_factory=list)

    @property
    def error_count(self) -> int:
        return sum(1 for i in self.issues if i.severity == ValidationSeverity.ERROR)

    @property
    def warning_count(self) -> int:
        return sum(1 for i in self.issues if i.severity == ValidationSeverity.WARNING)


class Validator:
    """Enforces Test Exclusion Policy (Pattern A)."""

    def __init__(self, repo_root: Path) -> None:
        self.repo_root = repo_root

    def validate_repo(self) -> ValidationSummary:
        results = repo_scanner.scan_repository(self.repo_root)
        summary = ValidationSummary(total_projects=len(results))

        for result in results:
            project_issues = list(result.issues)  # Start with scanner issues

            # Enforce Pattern A
            is_test_project = result.project.name.endswith(
                "Tests"
            ) or result.project.name.endswith("Test")
            has_test_folders = bool(result.test_folders)
            pattern = result.project.pattern_type

            if has_test_folders and not is_test_project:
                if pattern != PatternType.PATTERN_A:
                    # If it's B or C, it's a violation of "Pattern A only" policy.
                    issue_type = (
                        ValidationIssueType.WILDCARD_DETECTED
                        if pattern == PatternType.PATTERN_B
                        else ValidationIssueType.MISSING_EXCLUSION
                    )
                    details = f"Project uses {pattern.value} pattern but must use Pattern A (explicit exclusions)."
                    project_issues.append(
                        ValidationIssue(
                            project_name=result.project.name,
                            issue_type=issue_type,
                            severity=ValidationSeverity.ERROR,
                            details=details,
                        )
                    )
            else:
                # No test folders.
                if pattern == PatternType.PATTERN_B:
                    project_issues.append(
                        ValidationIssue(
                            project_name=result.project.name,
                            issue_type=ValidationIssueType.WILDCARD_DETECTED,
                            severity=ValidationSeverity.ERROR,
                            details="Project has wildcard exclusions but no test folders detected. Remove the wildcard.",
                        )
                    )

            if project_issues:
                summary.failed_projects += 1
                summary.issues.extend(project_issues)
            else:
                summary.passed_projects += 1

        return summary
