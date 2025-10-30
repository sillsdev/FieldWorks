#!/usr/bin/env python3
"""
fill_copilot_frontmatter.py — Ensure COPILOT.md frontmatter exists and has required fields.

Behavior:
- If frontmatter missing: insert with provided values at top of file.
- If present: fill missing fields only; preserve existing values.
- last-reviewed defaults to today (YYYY-MM-DD) if missing.
- last-verified-commit defaults to current HEAD if not provided.

Usage:
    python .github/fill_copilot_frontmatter.py [--root <repo-root>] [--status draft|verified] [--commit <sha|HEAD>] [--dry-run]
"""
import argparse
import datetime as dt
import os
import subprocess
import sys
from pathlib import Path


def find_repo_root(start: Path) -> Path:
    p = start.resolve()
    while p != p.parent:
        if (p / ".git").exists():
            return p
        p = p.parent
    return start.resolve()


def git_rev_parse(root: Path, ref: str = "HEAD", short=True) -> str:
    try:
        args = ["git", "rev-parse"]
        if short:
            args.append("--short")
        args.append(ref)
        out = subprocess.check_output(args, cwd=str(root))
        return out.decode("utf-8").strip()
    except Exception:
        return ""


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
    for k in ["last-reviewed", "last-verified-commit", "status"]:
        if k in fm and fm[k] is not None:
            lines.append(f"{k}: {fm[k]}")
    lines.append("---")
    return "\n".join(lines) + "\n"


def ensure_frontmatter(path: Path, status: str, commit: str, dry_run=False) -> bool:
    text = path.read_text(encoding="utf-8", errors="replace")
    fm, body = parse_frontmatter(text)
    today = dt.date.today().strftime("%Y-%m-%d")
    changed = False
    if not fm:
        fm = {
            "last-reviewed": today,
            "last-verified-commit": commit or "FIXME(set-sha)",
            "status": status or "draft",
        }
        new_text = render_frontmatter(fm) + body
        changed = True
    else:
        # Fill missing
        if not fm.get("last-reviewed"):
            fm["last-reviewed"] = today
            changed = True
        if not fm.get("last-verified-commit"):
            fm["last-verified-commit"] = commit or "FIXME(set-sha)"
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
    ap.add_argument("--commit", default="HEAD")
    ap.add_argument("--dry-run", action="store_true")
    args = ap.parse_args()

    root = Path(args.root).resolve()
    src = root / "Src"
    if not src.exists():
        print(f"ERROR: Src/ not found under {root}")
        return 2

    commit = args.commit
    if commit.upper() == "HEAD":
        commit = git_rev_parse(root, ref="HEAD", short=True)

    changed_count = 0
    for copath in src.rglob("COPILOT.md"):
        if ensure_frontmatter(copath, args.status, commit, dry_run=args.dry_run):
            print(f"Updated: {copath}")
            changed_count += 1

    print(f"Frontmatter ensured. Files changed: {changed_count}")
    return 0


if __name__ == "__main__":
    sys.exit(main())
