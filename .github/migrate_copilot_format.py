#!/usr/bin/env python3
"""One-time helper to migrate AGENTS.md files to the new auto change-log format."""
from __future__ import annotations

import argparse
import re
from pathlib import Path
from typing import List, Tuple

AUTO_START = "<!-- copilot:auto-change-log start -->"
AUTO_END = "<!-- copilot:auto-change-log end -->"
AUTO_PLACEHOLDER = """<!-- copilot:auto-change-log start -->
## Change Log (auto)

This section is populated by running:
1. `python .github/plan_copilot_updates.py --folders <Folder>`
2. `python .github/copilot_apply_updates.py --folders <Folder>`

Do not edit this block manually; rerun the scripts above after code or doc updates.
<!-- copilot:auto-change-log end -->
"""

AUTO_HINT_HEADING = "## References (auto-generated hints)"
SECTION_RE = re.compile(r"^## ", re.MULTILINE)


def find_frontmatter_end(text: str) -> int:
    if not text.startswith("---\n"):
        return 0
    idx = text.find("\n---", 3)
    if idx == -1:
        return 0
    return idx + len("\n---\n")


def remove_auto_hint_section(text: str) -> Tuple[str, bool]:
    if AUTO_HINT_HEADING not in text:
        return text, False
    start = text.find(AUTO_HINT_HEADING)
    if start == -1:
        return text, False
    match = SECTION_RE.search(text, start + len(AUTO_HINT_HEADING))
    end = match.start() if match else len(text)
    updated = (text[:start].rstrip() + "\n\n" + text[end:].lstrip()).strip() + "\n"
    return updated, True


def ensure_auto_block(text: str) -> Tuple[str, bool]:
    if AUTO_START in text and AUTO_END in text:
        return text, False
    fm_end = find_frontmatter_end(text)
    before = text[:fm_end].rstrip()
    after = text[fm_end:].lstrip()
    pieces = [part for part in [before, AUTO_PLACEHOLDER.strip(), after] if part]
    updated = "\n\n".join(pieces) + "\n"
    return updated, True


def migrate_file(path: Path, dry_run: bool) -> Tuple[bool, List[str]]:
    text = path.read_text(encoding="utf-8", errors="replace")
    notes: List[str] = []
    text, removed = remove_auto_hint_section(text)
    if removed:
        notes.append("removed auto hints section")
    text, inserted = ensure_auto_block(text)
    if inserted:
        notes.append("inserted auto change-log placeholder")
    if not notes:
        return False, []
    if not dry_run:
        path.write_text(text, encoding="utf-8")
    return True, notes


def resolve_folders(root: Path, folders: List[str], include_all: bool) -> List[Path]:
    targets = set()
    for folder in folders:
        rel = folder.replace("\\", "/")
        if rel.startswith("Src/"):
            targets.add(rel)
        else:
            abs_path = Path(folder)
            if not abs_path.is_absolute():
                abs_path = (root / folder).resolve()
            rel = abs_path.relative_to(root).as_posix()
            targets.add(rel)
    if include_all or not targets:
        for copilot in root.glob("Src/**/AGENTS.md"):
            targets.add(copilot.parent.relative_to(root).as_posix())
    paths = []
    for rel in sorted(targets):
        copilot = root / rel / "AGENTS.md"
        if copilot.exists():
            paths.append(copilot)
    return paths


def main() -> int:
    ap = argparse.ArgumentParser(description="Migrate AGENTS.md files to new auto-block format")
    ap.add_argument("--root", default=str(Path.cwd()))
    ap.add_argument("--folders", nargs="*", default=[], help="Specific Src/<Folder> entries")
    ap.add_argument("--all", action="store_true", help="Process every AGENTS.md under Src/")
    ap.add_argument("--dry-run", action="store_true")
    args = ap.parse_args()

    root = Path(args.root).resolve()
    paths = resolve_folders(root, args.folders, args.all)
    if not paths:
        print("No AGENTS.md files matched the criteria.")
        return 0

    changed = 0
    for copilot_path in paths:
        updated, notes = migrate_file(copilot_path, args.dry_run)
        if updated:
            changed += 1
            action = "DRY" if args.dry_run else "UPDATED"
            print(f"[{action}] {copilot_path}: {', '.join(notes)}")
        else:
            print(f"[SKIP] {copilot_path}: already compliant")

    if args.dry_run:
        print(f"Dry run complete. {changed} file(s) would change.")
    else:
        print(f"Migration complete. {changed} file(s) updated.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())

