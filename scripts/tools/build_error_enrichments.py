"""Registry of known MSBuild error patterns with on-disk follow-up searches.

Used by analyze_build_log.py to enrich errors with root-cause context.
"""

# To add a new enrichment:
# 1. Define a compiled regex with named groups that matches the error message.
# 2. Write an enrich function: (groups, log_path, workspace_root, error_line) -> list[RootCauseFinding]
# 3. Append an ErrorEnrichment(name, error_pattern, enrich) to REGISTRY.

import re
from dataclasses import dataclass
from pathlib import Path, PureWindowsPath
from typing import Callable, Optional


@dataclass
class RootCauseFinding:
    """A single enriched finding to surface in the report."""
    error_code: str
    error_line: int
    summary: str
    detail: list[str]


@dataclass
class ErrorEnrichment:
    """Pairs an error regex with a function that produces root-cause findings."""
    name: str
    error_pattern: re.Pattern
    enrich: Callable


# ---------------------------------------------------------------------------
# Helpers
# ---------------------------------------------------------------------------

def _detect_encoding(path: Path) -> str:
    """Return a safe text encoding based on BOM detection."""
    with open(path, "rb") as fh:
        prefix = fh.read(4)
    if prefix.startswith(b"\xff\xfe") or prefix.startswith(b"\xfe\xff"):
        return "utf-16"
    if prefix.startswith(b"\xef\xbb\xbf"):
        return "utf-8-sig"
    return "utf-8"


_WIX_FILE_RX = re.compile(r'<File\b[^>]*\bSource="(?P<source>[^"]+)"', re.IGNORECASE)


def _find_component_file_in_wxs(wxs_text: str, component_id: str) -> Optional[str]:
    """Find the Source filename for a component's <File> element in wxs text."""
    comp_rx = re.compile(
        r'<Component\b[^>]*\bId="' + re.escape(component_id) + r'"',
        re.IGNORECASE,
    )
    m = comp_rx.search(wxs_text)
    if not m:
        return None
    snippet = wxs_text[m.start(): m.start() + 2000]
    fm = _WIX_FILE_RX.search(snippet)
    if not fm:
        return None
    raw = fm.group("source")
    return Path(raw).name if ("\\" in raw or "/" in raw) else raw


def _resolve_build_path(windows_path: str, workspace_root: Optional[Path]) -> Optional[Path]:
    """Resolve an absolute Windows build path to a file under workspace_root."""
    if not workspace_root:
        return None
    parts = PureWindowsPath(windows_path).parts
    rel_parts = [p for p in parts if p not in ('\\', '/') and ':' not in p]
    for length in range(len(rel_parts), 0, -1):
        candidate = workspace_root.joinpath(*rel_parts[-length:])
        if candidate.exists():
            return candidate
    return None


# ---------------------------------------------------------------------------
# WiX / Pyro enrichments
# ---------------------------------------------------------------------------

_PYRO0305_RX = re.compile(
    r"(?P<wxs_path>[A-Za-z]:\\[^\s(]+\.wxs)"
    r".*?error PYRO0305.*?"
    r"Removing component '(?P<component_id>[^']+)' from feature '(?P<feature>[^']+)'",
    re.IGNORECASE,
)


def _enrich_pyro0305(groups: dict, log_path: Path, workspace_root: Optional[Path], error_line: int) -> list[RootCauseFinding]:
    """Look up which file a removed WiX component references."""
    component_id = groups["component_id"]
    feature = groups["feature"]
    wxs_path_str = groups.get("wxs_path", "")

    if workspace_root and wxs_path_str:
        resolved = _resolve_build_path(wxs_path_str, workspace_root)
        if resolved:
            try:
                enc = _detect_encoding(resolved)
                wxs_text = resolved.read_text(encoding=enc, errors="replace")
                file_name = _find_component_file_in_wxs(wxs_text, component_id)
                if file_name:
                    return [RootCauseFinding(
                        error_code="PYRO0305",
                        error_line=error_line,
                        summary=f"The File '{file_name}' was removed in the patch.",
                        detail=[
                            f"    Component : {component_id}",
                            f"    Feature   : {feature}",
                            f"    Source    : {resolved.name}",
                        ],
                    )]
                else:
                    return [RootCauseFinding(
                        error_code="PYRO0305",
                        error_line=error_line,
                        summary=f"Component '{component_id}' found in {resolved.name} but no <File Source=...> element located nearby.",
                        detail=[f"    Feature : {feature}", f"    Source  : {resolved.name}"],
                    )]
            except Exception:
                pass

    return [RootCauseFinding(
        error_code="PYRO0305",
        error_line=error_line,
        summary=f"Component '{component_id}' removed from feature '{feature}' — could not locate {Path(wxs_path_str).name if wxs_path_str else 'the .wxs file'} on disk.",
        detail=[],
    )]


# ---------------------------------------------------------------------------
# Bundled-download enrichments
# ---------------------------------------------------------------------------

_DOWNLOAD_RETRY_RX = re.compile(
    r"Could not retrieve latest\s+(?P<url>https?://\S+?)\s*\.\s*Exceeded retry count",
    re.IGNORECASE,
)


def _enrich_download_retry(groups: dict, log_path: Path, workspace_root: Optional[Path], error_line: int) -> list[RootCauseFinding]:
    """Name the missing bundled download and locate where its address is pinned."""
    url = groups["url"]
    file_name = url.rstrip("/").rsplit("/", 1)[-1]

    pins = []
    if workspace_root:
        build_dir = Path(workspace_root) / "Build"
        if build_dir.is_dir():
            for targets_file in sorted(build_dir.glob("*.targets")):
                try:
                    text = targets_file.read_text(
                        encoding=_detect_encoding(targets_file), errors="replace")
                except OSError:
                    continue
                for lnum, line in enumerate(text.splitlines(), 1):
                    if url in line:
                        pins.append(f"    Pinned at : Build/{targets_file.name}:{lnum}")

    detail = pins + [
        f"    URL       : {url}",
        "    The server did not return the file (download retries exhausted, likely 404).",
        "    Upload the file to the server, or repoint the pinned address to a published version.",
    ]
    return [RootCauseFinding(
        error_code="DownloadFile",
        error_line=error_line,
        summary=f"Bundled download '{file_name}' is not available at the pinned address.",
        detail=detail,
    )]


# ---------------------------------------------------------------------------
# Registry
# ---------------------------------------------------------------------------

REGISTRY: list[ErrorEnrichment] = [
    ErrorEnrichment(
        name="WiX: component removed from feature (PYRO0305)",
        error_pattern=_PYRO0305_RX,
        enrich=_enrich_pyro0305,
    ),
    ErrorEnrichment(
        name="Bundled installer download failed (DownloadFile retry exhaustion)",
        error_pattern=_DOWNLOAD_RETRY_RX,
        enrich=_enrich_download_retry,
    ),
]


# ---------------------------------------------------------------------------
# Public API
# ---------------------------------------------------------------------------

def run_enrichments(steps: list, log_path: Path, workspace_root: Optional[Path] = None) -> list[RootCauseFinding]:
    """Match errors against REGISTRY and return deduplicated root-cause findings."""
    findings = []
    seen: set[tuple[str, str]] = set()
    for step in steps:
        for err in step.errors:
            for entry in REGISTRY:
                m = entry.error_pattern.search(err.message)
                if m:
                    for finding in entry.enrich(m.groupdict(), log_path, workspace_root, err.line_num):
                        key = (finding.error_code, finding.summary)
                        if key not in seen:
                            seen.add(key)
                            findings.append(finding)
    return findings
