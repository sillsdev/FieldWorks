from __future__ import annotations

import argparse
import json
import sys
from pathlib import Path

from scripts.test_exclusions.converter import Converter
from scripts.test_exclusions.models import PatternType, Project, TestFolder

_DEFAULT_INPUT = Path("Output/test-exclusions/report.json")


def parse_args(argv: list[str] | None = None) -> argparse.Namespace:
    parser = argparse.ArgumentParser(
        description="Convert projects to Pattern A test exclusions.",
    )
    parser.add_argument(
        "--input",
        type=Path,
        default=_DEFAULT_INPUT,
        help="Path to the JSON report (default: Output/test-exclusions/report.json)",
    )
    parser.add_argument(
        "--batch-size",
        type=int,
        default=10,
        help="Number of projects to convert in this run (default: 10)",
    )
    parser.add_argument(
        "--dry-run",
        action="store_true",
        help="Print planned changes without modifying files.",
    )
    parser.add_argument(
        "--no-verify",
        action="store_true",
        help="Skip build verification (faster, but less safe).",
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

    if not args.input.exists():
        print(f"Input report not found: {args.input}")
        return 1

    with args.input.open(encoding="utf-8") as fp:
        report_data = json.load(fp)

    candidates = []
    for entry in report_data.get("projects", []):
        p_data = entry["project"]
        if p_data.get("hasMixedCode"):
            continue

        if p_data.get("patternType") == PatternType.PATTERN_A.value:
            continue

        project = Project(
            name=p_data["name"],
            relative_path=p_data["relativePath"],
            pattern_type=PatternType(p_data["patternType"]),
            has_mixed_code=p_data["hasMixedCode"],
        )

        test_folders = []
        for tf_data in entry.get("testFolders", []):
            test_folders.append(
                TestFolder(
                    project_name=tf_data["projectName"],
                    relative_path=tf_data["relativePath"],
                    depth=tf_data["depth"],
                    contains_source=tf_data["containsSource"],
                    excluded=tf_data["excluded"],
                )
            )

        candidates.append((project, test_folders))

    print(f"Found {len(candidates)} candidates for conversion.")

    batch = candidates[: args.batch_size]
    if not batch:
        print("No projects to convert.")
        return 0

    converter = Converter(repo_root)
    converted_count = 0

    print(f"Processing batch of {len(batch)} projects...")

    for project, folders in batch:
        try:
            changed = converter.convert_project(
                project, folders, dry_run=args.dry_run, verify=not args.no_verify
            )
            if changed:
                if not args.dry_run:
                    print(f"Converted: {project.name}")
                converted_count += 1
            else:
                print(f"Skipped (no changes needed): {project.name}")
        except Exception as e:
            print(f"Failed to convert {project.name}: {e}")

    print(f"Batch complete. Converted {converted_count} projects.")
    if args.dry_run:
        print("Dry run - no files modified.")

    return 0


if __name__ == "__main__":
    raise SystemExit(main(sys.argv[1:]))
