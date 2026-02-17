#!/usr/bin/env python3
"""Generate short `*.instructions.md` files from large `AGENTS.md` summaries.

This script finds `AGENTS.md` files in `Src/` that exceed a size threshold and
writes concise path-specific instruction files to `.github/instructions/`.
"""
from __future__ import annotations
import argparse
from pathlib import Path
import re

ROOT = Path(__file__).resolve().parents[2]
SRC_DIR = ROOT / "Src"
INSTRUCTIONS_DIR = ROOT / ".github" / "instructions"
THRESHOLD_LINES = 200


def extract_summary(p: Path, lines: int = 40) -> str:
    text = p.read_text(encoding="utf-8", errors="ignore").splitlines()
    # Attempt to extract first meaningful sections (until '##' or 40 lines)
    head = "\n".join(text[:lines])
    # Remove code fences if present
    head = re.sub(r"```.*?```", "", head, flags=re.DOTALL)
    # Keep first two headings and up to 4 bullet rules if present
    return head.strip()


def to_filename(folderpath: Path) -> str:
    # convert 'Src/Common' -> 'common.instructions.md'
    name = folderpath.name.lower().replace(" ", "-")
    return f"{name}.instructions.md"


def main() -> int:
    INSTRUCTIONS_DIR.mkdir(parents=True, exist_ok=True)
    created = 0
    for p in sorted(SRC_DIR.rglob("AGENTS.md")):
        lines = p.read_text(encoding="utf-8", errors="ignore").splitlines()
        if len(lines) < THRESHOLD_LINES:
            continue
        rel_dir = p.parent
        out_name = to_filename(rel_dir)
        dest = INSTRUCTIONS_DIR / out_name
        summary = extract_summary(p)
        apply_to = f"Src/{rel_dir.relative_to(SRC_DIR).as_posix()}/**"
        header = (
            '---\napplyTo: "'
            + apply_to
            + '"\nname: "'
            + rel_dir.name.lower()
            + '.instructions"\n'
        )
        header += (
            'description: "Auto-generated concise instructions from AGENTS.md for '
            + rel_dir.name
            + '"\n---\n\n'
        )
        content = (
            header
            + "# "
            + rel_dir.name
            + " (Concise)\n\n"
            + "## Purpose & Scope\n"
            + "Summarized key points from AGENTS.md\n\n"
            + "## Key Rules\n"
        )
        # Extract bullet points if present
        bullets = []
        for line in lines:
            if line.strip().startswith("- "):
                bullets.append(line.strip())
            if len(bullets) >= 6:
                break
        if bullets:
            content += "\n".join(bullets[:6]) + "\n\n"
        content += "## Example (from summary)\n\n"
        content += summary[:2000] + "\n"
        dest.write_text(content, encoding="utf-8")
        print("Wrote", dest)
        created += 1
    print("generated", created, "instruction files")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())

