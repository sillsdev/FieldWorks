from __future__ import annotations

from dataclasses import dataclass, field
from datetime import datetime, timezone
from enum import Enum
from typing import Any, Dict, List, Optional


class PatternType(str, Enum):
    """Enumeration of supported exclusion patterns."""

    PATTERN_A = "A"
    PATTERN_B = "B"
    PATTERN_C = "C"
    NONE = "None"


class ProjectStatus(str, Enum):
    """Processing status for a project within the convergence plan."""

    PENDING = "Pending"
    CONVERTED = "Converted"
    FLAGGED = "Flagged"


class ExclusionScope(str, Enum):
    """Scope for an exclusion rule (Compile, None, or both)."""

    COMPILE = "Compile"
    NONE = "None"
    BOTH = "Both"


class RuleSource(str, Enum):
    """Indicates whether an exclusion rule was authored manually or generated."""

    EXPLICIT = "Explicit"
    GENERATED = "Generated"


class ValidationIssueType(str, Enum):
    """Categories of issues surfaced by the validator/auditor."""

    MISSING_EXCLUSION = "MissingExclusion"
    MIXED_CODE = "MixedCode"
    WILDCARD_DETECTED = "WildcardDetected"
    SCRIPT_ERROR = "ScriptError"


class ValidationSeverity(str, Enum):
    """Severity ladder for validation issues."""

    WARNING = "Warning"
    ERROR = "Error"


def _utcnow() -> datetime:
    return datetime.now(timezone.utc)


@dataclass(slots=True)
class TestFolder:
    __test__ = False  # Prevent pytest from treating this as a test case.

    project_name: str
    relative_path: str
    depth: int
    contains_source: bool
    excluded: bool

    def to_dict(self) -> Dict[str, Any]:
        return {
            "projectName": self.project_name,
            "relativePath": self.relative_path,
            "depth": self.depth,
            "containsSource": self.contains_source,
            "excluded": self.excluded,
        }


@dataclass(slots=True)
class ExclusionRule:
    project_name: str
    pattern: str
    scope: ExclusionScope
    source: RuleSource = RuleSource.EXPLICIT
    covers_nested: bool = True

    def to_dict(self) -> Dict[str, Any]:
        return {
            "projectName": self.project_name,
            "pattern": self.pattern,
            "scope": self.scope.value,
            "source": self.source.value,
            "coversNested": self.covers_nested,
        }


@dataclass(slots=True)
class ValidationIssue:
    project_name: str
    issue_type: ValidationIssueType
    severity: ValidationSeverity
    details: str
    detected_on: datetime = field(default_factory=_utcnow)
    resolved: bool = False

    def to_dict(self) -> Dict[str, Any]:
        return {
            "projectName": self.project_name,
            "issueType": self.issue_type.value,
            "severity": self.severity.value,
            "details": self.details,
            "detectedOn": self.detected_on.isoformat() + "Z",
            "resolved": self.resolved,
        }


@dataclass(slots=True)
class Project:
    name: str
    relative_path: str
    pattern_type: PatternType = PatternType.NONE
    has_mixed_code: bool = False
    status: ProjectStatus = ProjectStatus.PENDING
    last_validated: Optional[datetime] = None

    def to_dict(self) -> Dict[str, Any]:
        payload: Dict[str, Any] = {
            "name": self.name,
            "relativePath": self.relative_path,
            "patternType": self.pattern_type.value,
            "status": self.status.value,
            "hasMixedCode": self.has_mixed_code,
        }
        if self.last_validated:
            payload["lastValidated"] = self.last_validated.isoformat() + "Z"
        return payload


@dataclass(slots=True)
class ConversionJob:
    job_id: str
    initiated_by: str
    project_list: List[str]
    script_version: str
    start_time: datetime
    end_time: Optional[datetime] = None
    result: str = "Pending"

    def to_dict(self) -> Dict[str, Any]:
        payload: Dict[str, Any] = {
            "jobId": self.job_id,
            "initiatedBy": self.initiated_by,
            "projectList": self.project_list,
            "scriptVersion": self.script_version,
            "startTime": self.start_time.isoformat() + "Z",
            "result": self.result,
        }
        if self.end_time:
            payload["endTime"] = self.end_time.isoformat() + "Z"
        return payload


@dataclass(slots=True)
class ProjectScanResult:
    project: Project
    test_folders: List[TestFolder]
    exclusion_rules: List[ExclusionRule]
    issues: List[ValidationIssue] = field(default_factory=list)

    def summary(self) -> Dict[str, Any]:
        return {
            "project": self.project.to_dict(),
            "testFolders": [folder.to_dict() for folder in self.test_folders],
            "rules": [rule.to_dict() for rule in self.exclusion_rules],
            "issues": [issue.to_dict() for issue in self.issues],
        }
