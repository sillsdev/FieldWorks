#!/usr/bin/env python3
"""
fill_copilot_frontmatter.py — Ensure COPILOT.md frontmatter exists and has required fields.

Behavior:
- If frontmatter missing: insert with provided values at top of file.
- If present: fill missing fields only; preserve existing values.
- last-reviewed defaults to today (YYYY-MM-DD) if missing.
- last-reviewed-tree defaults to the folder hash at the selected ref if not provided.

Usage:
    python .github/fill_copilot_frontmatter.py [--root <repo-root>] [--status draft|verified] [--ref <sha|HEAD>] [--dry-run]
"""
import argparse
import datetime as dt
import sys
from pathlib import Path
from typing import Optional

from copilot_tree_hash import compute_folder_tree_hash


def find_repo_root(start: Path) -> Path:
    p = start.resolve()
    while p != p.parent:
        if (p / ".git").exists():
            return p
        p = p.parent
    return start.resolve()


def parse_frontmatter(text: str):
    lines = text.splitlines()
    if len(lines) >= 3 and lines[0].strip() == "---":
        # Find closing '---'
        end_idx = -1
        for i in range(1, min(len(lines), 200)):
            if lines[i].strip() == "---":
                end_idx = i
                break
        if end_idx == -1:
            return None, text
        fm_lines = lines[1:end_idx]
        fm = {}
        for l in fm_lines:
            l = l.strip()
            if not l or l.startswith("#"):
                continue
            if ":" in l:
                k, v = l.split(":", 1)
                fm[k.strip()] = v.strip().strip('"')
        body = "\n".join(lines[end_idx + 1 :])
        return fm, body
    return None, text


def render_frontmatter(fm: dict) -> str:
    lines = ["---"]
    for k in ["last-reviewed", "last-reviewed-tree", "status"]:
        if k in fm and fm[k] is not None:
            lines.append(f"{k}: {fm[k]}")
    lines.append("---")
    return "\n".join(lines) + "\n"


def ensure_frontmatter(
    path: Path, status: str, folder_hash: Optional[str], dry_run=False
) -> bool:
    text = path.read_text(encoding="utf-8", errors="replace")
    fm, body = parse_frontmatter(text)
    today = dt.date.today().strftime("%Y-%m-%d")
    changed = False
    hash_value = folder_hash or "FIXME(set-tree-hash)"
    if not fm:
        fm = {
            "last-reviewed": today,
            "last-reviewed-tree": hash_value,
            "status": status or "draft",
        }
        new_text = render_frontmatter(fm) + body
        changed = True
    else:
        # Fill missing
        if not fm.get("last-reviewed"):
            fm["last-reviewed"] = today
            changed = True
        if "last-verified-commit" in fm:
            fm.pop("last-verified-commit")
            changed = True
        existing_hash = fm.get("last-reviewed-tree")
        if existing_hash != hash_value:
            fm["last-reviewed-tree"] = hash_value
            changed = True
        if not fm.get("status"):
            fm["status"] = status or "draft"
            changed = True
        if changed:
            new_text = render_frontmatter(fm) + body
        else:
            new_text = text

    if changed and not dry_run:
        path.write_text(new_text, encoding="utf-8")
    return changed


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--root", default=str(find_repo_root(Path.cwd())))
    ap.add_argument("--status", default="draft")
    ap.add_argument("--ref", "--commit", dest="ref", default="HEAD")
    ap.add_argument("--dry-run", action="store_true")
    args = ap.parse_args()

    root = Path(args.root).resolve()
    src = root / "Src"
    if not src.exists():
        print(f"ERROR: Src/ not found under {root}")
        return 2

    changed_count = 0
    hash_cache = {}
    for copath in src.rglob("COPILOT.md"):
        parent = copath.parent
        folder_hash = hash_cache.get(parent)
        if folder_hash is None:
            try:
                folder_hash = compute_folder_tree_hash(root, parent, ref=args.ref)
            except Exception as exc:  # pragma: no cover - defensive logging only
                print(f"WARNING: unable to compute tree hash for {parent}: {exc}")
                folder_hash = None
            hash_cache[parent] = folder_hash

        if ensure_frontmatter(copath, args.status, folder_hash, dry_run=args.dry_run):
            print(f"Updated: {copath}")
            changed_count += 1

    print(f"Frontmatter ensured. Files changed: {changed_count}")
    return 0


if __name__ == "__main__":
    sys.exit(main())
