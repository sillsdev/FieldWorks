from __future__ import annotations

import argparse
import sys
from pathlib import Path

from scripts.test_exclusions import repo_scanner
from scripts.test_exclusions.escalation_writer import EscalationWriter
from scripts.test_exclusions.report_writer import ReportWriter

_DEFAULT_OUTPUT = Path("Output/test-exclusions/report.json")
_DEFAULT_CSV = Path("Output/test-exclusions/report.csv")
_DEFAULT_ESCALATION_JSON = Path("Output/test-exclusions/mixed-code.json")
_DEFAULT_ESCALATION_DIR = Path("Output/test-exclusions/escalations")


def parse_args(argv: list[str] | None = None) -> argparse.Namespace:
    parser = argparse.ArgumentParser(
        description="Audit SDK-style projects and summarize their test exclusion patterns.",
    )
    parser.add_argument(
        "--output",
        type=Path,
        default=_DEFAULT_OUTPUT,
        help="Path to the JSON report (default: Output/test-exclusions/report.json)",
    )
    parser.add_argument(
        "--csv-output",
        type=Path,
        default=_DEFAULT_CSV,
        help="Path to the CSV report (default: Output/test-exclusions/report.csv)",
    )
    parser.add_argument(
        "--mixed-code-json",
        type=Path,
        default=_DEFAULT_ESCALATION_JSON,
        help="Path to the mixed-code escalation JSON (default: Output/test-exclusions/mixed-code.json)",
    )
    parser.add_argument(
        "--escalations-dir",
        type=Path,
        default=_DEFAULT_ESCALATION_DIR,
        help="Directory for Markdown issue templates (default: Output/test-exclusions/escalations)",
    )
    parser.add_argument(
        "--repo-root",
        type=Path,
        default=Path(__file__).resolve().parents[1],
        help=argparse.SUPPRESS,
    )
    return parser.parse_args(argv)


def main(argv: list[str] | None = None) -> int:
    args = parse_args(argv)
    repo_root = args.repo_root.resolve()
    results = repo_scanner.scan_repository(repo_root)
    writer = ReportWriter(repo_root)
    json_path = _resolve_path(repo_root, args.output)
    csv_path = _resolve_path(repo_root, args.csv_output) if args.csv_output else None
    writer.write_reports(results, json_path, csv_path)
    escalation_writer = EscalationWriter(repo_root)
    escalation_writer.write_outputs(
        results,
        _resolve_path(repo_root, args.mixed_code_json),
        _resolve_path(repo_root, args.escalations_dir),
    )
    print(f"Scanned {len(results)} projects. JSON → {json_path}")
    if csv_path:
        print(f"CSV → {csv_path}")
    print(f"Mixed-code escalations → {args.mixed_code_json} / {args.escalations_dir}")
    return 0


def _resolve_path(repo_root: Path, path: Path) -> Path:
    if path.is_absolute():
        return path
    return repo_root / path


if __name__ == "__main__":
    raise SystemExit(main(sys.argv[1:]))
