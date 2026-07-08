"""Analyze an MSBuild log to identify root-cause failures.

Streams the log in a single pass, detects project boundaries and errors,
and writes a GitHub Actions job summary with annotations.
"""

import argparse
import os
import re
import sys
from build_error_enrichments import run_enrichments
from collections import deque
from dataclasses import dataclass, field
from pathlib import Path
from typing import Optional

# ---------------------------------------------------------------------------
# Regex patterns
# ---------------------------------------------------------------------------

EXIT_CODE_RX = re.compile(
    r"(?:exit(?:ed)?\s+with\s+(?:exit\s+)?code|"
    r"completed\s+with\s+exit\s+code|"
    r"Process\s+completed\s+with\s+exit\s+code|"
    r"with\s+exit\s+code|"
    r"Exit\s+code:)\s*(-?\d+)",
    re.IGNORECASE,
)

MSBUILD_ERROR_SUMMARY = re.compile(r"^\s*([1-9][0-9]*)\s+Error\(s\)", re.IGNORECASE)
MSBUILD_WARN_SUMMARY = re.compile(r"^\s*([1-9][0-9]*)\s+Warning\(s\)", re.IGNORECASE)
# The error code is optional: custom build tasks (e.g. FwBuildTasks' DownloadFile)
# log via Log.LogError without a code, which MSBuild renders as "error : message".
MSBUILD_DIAGNOSTIC_RX = re.compile(
    r"^\s*(?:(?P<node>\d+)>)?\s*(?P<source>.+?)\s*:\s*"
    r"(?P<level>error|warning)(?:\s+(?P<code>[A-Z]{2,}[0-9]{3,5}))?\s*:\s*(?P<message>.*)$",
    re.IGNORECASE,
)

MSBUILD_PROJECT_START = re.compile(
    r"^\s*\d+>Project\s+\"([^\"]+)\".*\son\s+node\s+\d+\s+\(([^)]*(?:\([^)]*\)[^)]*)*)\)[.,]?\s*$",
    re.IGNORECASE,
)
# "build[a-z]*" covers compound targets such as BuildInstaller, BuildBaseInstaller,
# and BuildPatch that a bare "build\b" would miss (no word boundary mid-word).
_REAL_TARGETS = re.compile(
    r"^\s*(?:default|build[a-z]*|rebuild|publish|pack|restore|clean|test"
    r"|install|deploy|custombuild|customactions)\b",
    re.IGNORECASE,
)
MSBUILD_PROJECT_DONE = re.compile(
    r"^\s*(?:\d+>)?Done\s+Building\s+Project\s+\"([^\"]+)\".*?(--\s*FAILED\.?)?$",
    re.IGNORECASE,
)


def detect_text_encoding(path: Path) -> str:
    """Detect a UTF BOM and return a safe text encoding."""
    with open(path, "rb") as fh:
        prefix = fh.read(4)
    if prefix.startswith(b"\xff\xfe") or prefix.startswith(b"\xfe\xff"):
        return "utf-16"
    if prefix.startswith(b"\xef\xbb\xbf"):
        return "utf-8-sig"
    return "utf-8"


# ---------------------------------------------------------------------------
# Data structures
# ---------------------------------------------------------------------------

@dataclass
class ErrorRecord:
    """An individual error with surrounding context lines."""
    line_num: int
    message: str
    context_before: list
    context_after: list
    _after_remaining: int = field(default=0, repr=False)


@dataclass
class StepRecord:
    """A build step (MSBuild project) with its errors, warnings, and exit codes."""
    name: str
    start_line: int
    errors: list = field(default_factory=list)
    warnings: list = field(default_factory=list)
    exit_codes: list = field(default_factory=list)
    notable: list = field(default_factory=list)
    end_line: Optional[int] = None
    # Set when MSBuild prints 'Done Building Project "..." -- FAILED.'
    explicitly_failed: bool = False

    @property
    def failed(self) -> bool:
        return bool(self.errors) or any(c != 0 for _, c in self.exit_codes) or self.explicitly_failed

    @property
    def first_failure_line(self) -> Optional[int]:
        candidates = []
        if self.errors:
            candidates.append(self.errors[0].line_num)
        bad_exits = [ln for ln, c in self.exit_codes if c != 0]
        if bad_exits:
            candidates.append(bad_exits[0])
        if candidates:
            return min(candidates)
        if self.explicitly_failed:
            return self.end_line or self.start_line
        return None

    @property
    def exit_code_summary(self) -> str:
        if not self.exit_codes:
            return ""
        return "exit codes: " + ", ".join(str(c) for _, c in self.exit_codes)


