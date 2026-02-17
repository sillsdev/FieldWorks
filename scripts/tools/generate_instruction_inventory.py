#!/usr/bin/env python3
"""Generate an inventory of Copilot instruction files and COPILOT.md summaries."""

from __future__ import annotations

import argparse
import datetime as dt
import re
from pathlib import Path
from typing import Any, Dict, List, Optional

ROOT = Path(__file__).resolve().parents[2]
GITHUB_DIR = ROOT / ".github"
INSTRUCTIONS_DIR = GITHUB_DIR / "instructions"
SRC_DIR = ROOT / "Src"

FRONTMATTER_RE = re.compile(r"^---\s*$")


def parse_frontmatter(path: Path) -> Dict[str, Any]:
    """Parse simple YAML frontmatter block if present."""
    text = path.read_text(encoding="utf-8", errors="ignore").splitlines()
    if not text or not FRONTMATTER_RE.match(text[0]):
        return {}
    fm_lines: List[str] = []
    for line in text[1:]:
        if FRONTMATTER_RE.match(line):
            break
        fm_lines.append(line)
    data: Dict[str, Any] = {}
    for line in fm_lines:
        if not line.strip() or line.strip().startswith("#"):
            continue
        if ":" not in line:
            continue
        key, value = line.split(":", 1)
        data[key.strip()] = value.strip().strip('"')
    return data


def build_entry(
    path: Path, kind: str, apply_to: Optional[str] = None
) -> Dict[str, Any]:
    frontmatter = parse_frontmatter(path)
    rel_path = path.relative_to(ROOT).as_posix()
    entry: Dict[str, Any] = {
        "path": rel_path,
        "kind": kind,
        "size": path.stat().st_size,
        "lines": sum(1 for _ in path.open(encoding="utf-8", errors="ignore")),
        "applyTo": apply_to or frontmatter.get("applyTo"),
        "description": frontmatter.get("description"),
        "owners": frontmatter.get("owners"),
    }
    return entry


def collect_entries() -> List[Dict[str, Any]]:
    entries: List[Dict[str, Any]] = []

    # Repo-wide instructions
    repo_instruction = GITHUB_DIR / "copilot-instructions.md"
    if repo_instruction.exists():
        entries.append(build_entry(repo_instruction, "repo-wide"))

    # Path-specific instructions
    for instruction_file in sorted(INSTRUCTIONS_DIR.glob("*.instructions.md")):
        entries.append(build_entry(instruction_file, "path-specific"))

    # COPILOT summaries
    if SRC_DIR.exists():
        for copilot_file in sorted(SRC_DIR.rglob("COPILOT.md")):
            entries.append(build_entry(copilot_file, "folder-summary"))

    return entries


def dump_yaml(data: List[Dict[str, Any]]) -> str:
    lines: List[str] = []
    timestamp = dt.datetime.now(dt.timezone.utc).isoformat()
    lines.append(f"# Generated {timestamp}")
    for item in data:
        lines.append("- path: " + str(item["path"]))
        lines.append(f"  kind: {item['kind']}")
        lines.append(f"  size: {item['size']}")
        lines.append(f"  lines: {item['lines']}")
        if item.get("applyTo"):
            lines.append(f"  applyTo: \"{item['applyTo']}\"")
        if item.get("description"):
            lines.append(f"  description: \"{item['description']}\"")
        if item.get("owners"):
            lines.append(f"  owners: \"{item['owners']}\"")
    return "\n".join(lines) + "\n"


def main() -> None:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument(
        "--output",
        type=Path,
        default=INSTRUCTIONS_DIR / "inventory.yml",
        help="Path to output inventory YAML",
    )
    args = parser.parse_args()

    entries = collect_entries()
    yaml_text = dump_yaml(entries)
    args.output.parent.mkdir(parents=True, exist_ok=True)
    args.output.write_text(yaml_text, encoding="utf-8")
    print(f"Wrote {len(entries)} entries to {args.output}")


if __name__ == "__main__":
    main()
