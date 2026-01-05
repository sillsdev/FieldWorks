"""Shared data structures for the GenerateAssemblyInfo automation suite."""

from __future__ import annotations

from dataclasses import dataclass, field, asdict
from pathlib import Path
from typing import List, Optional, Literal, Dict, Any

Category = Literal["T", "C", "G"]
RemediationState = Literal[
    "AuditPending", "NeedsRemediation", "Remediated", "Validated"
]
FindingCode = Literal[
    "MissingTemplateImport",
    "GenerateAssemblyInfoTrue",
    "MissingAssemblyInfoFile",
    "DuplicateCompileEntry",
]
Severity = Literal["Error", "Warning", "Info"]


@dataclass
class AssemblyInfoFile:
    """Represents a per-project AssemblyInfo file and its metadata."""

    project_id: str
    path: Path
    relative_path: str
    custom_attributes: List[str] = field(default_factory=list)
    has_conditional_blocks: bool = False
    restored_from_commit: Optional[str] = None
    release_ref_name: Optional[str] = None
    present_in_release_ref: Optional[bool] = None
    last_commit_sha: Optional[str] = None
    last_commit_date: Optional[str] = None
    last_commit_author: Optional[str] = None

    def to_dict(self) -> Dict[str, Any]:
        data = asdict(self)
        data["path"] = str(self.path)
        return data


@dataclass
class TemplateLink:
    """Tracks how a project links CommonAssemblyInfo."""

    project_id: str
    include_path: str
    link_alias: str = "Properties\\CommonAssemblyInfo.cs"
    comment: str = "Using CommonAssemblyInfoTemplate; prevent SDK duplication."


@dataclass
class ManagedProject:
    """Projection of a .csproj file used across audit/convert/validate phases."""

    project_id: str
    path: Path
    category: Category
    template_imported: bool
    has_custom_assembly_info: bool
    generate_assembly_info_value: Optional[bool]
    remediation_state: RemediationState = "AuditPending"
    notes: str = ""
    assembly_info_files: List[AssemblyInfoFile] = field(default_factory=list)
    release_ref_has_custom_files: Optional[bool] = None
    latest_custom_commit_sha: Optional[str] = None
    latest_custom_commit_date: Optional[str] = None

    def to_dict(self) -> Dict[str, Any]:
        data = asdict(self)
        data["path"] = str(self.path)
        data["assembly_info_files"] = [f.to_dict() for f in self.assembly_info_files]
        return data


@dataclass
class ValidationFinding:
    """Represents a single validation error or warning."""

    project_id: str
    finding_code: FindingCode
    severity: Severity = "Error"
    details: str = ""

    def to_dict(self) -> Dict[str, Any]:
        return asdict(self)


@dataclass
class RestoreInstruction:
    """Specifies where to recover deleted AssemblyInfo files from git history."""

    project_id: str
    relative_path: str
    commit_sha: str

    def to_dict(self) -> Dict[str, Any]:
        return asdict(self)


@dataclass
class RemediationScriptRun:
    """Captures audit/convert/validate executions for traceability."""

    script: Literal["audit", "convert", "validate"]
    timestamp: str
    input_artifacts: List[str] = field(default_factory=list)
    output_artifacts: List[str] = field(default_factory=list)
    exit_code: int = 0

    def to_dict(self) -> Dict[str, Any]:
        return asdict(self)
