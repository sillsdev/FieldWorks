"""Shared CLI argument helpers for GenerateAssemblyInfo scripts."""

from __future__ import annotations

import argparse
from pathlib import Path

DEFAULT_REPO_ROOT = Path(__file__).resolve().parents[2]


def build_common_parser(description: str) -> argparse.ArgumentParser:
    parser = argparse.ArgumentParser(description=description)
    parser.add_argument(
        "--repo-root",
        type=Path,
        default=DEFAULT_REPO_ROOT,
        help="Path to the FieldWorks repository root.",
    )
    parser.add_argument(
        "--branch",
        type=str,
        default=None,
        help="Optional branch identifier used for logging and restore lookups.",
    )
    parser.add_argument(
        "--release-ref",
        type=str,
        default="origin/release/9.3",
        help="Git ref used as the historical baseline when comparing AssemblyInfo files.",
    )
    parser.add_argument(
        "--output",
        type=Path,
        default=DEFAULT_REPO_ROOT / "Output" / "GenerateAssemblyInfo",
        help="Directory for generated artifacts (CSV/JSON/logs).",
    )
    parser.add_argument(
        "--restore-map",
        type=Path,
        default=None,
        help="Path to restore_map.json produced by the history diff helper.",
    )
    parser.add_argument(
        "--decisions",
        type=Path,
        default=None,
        help="Optional decisions CSV to drive the conversion script.",
    )
    parser.add_argument(
        "--report",
        type=Path,
        default=None,
        help="Optional validation report path (defaults to output/validation_report.txt).",
    )
    parser.add_argument(
        "--run-build",
        action="store_true",
        help="Run msbuild validation in addition to structural checks when supported.",
    )
    parser.add_argument(
        "--log-level",
        type=str,
        default="INFO",
        choices=["DEBUG", "INFO", "WARNING", "ERROR", "CRITICAL"],
        help="Verbosity for script logging output.",
    )
    parser.add_argument(
        "--skip-history",
        action="store_true",
        help="Skip git history lookups when assembling audit metadata.",
    )
    return parser


def resolve_output_path(args: argparse.Namespace, default_name: str) -> Path:
    if args.report is not None:
        return args.report
    return Path(args.output) / default_name