# ---------------------------------------------------------------------------
# Core log parser (single streaming pass)
# ---------------------------------------------------------------------------

# Lines captured after "Build FAILED." for the unattributed-failure safety net.
_BUILD_FAILED_CAPTURE_LINES = 100


def parse_log(log_path: Path, context_before: int = 30, context_after: int = 10) -> tuple[list[StepRecord], int, Optional[list]]:
    """Stream the log file once and return (steps, total_lines, build_failed_block).

    build_failed_block is the MSBuild failure summary following the first
    "Build FAILED." line (or None if the log never reports a failed build).
    It guarantees a failure is surfaced even when no step can be attributed.
    """
    steps: list[StepRecord] = []
    node_stack: dict[int, list[StepRecord]] = {}
    node_rolling: dict[int, deque] = {}
    pending_after: list[ErrorRecord] = []
    found_project_boundary = False
    build_failed_block: list = []
    build_failed_remaining = 0
    _DEFAULT_NODE = 0

    def get_rolling(node: int) -> deque:
        if node not in node_rolling:
            node_rolling[node] = deque(maxlen=context_before)
        return node_rolling[node]

    def push_step(node: int, step: StepRecord) -> None:
        node_stack.setdefault(node, []).append(step)

    def pop_step(node: int, lnum: int, explicitly_failed: bool = False) -> None:
        stack = node_stack.get(node)
        if stack:
            step = stack.pop()
            if step.end_line is None:
                step.end_line = lnum
            if explicitly_failed:
                step.explicitly_failed = True
            if not stack:
                node_stack.pop(node, None)

    def top_step(node: int) -> Optional[StepRecord]:
        stack = node_stack.get(node)
        return stack[-1] if stack else None

    def sole_open_step() -> Optional[StepRecord]:
        """The single open step across all nodes, if unambiguous.

        Diagnostics logged without a node prefix land on the default node;
        when exactly one step is open anywhere, attribute them to it.
        """
        open_steps = [stack[-1] for stack in node_stack.values() if stack]
        return open_steps[0] if len(open_steps) == 1 else None

    log_encoding = detect_text_encoding(log_path)
    total_lines = 0

    with open(log_path, "r", encoding=log_encoding, errors="replace") as fh:
        for raw in fh:
            total_lines += 1
            lnum = total_lines
            content = raw.rstrip("\n\r")

            if pending_after:
                still_pending = []
                for err_rec in pending_after:
                    err_rec.context_after.append((lnum, content))
                    err_rec._after_remaining -= 1
                    if err_rec._after_remaining > 0:
                        still_pending.append(err_rec)
                pending_after = still_pending

            if build_failed_remaining > 0:
                build_failed_block.append((lnum, content))
                build_failed_remaining -= 1
                if "Time Elapsed" in content:
                    build_failed_remaining = 0
            elif not build_failed_block and content.strip() == "Build FAILED.":
                build_failed_block.append((lnum, content.strip()))
                build_failed_remaining = _BUILD_FAILED_CAPTURE_LINES

            _node_m = re.match(r'^\s*(\d+)>', content)
            node = int(_node_m.group(1)) if _node_m else _DEFAULT_NODE
            rolling = get_rolling(node)

            start_match = MSBUILD_PROJECT_START.match(content)
            if start_match:
                targets = start_match.group(2).strip()
                if not _REAL_TARGETS.match(targets):
                    rolling.append((lnum, content))
                    continue
                found_project_boundary = True
                proj_name = Path(start_match.group(1)).name
                step = StepRecord(name=f"{proj_name} ({targets})", start_line=lnum)
                push_step(node, step)
                steps.append(step)
                rolling.clear()
                rolling.append((lnum, content))
                continue

            done_match = MSBUILD_PROJECT_DONE.match(content)
            if done_match:
                rolling.append((lnum, content))
                done_targets = re.search(r'\(([^)]*(?:\([^)]*\)[^)]*)*)\)', content)
                if not done_targets or _REAL_TARGETS.match(done_targets.group(1).strip()):
                    pop_step(node, lnum, explicitly_failed=content.rstrip().endswith("FAILED."))
                continue

            current = top_step(node)
            if current is None and node == _DEFAULT_NODE:
                current = sole_open_step()
            if current is None:
                rolling.append((lnum, content))
                continue

            content_lower = content.lower()

            if ": error" in content_lower or ": warning" in content_lower:
                diag_match = MSBUILD_DIAGNOSTIC_RX.match(content)
                if diag_match:
                    level = diag_match.group("level").lower()
                    msg = content.strip()
                    if level == "error":
                        err = ErrorRecord(
                            line_num=lnum, message=msg,
                            context_before=list(rolling), context_after=[],
                            _after_remaining=context_after,
                        )
                        current.errors.append(err)
                        pending_after.append(err)
                    else:
                        current.warnings.append((lnum, msg))

            if "exit" in content_lower:
                ec_match = EXIT_CODE_RX.search(content)
                if ec_match:
                    # Informational only in boundary mode: tools such as Exec with
                    # IgnoreExitCode log non-zero codes on healthy builds. Real
                    # failures surface as errors or a "-- FAILED." project marker.
                    # The synthetic fallback below still fails on exit codes, its
                    # only signal.
                    current.notable.append((lnum, content.strip()))

            if "error(s)" in content_lower:
                if MSBUILD_ERROR_SUMMARY.search(content):
                    current.notable.append((lnum, f"MSBuild: {content.strip()}"))
            if "warning(s)" in content_lower:
                if MSBUILD_WARN_SUMMARY.search(content):
                    current.notable.append((lnum, f"MSBuild: {content.strip()}"))

            rolling.append((lnum, content))

    if not found_project_boundary:
        if not steps:
            synthetic = StepRecord(name=log_path.name, start_line=1, end_line=total_lines)
            steps.append(synthetic)
            rolling2: deque = deque(maxlen=context_before)
            pending_after2: list[ErrorRecord] = []
            with open(log_path, "r", encoding=detect_text_encoding(log_path), errors="replace") as fh2:
                for lnum2, raw2 in enumerate(fh2, 1):
                    c2 = raw2.rstrip("\n\r")
                    if pending_after2:
                        still = []
                        for er in pending_after2:
                            er.context_after.append((lnum2, c2))
                            er._after_remaining -= 1
                            if er._after_remaining > 0:
                                still.append(er)
                        pending_after2 = still
                    cl = c2.lower()
                    if ": error" in cl or ": warning" in cl:
                        dm = MSBUILD_DIAGNOSTIC_RX.match(c2)
                        if dm:
                            lvl = dm.group("level").lower()
                            if lvl == "error":
                                er = ErrorRecord(
                                    line_num=lnum2, message=c2.strip(),
                                    context_before=list(rolling2), context_after=[],
                                    _after_remaining=context_after,
                                )
                                synthetic.errors.append(er)
                                pending_after2.append(er)
                            else:
                                synthetic.warnings.append((lnum2, c2.strip()))
                    if "exit" in cl:
                        ecm = EXIT_CODE_RX.search(c2)
                        if ecm:
                            synthetic.exit_codes.append((lnum2, int(ecm.group(1))))
                    if "error(s)" in cl and MSBUILD_ERROR_SUMMARY.search(c2):
                        synthetic.notable.append((lnum2, f"MSBuild: {c2.strip()}"))
                    if "warning(s)" in cl and MSBUILD_WARN_SUMMARY.search(c2):
                        synthetic.notable.append((lnum2, f"MSBuild: {c2.strip()}"))
                    rolling2.append((lnum2, c2))
    else:
        for stack in node_stack.values():
            for step in stack:
                if step.end_line is None:
                    step.end_line = total_lines

    merged: list[StepRecord] = []
    by_name: dict[str, StepRecord] = {}
    for step in steps:
        if step.name in by_name:
            canonical = by_name[step.name]
            canonical.errors.extend(step.errors)
            canonical.warnings.extend(step.warnings)
            canonical.exit_codes.extend(step.exit_codes)
            canonical.notable.extend(step.notable)
            if step.end_line and (canonical.end_line is None or step.end_line > canonical.end_line):
                canonical.end_line = step.end_line
        else:
            by_name[step.name] = step
            merged.append(step)

    return merged, total_lines, (build_failed_block or None)


