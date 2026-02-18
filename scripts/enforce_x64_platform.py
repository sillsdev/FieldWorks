#!/usr/bin/env python3
"""
Force managed project files to target x64 builds only.

The script walks all *.csproj files below the requested root and ensures that
PlatformTarget is set to x64, Prefer32Bit is disabled, and configuration-specific
property groups no longer reference AnyCPU/x86/Win32 platforms. Duplicate
PropertyGroup blocks that only applied to the removed platforms are deleted.

Usage:
    python tools/enforce_x64_platform.py [--root <path>] [--dry-run] [--force]

Run with --dry-run to preview the files that would change. Pass --force to
rewrite matching project files even when no structural edits are detected
(useful for normalizing formatting).
"""
from __future__ import annotations

import argparse
import re
import sys
from pathlib import Path
import xml.etree.ElementTree as ET

# Platforms that should be migrated to x64.
_LEGACY_PLATFORMS = {"anycpu", "any cpu", "x86", "win32"}


def _iter_property_groups(root: ET.Element) -> list[ET.Element]:
    return list(root.findall("PropertyGroup"))


def _extract_config_platform(condition: str) -> tuple[str, str, str] | None:
    """Return (segment, config_raw, platform_raw) if condition matches config|platform."""
    normalized = condition.replace('"', "'")
    for segment in normalized.split("'"):
        if "|" not in segment:
            continue
        if "$(" in segment:  # Skip the left-hand side '$(Configuration)|$(Platform)'
            continue
        config_raw, platform_raw = segment.split("|", 1)
        return segment, config_raw, platform_raw
    return None


def _extract_platform_only(condition: str) -> str | None:
    if "$(Platform)" not in condition:
        return None
    normalized = condition.replace('"', "'")
    for segment in normalized.split("'"):
        if not segment or "$(" in segment or "|" in segment:
            continue
        return segment
    return None


def _replace_platform_tokens(text: str) -> tuple[str, bool]:
    replacements = [
        (re.compile(r"(?i)any\s*cpu"), "x64"),
        (re.compile(r"(?i)\bx86\b"), "x64"),
        (re.compile(r"(?i)\bwin32\b"), "x64"),
    ]
    updated = text
    changed = False
    for pattern, repl in replacements:
        new_value, count = pattern.subn(repl, updated)
        if count:
            updated = new_value
            changed = True
    return updated, changed


def _ensure_element_text(parent: ET.Element, tag: str, value: str) -> bool:
    element = parent.find(tag)
    if element is None:
        element = ET.SubElement(parent, tag)
        element.text = value
        return True
    current = (element.text or "").strip()
    if current != value:
        element.text = value
        return True
    return False


def _get_or_create_unconditional_group(root: ET.Element) -> ET.Element:
    for group in root.findall("PropertyGroup"):
        if group.get("Condition") is None:
            return group
    group = ET.Element("PropertyGroup")
    root.insert(0, group)
    return group


def _process_csproj(path: Path, dry_run: bool, force: bool) -> bool:
    try:
        tree = ET.parse(path)
    except ET.ParseError as exc:  # pragma: no cover
        print(f"Skipping {path}: XML parse error: {exc}", file=sys.stderr)
        return False

    root = tree.getroot()
    property_groups = _iter_property_groups(root)

    # Track configurations that already have explicit x64 blocks.
    configs_with_x64: set[str] = set()
    config_entries: list[tuple[ET.Element, str, str, str]] = []

    for group in property_groups:
        condition = group.get("Condition")
        if not condition:
            continue
        parsed = _extract_config_platform(condition)
        if parsed:
            segment, config_raw, platform_raw = parsed
            config_name = config_raw.strip().lower()
            platform_name = platform_raw.strip().lower()
            config_entries.append((group, condition, segment, platform_raw))
            if platform_name == "x64":
                configs_with_x64.add(config_name)

    changed = False
    groups_to_remove: list[ET.Element] = []

    for group, condition, segment, platform_raw in config_entries:
        platform_key = platform_raw.strip().lower()
        if platform_key in _LEGACY_PLATFORMS:
            parsed = _extract_config_platform(condition)
            if not parsed:
                continue
            segment, config_raw, platform_raw = parsed
            config_key = config_raw.strip().lower()
            if config_key in configs_with_x64:
                groups_to_remove.append(group)
                changed = True
                continue
            new_segment = f"{config_raw}|x64"
            new_condition = condition.replace(segment, new_segment, 1)
            if new_condition != condition:
                group.set("Condition", new_condition)
                changed = True
            configs_with_x64.add(config_key)

    if groups_to_remove:
        for group in groups_to_remove:
            root.remove(group)
        property_groups = _iter_property_groups(root)

    # Update remaining Condition attributes that mention legacy platforms explicitly.
    for group in property_groups:
        condition = group.get("Condition")
        if not condition:
            continue
        updated_condition, cond_changed = _replace_platform_tokens(condition)
        if cond_changed:
            group.set("Condition", updated_condition)
            changed = True

    # Force all PlatformTarget elements to x64.
    for target in root.findall(".//PlatformTarget"):
        if (target.text or "").strip().lower() != "x64":
            target.text = "x64"
            changed = True

    # Ensure the main PropertyGroup has PlatformTarget and Prefer32Bit settings.
    base_group = _get_or_create_unconditional_group(root)
    if _ensure_element_text(base_group, "PlatformTarget", "x64"):
        changed = True
    if _ensure_element_text(base_group, "Prefer32Bit", "false"):
        changed = True

    if not changed and not force:
        return False

    if dry_run:
        return True

    try:  # pragma: no cover
        ET.indent(tree, space="  ")
    except AttributeError:
        pass

    tree.write(path, encoding="utf-8", xml_declaration=True)
    content = path.read_text(encoding="utf-8")
    if content.startswith("<?xml version='1.0' encoding='utf-8'?>"):
        content = content.replace(
            "<?xml version='1.0' encoding='utf-8'?>",
            '<?xml version="1.0" encoding="utf-8"?>',
            1,
        )
    if not content.endswith("\n"):
        content += "\n"
    path.write_text(content, encoding="utf-8")
    return True


def _iter_projects(root: Path) -> list[Path]:
    return sorted(root.rglob("*.csproj"))


def main() -> int:
    parser = argparse.ArgumentParser(
        description="Ensure all managed projects build for x64"
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
    parser.add_argument(
        "--force",
        action="store_true",
        help="Re-write matching files even if no structural changes are detected",
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
        if _process_csproj(project, args.dry_run, args.force):
            status = "would update" if args.dry_run else "updated"
            print(f"{status}: {project}")
            updates += 1

    if updates == 0:
        print("All projects already target x64.")
    else:
        suffix = " (dry run)" if args.dry_run else ""
        print(f"Completed: {updates} project(s) modified{suffix}.")

    return 0


if __name__ == "__main__":
    raise SystemExit(main())
