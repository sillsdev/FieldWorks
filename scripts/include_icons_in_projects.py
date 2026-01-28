#!/usr/bin/env python3
"""
Ensure that icon files located alongside a C# project are referenced by that project.

For each *.csproj discovered under the target root this script searches for *.ico files
inside the project directory (recursively, excluding generated folders such as bin/ and
obj/). Any icons that are not already listed in the project file will be added as
EmbeddedResource items.

Usage:
    python tools/include_icons_in_projects.py [--root <path>] [--dry-run]

The script only modifies projects when new icon references need to be added.
"""
from __future__ import annotations

import argparse
import sys
from pathlib import Path
import xml.etree.ElementTree as ET

_SKIPPED_DIRS = {"bin", "obj", "out", "output", ".git"}
_ICON_EXT = ".ico"
_ITEM_ELEMENT = "EmbeddedResource"


def _normalize_include(value: str) -> str:
    return value.replace("/", "\\").lower()


def _icon_paths(project_path: Path) -> list[Path]:
    project_dir = project_path.parent
    icons: list[Path] = []
    for candidate in project_dir.rglob(f"*{_ICON_EXT}"):
        if any(part.lower() in _SKIPPED_DIRS for part in candidate.parts):
            continue
        # Skip artifacts living outside the project folder (e.g. Output/Debug/...)
        try:
            candidate.relative_to(project_dir)
        except ValueError:
            continue
        icons.append(candidate)
    return sorted(icons)


def _existing_includes(root: ET.Element) -> set[str]:
    includes: set[str] = set()
    for item_group in root.findall("ItemGroup"):
        for child in item_group:
            include_value = child.get("Include")
            if not include_value:
                continue
            if include_value.lower().endswith(_ICON_EXT):
                includes.add(_normalize_include(include_value))
    return includes


def _ensure_icons(project_path: Path, dry_run: bool) -> bool:
    try:
        tree = ET.parse(project_path)
    except ET.ParseError as exc:  # pragma: no cover - defensive guard
        print(f"Skipping {project_path}: XML parse error: {exc}", file=sys.stderr)
        return False

    root = tree.getroot()
    project_dir = project_path.parent

    icons = _icon_paths(project_path)
    if not icons:
        return False

    existing = _existing_includes(root)

    missing_relative: list[str] = []
    for icon in icons:
        rel_path = icon.relative_to(project_dir)
        include_value = str(rel_path).replace("/", "\\")
        if _normalize_include(include_value) not in existing:
            missing_relative.append(include_value)

    if not missing_relative:
        return False

    if dry_run:
        print(f"would update: {project_path}")
        for rel in missing_relative:
            print(f'  add {_ITEM_ELEMENT} Include="{rel}"')
        return True

    item_group = ET.SubElement(root, "ItemGroup")
    for rel in missing_relative:
        element = ET.SubElement(item_group, _ITEM_ELEMENT)
        element.set("Include", rel)

    try:
        ET.indent(tree, space="  ")
    except AttributeError:  # pragma: no cover - fallback for older Python
        pass

    tree.write(project_path, encoding="utf-8", xml_declaration=True)
    print(f"updated: {project_path}")
    return True


def _iter_projects(root: Path) -> list[Path]:
    return sorted(root.rglob("*.csproj"))


def main() -> int:
    parser = argparse.ArgumentParser(
        description="Add missing icon references to C# project files"
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
        help="Show the changes that would be made without writing files",
    )
    args = parser.parse_args()

    if not args.root.exists():
        print(f"Root path {args.root} does not exist", file=sys.stderr)
        return 1

    projects = _iter_projects(args.root)
    if not projects:
        print("No .csproj files found", file=sys.stderr)
        return 1

    updates = 0
    for project in projects:
        if _ensure_icons(project, args.dry_run):
            updates += 1

    if updates == 0:
        print("No icon updates were required.")
    else:
        suffix = " (dry run)" if args.dry_run else ""
        print(f"Completed: {updates} project(s) processed{suffix}.")

    return 0


if __name__ == "__main__":
    raise SystemExit(main())
