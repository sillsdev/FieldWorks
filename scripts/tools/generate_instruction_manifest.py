#!/usr/bin/env python3
"""Generate a manifest.json from inventory.yml for instructions discoverability."""
from __future__ import annotations
import json
from pathlib import Path
import re

ROOT = Path(__file__).resolve().parents[2]
INVENTORY = ROOT / '.github' / 'instructions' / 'inventory.yml'
MANIFEST = ROOT / '.github' / 'instructions' / 'manifest.json'


def main():
    if not INVENTORY.exists():
        raise SystemExit('Run generate_instruction_inventory.py first')
    text = INVENTORY.read_text(encoding='utf-8')
    items = []
    current = None
    for line in text.splitlines():
        m = re.match(r'^-\s+path:\s+(.*)$', line)
        if m:
            if current:
                items.append(current)
            current = { 'path': m.group(1).strip() }
            continue
        if current is None:
            continue
        m2 = re.match(r'^\s*(\w+):\s+"?(.*)"?$', line)
        if m2:
            key = m2.group(1)
            val = m2.group(2).strip()
            current[key] = val
    if current:
        items.append(current)
    manifest = { 'generated': True, 'items': items }
    MANIFEST.write_text(json.dumps(manifest, indent=2), encoding='utf-8')
    print(f'Wrote {MANIFEST} ({len(items)} items)')


if __name__ == '__main__':
    main()