# ---------------------------------------------------------------------------
# GitHub Actions output
# ---------------------------------------------------------------------------

_MAX_ERRORS_DETAIL = 10
_MAX_ANNOTATIONS = 10


def _strip_workspace(source: str, workspace: Optional[Path]) -> str:
    """Strip the runner workspace prefix to produce a repo-relative path."""
    if not workspace:
        return source
    ws = str(workspace).replace("\\", "/").rstrip("/") + "/"
    normed = source.replace("\\", "/")
    if normed.startswith(ws):
        return normed[len(ws):]
    return source


def _build_enrichment_index(findings: list) -> dict[int, list]:
    """Index enrichment findings by error_line for inline display."""
    idx: dict[int, list] = {}
    for f in findings:
        idx.setdefault(f.error_line, []).append(f)
    return idx


def write_report_markdown(steps: list[StepRecord], total_lines: int, log_path: Path,
                          enrichment_findings: list, workspace: Optional[Path] = None,
                          build_failed: Optional[list] = None) -> str:
    """Produce a GitHub-flavored markdown report."""
    lines = []
    w = lines.append
    failed_steps = [s for s in steps if s.failed]
    enrichment_idx = _build_enrichment_index(enrichment_findings or [])
    matched_lines = set()

    w("## MSBuild Log Analysis")
    size_mb = log_path.stat().st_size / 1_048_576
    w(f"**Log:** `{log_path.name}` ({size_mb:.1f} MB) | "
      f"**Lines:** {total_lines:,} | "
      f"**Steps:** {len(steps)} total, {len(failed_steps)} failed")
    w("")

    if not failed_steps:
        if build_failed:
            w("> :warning: MSBuild reported **Build FAILED.**, but the failure could not be "
              "attributed to any step. Raw MSBuild failure summary:")
            w("")
            w("```")
            for ln, text in build_failed[:60]:
                w(f"{ln:>8}: {text}")
            w("```")
            w("")
            w("If this error shape recurs, teach `scripts/tools/analyze_build_log.py` to recognize it.")
        else:
            w("> No failed steps detected.")
        return "\n".join(lines)

    # Step summary table
    w("### Step Summary")
    w("| # | Status | Step | Errors | Warnings |")
    w("|---|--------|------|-------:|----------:|")
    for i, s in enumerate(steps, 1):
        status = ":x: FAIL" if s.failed else ":white_check_mark:"
        w(f"| {i} | {status} | {s.name} | {len(s.errors)} | {len(s.warnings)} |")
    w("")

    # Root cause
    root = failed_steps[0]
    w("### Root Cause")
    w(f"**First failed step:** [{steps.index(root) + 1}] `{root.name}` at line {root.first_failure_line:,}")
    if len(failed_steps) > 1:
        w(f"\n{len(failed_steps) - 1} subsequent step(s) also failed (likely cascading).")
    w("")

    # Error details
    errors_shown = 0
    total_errors = sum(len(s.errors) for s in failed_steps)
    for step in failed_steps:
        if errors_shown >= _MAX_ERRORS_DETAIL:
            break
        if not step.errors and not any(c != 0 for _, c in step.exit_codes):
            continue
        w(f"### [{steps.index(step) + 1}] {step.name}")
        if step == root:
            w("*Root cause*")
        w("")

        for err in step.errors:
            if errors_shown >= _MAX_ERRORS_DETAIL:
                break
            w(f"**Line {err.line_num:,}:** `{err.message[:200]}`")

            # Inline enrichment
            if err.line_num in enrichment_idx:
                for finding in enrichment_idx[err.line_num]:
                    matched_lines.add(err.line_num)
                    w(f"\n> :mag: **{finding.error_code}:** {finding.summary}")
                    if finding.detail:
                        for d in finding.detail:
                            w(f"> {d}")
                w("")

            if err.context_before or err.context_after:
                w("<details><summary>Context</summary>\n")
                w("```")
                for ln, text in err.context_before[-10:]:
                    w(f"{ln:>8}: {text}")
                w(f"{'>' * 8}: {err.message}")
                for ln, text in err.context_after:
                    w(f"{ln:>8}: {text}")
                w("```\n</details>\n")
            errors_shown += 1

        bad_exits = [(ln, c) for ln, c in step.exit_codes if c != 0]
        if bad_exits and not step.errors:
            w("**Non-zero exit codes:**")
            for ln, c in bad_exits:
                w(f"- Line {ln:,}: exit code {c}")
            w("")

    if errors_shown < total_errors:
        w(f"\n> ... and {total_errors - errors_shown} more error(s). See build log for details.\n")

    # Unmatched enrichment findings
    unmatched = [f for f in (enrichment_findings or []) if f.error_line not in matched_lines]
    if unmatched:
        w("### Additional Findings")
        for finding in unmatched:
            w(f"- **{finding.error_code}** (line {finding.error_line:,}): {finding.summary}")
            for d in finding.detail:
                w(f"  {d}")
        w("")

    return "\n".join(lines)


