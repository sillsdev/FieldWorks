#!/usr/bin/env python3
"""Simple linter/validator for instructions files.
Checks for frontmatter and short headings as recommended by Copilot guidance.
"""
from __future__ import annotations
import argparse
from pathlib import Path
import re

ROOT = Path(__file__).resolve().parents[2]
INSTRUCTIONS_DIR = ROOT / '.github' / 'instructions'
SRC_DIR = ROOT / 'Src'

FRONTMATTER_RE = re.compile(r'^---\s*$', re.MULTILINE)

def has_frontmatter(p: Path) -> bool:
    text = p.read_text(encoding='utf-8', errors='ignore')
    # Accept frontmatter anywhere in the first 20 lines (some files include backticks for renderers)
    head = '\n'.join(text.splitlines()[:20])
    return bool(FRONTMATTER_RE.search(head))

def check_sections(p: Path) -> list[str]:
    text = p.read_text(encoding='utf-8', errors='ignore')
    missing = []
    if '# ' not in text and '## ' not in text:
        missing.append('No headings found; add Purpose & Scope and Key Rules')
    if '## Purpose' not in text and '## Purpose & Scope' not in text:
        missing.append('Missing "Purpose & Scope" section')
    return missing


def main() -> int:
    failures = 0
    for p in sorted(INSTRUCTIONS_DIR.glob('*.instructions.md')):
        print('Checking', p)
        if not has_frontmatter(p):
            print('  ERROR: Missing YAML frontmatter')
            failures += 1
            continue
        missing = check_sections(p)
        if missing:
            for m in missing:
                print('  WARNING:', m)
    # Check COPILOT.md files in Src
    for p in sorted(SRC_DIR.rglob('COPILOT.md')):
        print('Checking', p)
        if p.stat().st_size > (1024*10):
            print('  WARNING: COPILOT.md is large; consider a shorter copilot.instructions.md for agent consumption')
    return 1 if failures else 0

if __name__ == '__main__':
    raise SystemExit(main())
