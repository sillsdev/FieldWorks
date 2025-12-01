#!/usr/bin/env python3
"""
Remove per-platform PropertyGroup elements that target the x86 platform
from C# project files in the FieldWorks repository.

Usage:
    python tools/remove_x86_property_groups.py [--root <path>] [--dry-run]

By default the script walks the repository root and processes every *.csproj
below it.  Use --root to limit the scan.  Pass --dry-run to see the files
that would be modified without writing the changes.
"""
from __future__ import annotations

import argparse
import sys
from pathlib import Path
import xml.etree.ElementTree as ET

# PropertyGroup conditions that should be removed when they mention these tokens.
_PLATFORM_TOKENS = {"|x86", "|win32"}


def _should_remove(condition: str | None) -> bool:
    if not condition:
        return False
    normalized = condition.lower().replace(" ", "")
    return any(token in normalized for token in _PLATFORM_TOKENS)


def _process_project(path: Path, dry_run: bool) -> bool:
    try:
        tree = ET.parse(path)
    except ET.ParseError as exc:  # pragma: no cover - defensive guard
        print(f"Skipping {path}: XML parse error: {exc}", file=sys.stderr)
        return False

    root = tree.getroot()
    removed = False

    for group in list(root.findall("PropertyGroup")):
        if _should_remove(group.get("Condition")):
            root.remove(group)
            removed = True

    if not removed:
        return False

    if dry_run:
        return True

    # Pretty-print the output to keep diffs readable (Python 3.9+).
    try:
        ET.indent(tree, space="  ")
    except AttributeError:  # pragma: no cover - fallback for older Python
        pass

    tree.write(path, encoding="utf-8", xml_declaration=True)
    return True


def _iter_projects(root: Path) -> list[Path]:
    return sorted(root.rglob("*.csproj"))


def main() -> int:
    parser = argparse.ArgumentParser(
        description="Remove x86 PropertyGroup entries from C# projects"
    )
    parser.add_argument(
        "--root",
        type=Path,
        default=Path.cwd(),
        help="Root directory to scan (default: current directory)",
    )
    parser.add_argument(
        "--dry-run",
        action="store_true",
        help="Show files that would be updated without writing changes",
    )
    args = parser.parse_args()

    if not args.root.exists():
        print(f"Root path {args.root} does not exist", file=sys.stderr)
        return 1

    projects = _iter_projects(args.root)
    if not projects:
        print("No .csproj files found", file=sys.stderr)
        return 1

    updated = 0
    for project in projects:
        if _process_project(project, args.dry_run):
            status = "would update" if args.dry_run else "updated"
            print(f"{status}: {project}")
            updated += 1

    if updated == 0:
        print("No x86-specific property groups found.")
    else:
        suffix = " (dry run)" if args.dry_run else ""
        print(f"Completed: {updated} project(s) modified{suffix}.")

    return 0


if __name__ == "__main__":
    raise SystemExit(main())
