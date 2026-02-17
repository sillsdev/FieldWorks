"""Shared helpers for RegFree COM tooling."""

from __future__ import annotations

from dataclasses import dataclass
from pathlib import Path
from typing import Iterable, List, Optional
import json

REPO_ROOT = Path(__file__).resolve().parents[2]
OUTPUT_ROOT = REPO_ROOT / "Output"
PROJECT_MAP_FILE = Path(__file__).resolve().with_name("project_map.json")


@dataclass(frozen=True)
class ExecutableInfo:
    """Metadata describing each executable that may need RegFree manifests."""

    id: str
    project_path: str
    output_name: str
    priority: str

    def csproj_path(self) -> Path:
        return REPO_ROOT / self.project_path

    def output_path(self, configuration: str = "Debug", platform: str = "x64") -> Path:
        return OUTPUT_ROOT / configuration / self.output_name

    def manifest_path(
        self, configuration: str = "Debug", platform: str = "x64"
    ) -> Path:
        return self.output_path(configuration, platform).with_suffix(".exe.manifest")


_EXECUTABLES: List[ExecutableInfo] = [
    ExecutableInfo(
        "FieldWorks", "Src/Common/FieldWorks/FieldWorks.csproj", "FieldWorks.exe", "P0"
    ),
    ExecutableInfo(
        "ComManifestTestHost",
        "Src/Utilities/ComManifestTestHost/ComManifestTestHost.csproj",
        "ComManifestTestHost.exe",
        "Test",
    ),
    ExecutableInfo(
        "LCMBrowser", "Src/LCMBrowser/LCMBrowser.csproj", "LCMBrowser.exe", "P1"
    ),
    ExecutableInfo(
        "UnicodeCharEditor",
        "Src/UnicodeCharEditor/UnicodeCharEditor.csproj",
        "UnicodeCharEditor.exe",
        "P1",
    ),
    ExecutableInfo(
        "MigrateSqlDbs",
        "Src/MigrateSqlDbs/MigrateSqlDbs.csproj",
        "MigrateSqlDbs.exe",
        "P2",
    ),
    ExecutableInfo(
        "FixFwData", "Src/Utilities/FixFwData/FixFwData.csproj", "FixFwData.exe", "P2"
    ),
    ExecutableInfo("FxtExe", "Src/FXT/FxtExe/FxtExe.csproj", "FxtExe.exe", "P2"),
    ExecutableInfo(
        "ConverterConsole",
        "Lib/src/Converter/ConvertConsole/ConverterConsole.csproj",
        "ConverterConsole.exe",
        "P3",
    ),
    ExecutableInfo(
        "Converter",
        "Lib/src/Converter/Converter/Converter.csproj",
        "Converter.exe",
        "P3",
    ),
    ExecutableInfo(
        "ConvertSFM",
        "Src/Utilities/SfmToXml/ConvertSFM/ConvertSFM.csproj",
        "ConvertSFM.exe",
        "P3",
    ),
    ExecutableInfo(
        "SfmStats", "Src/Utilities/SfmStats/SfmStats.csproj", "SfmStats.exe", "P3"
    ),
]


def iter_executables(priority: Optional[str] = None) -> Iterable[ExecutableInfo]:
    """Yield executables, optionally filtering by priority."""

    if priority is None:
        yield from _EXECUTABLES
        return

    for exe in _EXECUTABLES:
        if exe.priority == priority:
            yield exe


def get_executable(exe_id: str) -> ExecutableInfo:
    """Return metadata for the requested executable id."""

    for exe in _EXECUTABLES:
        if exe.id.lower() == exe_id.lower():
            return exe
    raise KeyError(f"Unknown executable id: {exe_id}")


def export_project_map(destination: Path = PROJECT_MAP_FILE) -> None:
    """Persist the metadata into JSON for non-Python consumers."""

    payload = [
        {
            "Id": exe.id,
            "ProjectPath": exe.project_path,
            "OutputPath": f"Output/{{configuration}}/{exe.output_name}",
            "ExeName": exe.output_name,
            "Priority": exe.priority,
        }
        for exe in _EXECUTABLES
    ]
    destination.write_text(json.dumps(payload, indent=2))


def load_project_map(source: Path = PROJECT_MAP_FILE) -> List[ExecutableInfo]:
    """Load executable metadata from JSON, falling back to the baked-in defaults."""

    if not source.exists():
        return list(_EXECUTABLES)

    data = json.loads(source.read_text())
    result = []
    for item in data:
        exe_name = item.get("ExeName")
        output_path = item.get("OutputPath")
        if output_path and not exe_name:
            exe_name = Path(output_path).name
        if not exe_name:
            exe_name = f"{item['Id']}.exe"

        result.append(
            ExecutableInfo(
                item["Id"],
                item["ProjectPath"],
                exe_name,
                item.get("Priority", "P3"),
            )
        )
    return result


__all__ = [
    "ExecutableInfo",
    "REPO_ROOT",
    "OUTPUT_ROOT",
    "PROJECT_MAP_FILE",
    "iter_executables",
    "get_executable",
    "export_project_map",
    "load_project_map",
]
