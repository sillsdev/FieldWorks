from __future__ import annotations

import json
import os
from dataclasses import dataclass, field
from pathlib import Path
from typing import Dict, Iterable, List, Optional, Set


_DEFAULT_TIMEOUT_SECONDS = 120
_DEFAULT_OUTPUT_CAP_BYTES = 1_000_000  # 1 MB
_DEFAULT_ALLOWED_TOOLS = {
    "Git-Search",
    "Read-FileContent",
    "Invoke-InContainer",
    "Invoke-AgentTask",
    "build",
    "test",
    "Copilot-Detect",
    "Copilot-Plan",
    "Copilot-Apply",
    "Copilot-Validate",
}


@dataclass
class ToolSelection:
    allow: Set[str] = field(default_factory=set)
    deny: Set[str] = field(default_factory=set)

    @classmethod
    def from_dict(cls, data: Dict[str, Iterable[str]]) -> "ToolSelection":
        allow = set(data.get("allow", []) or [])
        deny = set(data.get("deny", []) or [])
        return cls(allow=allow, deny=deny)

    def is_allowed(self, name: str) -> bool:
        if self.deny and name in self.deny:
            return False
        if self.allow:
            return name in self.allow
        return True


@dataclass
class ServerConfig:
    repo_root: Path
    agent_scripts_dir: Path
    timeout_seconds: int = _DEFAULT_TIMEOUT_SECONDS
    output_cap_bytes: int = _DEFAULT_OUTPUT_CAP_BYTES
    working_dir: Optional[Path] = None
    tools: ToolSelection = field(default_factory=ToolSelection)
    extra_tools: Dict[str, Path] = field(default_factory=dict)

    @classmethod
    def defaults(cls, repo_root: Path) -> "ServerConfig":
        return cls(
            repo_root=repo_root,
            agent_scripts_dir=repo_root / "scripts" / "Agent",
            timeout_seconds=_DEFAULT_TIMEOUT_SECONDS,
            output_cap_bytes=_DEFAULT_OUTPUT_CAP_BYTES,
            working_dir=repo_root,
            tools=ToolSelection(allow=set(_DEFAULT_ALLOWED_TOOLS)),
            extra_tools={
                "build": repo_root / "build.ps1",
                "test": repo_root / "test.ps1",
            },
        )

    def apply_override(self, other: "ServerConfig") -> None:
        self.timeout_seconds = other.timeout_seconds or self.timeout_seconds
        self.output_cap_bytes = other.output_cap_bytes or self.output_cap_bytes
        self.working_dir = other.working_dir or self.working_dir
        if other.tools.allow:
            self.tools.allow = other.tools.allow
        if other.tools.deny:
            self.tools.deny = other.tools.deny
        if other.extra_tools:
            self.extra_tools.update(other.extra_tools)


def _read_json(path: Path) -> Dict:
    try:
        return json.loads(path.read_text(encoding="utf-8"))
    except FileNotFoundError:
        return {}


def load_config(repo_root: Path, config_path: Optional[Path] = None) -> ServerConfig:
    base = ServerConfig.defaults(repo_root)
    if not config_path:
        return base

    data = _read_json(config_path)
    override = ServerConfig(
        repo_root=repo_root,
        agent_scripts_dir=Path(data.get("agent_scripts_dir", base.agent_scripts_dir)),
        timeout_seconds=int(data.get("timeout_seconds", base.timeout_seconds)),
        output_cap_bytes=int(data.get("output_cap_bytes", base.output_cap_bytes)),
        working_dir=Path(data["working_dir"]) if data.get("working_dir") else base.working_dir,
        tools=ToolSelection.from_dict(data.get("tools", {})),
        extra_tools={k: Path(v) for k, v in (data.get("extra_tools", {}) or {}).items()},
    )

    base.apply_override(override)
    return base


def repo_root_from_env() -> Path:
    root = os.getenv("REPO_ROOT")
    return Path(root) if root else Path.cwd()