def emit_github_annotations(steps: list[StepRecord], enrichment_findings: list, workspace: Optional[Path] = None,
                            build_failed: Optional[list] = None) -> None:
    """Emit ::error:: workflow commands for GitHub annotation badges."""

    def _esc_msg(s: str) -> str:
        return s.replace("%", "%25").replace("\r", "%0D").replace("\n", "%0A")

    def _esc_prop(s: str) -> str:
        return _esc_msg(s).replace(":", "%3A").replace(",", "%2C")

    loc_rx = re.compile(r"^(?P<file>.+?)(?:\((?P<line>\d+)(?:,(?P<col>\d+))?\))?$")

    if build_failed and not any(s.failed for s in steps):
        first_err = next((text for _, text in build_failed if ": error" in text.lower()),
                         build_failed[0][1])
        print(f"::error::Build FAILED (unattributed): {_esc_msg(first_err.strip()[:200])}")

    count = 0
    for step in steps:
        if not step.failed:
            continue
        for err in step.errors:
            if count >= _MAX_ANNOTATIONS:
                return
            diag = MSBUILD_DIAGNOSTIC_RX.match(err.message)
            if diag:
                raw_loc = diag.group("source")
                lm = loc_rx.match(raw_loc)
                file_path = _strip_workspace((lm.group("file") if lm else raw_loc), workspace)
                props = f"file={_esc_prop(file_path)}"
                if lm and lm.group("line"):
                    props += f",line={lm.group('line')}"
                if lm and lm.group("col"):
                    props += f",col={lm.group('col')}"
                code = diag.group("code")
                text = diag.group("message")
                msg = _esc_msg(f"{code}: {text}" if code else text)
                print(f"::error {props}::{msg}")
            else:
                print(f"::error::{_esc_msg(err.message[:200])}")
            count += 1

    for finding in (enrichment_findings or []):
        if count >= _MAX_ANNOTATIONS:
            return
        print(f"::error ::{finding.error_code}: {finding.summary}")
        count += 1


