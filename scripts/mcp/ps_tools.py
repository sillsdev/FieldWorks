from __future__ import annotations

import subprocess
from pathlib import Path
from typing import Any, Callable, Dict, Iterable, List, Optional

from .config import ServerConfig


class ToolDescriptor:
    def __init__(
        self,
        name: str,
        path: Path,
        description: str = "",
        parameters: Optional[Dict[str, Any]] = None,
        arg_builder: Optional[Callable[[Any], List[str]]] = None,
    ):
        self.name = name
        self.path = path
        self.description = description
        self.parameters = parameters or {}
        self.arg_builder = arg_builder

    def to_dict(self) -> Dict:
        return {
            "name": self.name,
            "description": self.description,
            "path": str(self.path),
            "parameters": self.parameters,
        }

    def build_args(self, payload: Any) -> List[str]:
        if payload is None:
            return []
        if isinstance(payload, dict) and self.arg_builder:
            return self.arg_builder(payload)
        if isinstance(payload, list):
            return [str(x) for x in payload]
        return [str(payload)]


def _append_arg(args: List[str], name: str, value: Any) -> None:
    if value is None:
        return
    if isinstance(value, bool):
        if value:
            args.append(f"-{name}")
        return
    if isinstance(value, (list, tuple)):
        if not value:
            return
        args.append(f"-{name}")
        args.extend(str(v) for v in value)
        return
    args.extend([f"-{name}", str(value)])


def _git_search_args(payload: Dict[str, Any]) -> List[str]:
    args: List[str] = []
    _append_arg(args, "Action", payload.get("action"))
    _append_arg(args, "Ref", payload.get("ref"))
    _append_arg(args, "Path", payload.get("path"))
    _append_arg(args, "Pattern", payload.get("pattern"))
    _append_arg(args, "RepoPath", payload.get("repoPath"))
    _append_arg(args, "HeadLines", payload.get("headLines"))
    _append_arg(args, "TailLines", payload.get("tailLines"))
    _append_arg(args, "Context", payload.get("context"))
    return args


def _read_file_args(payload: Dict[str, Any]) -> List[str]:
    args: List[str] = []
    _append_arg(args, "Path", payload.get("path"))
    _append_arg(args, "HeadLines", payload.get("headLines"))
    _append_arg(args, "TailLines", payload.get("tailLines"))
    _append_arg(args, "LineNumbers", payload.get("lineNumbers"))
    _append_arg(args, "Pattern", payload.get("pattern"))
    return args


def _build_args(payload: Dict[str, Any]) -> List[str]:
    args: List[str] = []
    _append_arg(args, "Configuration", payload.get("configuration"))
    _append_arg(args, "Platform", payload.get("platform"))
    _append_arg(args, "BuildTests", payload.get("buildTests"))
    _append_arg(args, "MsBuildArgs", payload.get("msBuildArgs"))
    _append_arg(args, "Action", payload.get("action"))
    _append_arg(args, "LogFile", payload.get("logFile"))
    _append_arg(args, "UseTraversal", payload.get("useTraversal"))
    return args


def _test_args(payload: Dict[str, Any]) -> List[str]:
    args: List[str] = []
    _append_arg(args, "TestProject", payload.get("testProject"))
    _append_arg(args, "TestFilter", payload.get("testFilter"))
    _append_arg(args, "NoBuild", payload.get("noBuild"))
    _append_arg(args, "Configuration", payload.get("configuration"))
    _append_arg(args, "Platform", payload.get("platform"))
    return args


def _copilot_detect_args(payload: Dict[str, Any]) -> List[str]:
    args: List[str] = []
    _append_arg(args, "Base", payload.get("base"))
    _append_arg(args, "Out", payload.get("out"))
    return args


def _copilot_plan_args(payload: Dict[str, Any]) -> List[str]:
    args: List[str] = []
    _append_arg(args, "DetectJson", payload.get("detectJson"))
    _append_arg(args, "Out", payload.get("out"))
    _append_arg(args, "Base", payload.get("base"))
    return args


def _copilot_apply_args(payload: Dict[str, Any]) -> List[str]:
    args: List[str] = []
    _append_arg(args, "Plan", payload.get("plan"))
    _append_arg(args, "Folders", payload.get("folders"))
    return args


def _copilot_validate_args(payload: Dict[str, Any]) -> List[str]:
    args: List[str] = []
    _append_arg(args, "Base", payload.get("base"))
    _append_arg(args, "Paths", payload.get("paths"))
    return args


