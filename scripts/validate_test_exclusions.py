from __future__ import annotations

import argparse
import json
import sys
from pathlib import Path

from scripts.test_exclusions.validator import Validator


def parse_args(argv: list[str] | None = None) -> argparse.Namespace:
    parser = argparse.ArgumentParser(
        description="Validate test exclusion patterns across the repository.",
    )
    parser.add_argument(
        "--fail-on-warning",
        action="store_true",
        help="Exit with error code if warnings are detected (default: fail only on errors).",
    )
    parser.add_argument(
        "--json-report",
        type=Path,
        help="Path to write the JSON validation report.",
    )
    parser.add_argument(
        "--analyze-log",
        type=Path,
        help="Path to an MSBuild log file to check for CS0436 warnings.",
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

    validator = Validator(repo_root)
    print(f"Validating repository at {repo_root}...")
    summary = validator.validate_repo()

    print(f"Scanned {summary.total_projects} projects.")
    print(f"Passed: {summary.passed_projects}")
    print(f"Failed: {summary.failed_projects}")
    print(f"Errors: {summary.error_count}")
    print(f"Warnings: {summary.warning_count}")

    if summary.issues:
        print("\nIssues:")
        for issue in summary.issues:
            print(f"[{issue.severity.value}] {issue.project_name}: {issue.details}")

    if args.json_report:
        report = {
            "totalProjects": summary.total_projects,
            "passedProjects": summary.passed_projects,
            "failedProjects": summary.failed_projects,
            "errorCount": summary.error_count,
            "warningCount": summary.warning_count,
            "issues": [i.to_dict() for i in summary.issues],
        }
        args.json_report.parent.mkdir(parents=True, exist_ok=True)
        args.json_report.write_text(json.dumps(report, indent=2), encoding="utf-8")
        print(f"\nReport written to {args.json_report}")

    exit_code = 0
    if summary.error_count > 0:
        exit_code = 1

    if args.fail_on_warning and summary.warning_count > 0:
        exit_code = 1

    if args.analyze_log:
        if not args.analyze_log.exists():
            print(f"Log file not found: {args.analyze_log}")
            exit_code = 1
        else:
            print(f"\nAnalyzing build log: {args.analyze_log}")
            cs0436_count = 0
            with args.analyze_log.open(encoding="utf-8", errors="replace") as fp:
                for line in fp:
                    if "CS0436" in line:
                        print(f"[CS0436] {line.strip()}")
                        cs0436_count += 1

            if cs0436_count > 0:
                print(f"Found {cs0436_count} CS0436 warnings.")
                exit_code = 1
            else:
                print("No CS0436 warnings found.")

    return exit_code


if __name__ == "__main__":
    raise SystemExit(main(sys.argv[1:]))