def write_github_step_summary(markdown: str) -> None:
    """Append markdown to $GITHUB_STEP_SUMMARY."""
    summary_path = os.environ.get("GITHUB_STEP_SUMMARY")
    if summary_path:
        with open(summary_path, "a", encoding="utf-8") as f:
            f.write(markdown + "\n")
    else:
        print(markdown, file=sys.stderr)


# ---------------------------------------------------------------------------
# CLI entry point
# ---------------------------------------------------------------------------

def main():
    parser = argparse.ArgumentParser(description="Analyze an MSBuild log for root-cause build failures.")
    parser.add_argument("log", help="Path to the MSBuild log file.")
    parser.add_argument("--context", "-c", type=int, default=30, help="Context lines before each error (default: 30).")
    parser.add_argument("--context-after", "-ca", type=int, default=10, help="Context lines after each error (default: 10).")
    parser.add_argument("--workspace", "-w", default=None, help="Repo workspace root for on-disk file lookups (default: $GITHUB_WORKSPACE or cwd).")
    args = parser.parse_args()

    log_path = Path(args.log)
    if not log_path.exists():
        sys.exit(f"ERROR: Log file not found: {log_path}")

    workspace = Path(args.workspace) if args.workspace else Path(os.environ.get("GITHUB_WORKSPACE", Path.cwd()))

    print(f"Analyzing {log_path.name} ({log_path.stat().st_size / 1_048_576:.1f} MB) ...")
    steps, total_lines, build_failed = parse_log(log_path, args.context, args.context_after)
    print(f"{total_lines:,} lines parsed, {len(steps)} steps found.")

    failed = [s for s in steps if s.failed]
    enrichment_findings = run_enrichments(steps, log_path, workspace_root=workspace)

    emit_github_annotations(steps, enrichment_findings, workspace, build_failed)
    markdown = write_report_markdown(steps, total_lines, log_path, enrichment_findings, workspace, build_failed)
    write_github_step_summary(markdown)

    error_count = sum(len(s.errors) for s in failed)
    print(f"Build analysis: {error_count} error(s) in {len(failed)} failed step(s).")

    if failed or build_failed:
        sys.exit(1)


if __name__ == "__main__":
    main()