class ToolDiscovery:
    def __init__(self, config: ServerConfig):
        self.config = config

    def discover(self) -> List[ToolDescriptor]:
        tools: List[ToolDescriptor] = []
        # Agent scripts
        if self.config.agent_scripts_dir.exists():
            for path in sorted(self.config.agent_scripts_dir.glob("*.ps1")):
                name = path.stem
                if not self.config.tools.is_allowed(name):
                    continue
                if name == "Git-Search":
                    tools.append(
                        ToolDescriptor(
                            name=name,
                            path=path,
                            description="Git helper (show/diff/log/search/etc)",
                            parameters={
                                "action": "show|diff|log|blame|search|branches|files",
                                "ref": "Git ref (default HEAD)",
                                "path": "File or directory path",
                                "pattern": "Search or branch pattern",
                                "repoPath": "Override repository path",
                                "headLines": "Head line limit",
                                "tailLines": "Tail line limit",
                                "context": "Search context lines",
                            },
                            arg_builder=_git_search_args,
                        )
                    )
                elif name == "Read-FileContent":
                    tools.append(
                        ToolDescriptor(
                            name=name,
                            path=path,
                            description="Read file with optional head/tail and filtering",
                            parameters={
                                "path": "File path",
                                "headLines": "Head line limit",
                                "tailLines": "Tail line limit",
                                "lineNumbers": "Include line numbers",
                                "pattern": "Regex filter",
                            },
                            arg_builder=_read_file_args,
                        )
                    )
                elif name == "Copilot-Detect":
                    tools.append(
                        ToolDescriptor(
                            name=name,
                            path=path,
                            description="Detect COPILOT.md updates needed",
                            parameters={
                                "base": "Base ref (optional)",
                                "out": "Output path (optional)",
                            },
                            arg_builder=_copilot_detect_args,
                        )
                    )
                elif name == "Copilot-Plan":
                    tools.append(
                        ToolDescriptor(
                            name=name,
                            path=path,
                            description="Plan COPILOT.md updates",
                            parameters={
                                "detectJson": "detect_copilot_needed output",
                                "out": "Plan output path",
                                "base": "Fallback base ref",
                            },
                            arg_builder=_copilot_plan_args,
                        )
                    )
                elif name == "Copilot-Apply":
                    tools.append(
                        ToolDescriptor(
                            name=name,
                            path=path,
                            description="Apply COPILOT plan changes",
                            parameters={
                                "plan": "Plan JSON path",
                                "folders": "Folders filter (optional)",
                            },
                            arg_builder=_copilot_apply_args,
                        )
                    )
                elif name == "Copilot-Validate":
                    tools.append(
                        ToolDescriptor(
                            name=name,
                            path=path,
                            description="Validate COPILOT docs",
                            parameters={
                                "base": "Base ref (optional)",
                                "paths": "Paths filter (optional)",
                            },
                            arg_builder=_copilot_validate_args,
                        )
                    )
                else:
                    tools.append(ToolDescriptor(name=name, path=path, description=f"Agent script {path.name}"))
        # Extra root tools (build/test)
        for name, path in self.config.extra_tools.items():
            if not self.config.tools.is_allowed(name):
                continue
            if name == "build":
                tools.append(
                    ToolDescriptor(
                        name=name,
                        path=path,
                        description="FieldWorks build wrapper (build.ps1)",
                        parameters={
                            "configuration": "Debug/Release",
                            "platform": "x64",
                            "buildTests": "bool",
                            "msBuildArgs": "array of msbuild args",
                            "action": "optional build action",
                            "logFile": "output log file",
                            "useTraversal": "bool"
                        },
                        arg_builder=_build_args,
                    )
                )
            elif name == "test":
                tools.append(
                    ToolDescriptor(
                        name=name,
                        path=path,
                        description="FieldWorks test wrapper (test.ps1)",
                        parameters={
                            "testProject": "specific test project",
                            "testFilter": "vstest filter expression",
                            "noBuild": "bool",
                            "configuration": "Debug/Release",
                            "platform": "x64",
                        },
                        arg_builder=_test_args,
                    )
                )
            else:
                tools.append(ToolDescriptor(name=name, path=path, description=f"Root script {path.name}"))
        return tools


class ToolRunner:
    def __init__(self, config: ServerConfig):
        self.config = config

    def run(self, tool: ToolDescriptor, args: Any = None) -> Dict:
        args = tool.build_args(args)
        command = [
            "powershell",
            "-NoProfile",
            "-ExecutionPolicy",
            "Bypass",
            "-File",
            str(tool.path),
        ] + args

        try:
            proc = subprocess.run(
                command,
                cwd=self.config.working_dir,
                capture_output=True,
                text=True,
                timeout=self.config.timeout_seconds,
            )
            stdout, stderr = self._cap(proc.stdout), self._cap(proc.stderr)
            truncated = len(proc.stdout.encode("utf-8")) > self.config.output_cap_bytes or len(proc.stderr.encode("utf-8")) > self.config.output_cap_bytes
            return {
                "stdout": stdout,
                "stderr": stderr,
                "exitCode": proc.returncode,
                "truncated": truncated,
                "timedOut": False,
            }
        except subprocess.TimeoutExpired as ex:
            stdout, stderr = self._cap(ex.stdout or ""), self._cap(ex.stderr or "")
            return {
                "stdout": stdout,
                "stderr": stderr or "Timed out",
                "exitCode": -1,
                "truncated": False,
                "timedOut": True,
            }

    def _cap(self, text: str) -> str:
        data = text.encode("utf-8")
        if len(data) <= self.config.output_cap_bytes:
            return text
        return data[: self.config.output_cap_bytes].decode("utf-8", errors="ignore") + "\n[truncated]"
