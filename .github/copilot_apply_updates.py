#!/usr/bin/env python3
"""Apply planner output to refresh auto sections in AGENTS.md files."""
from __future__ import annotations

import argparse
import json
from pathlib import Path
from typing import Dict, List, Optional

AUTO_START = "<!-- copilot:auto-change-log start -->"
AUTO_END = "<!-- copilot:auto-change-log end -->"


def read_plan(path: Path) -> Dict[str, object]:
    return json.loads(path.read_text(encoding="utf-8"))


def select_entries(plan: Dict[str, object], folders: Optional[List[str]]) -> List[Dict[str, object]]:
    entries = plan.get("folders", [])
    if not folders:
        return entries
    want = {f.replace("\\", "/") for f in folders}
    filtered = []
    for entry in entries:
        folder = entry.get("folder")
        if folder in want:
            filtered.append(entry)
    return filtered


def build_auto_block(entry: Dict[str, object], heading: str, max_files: int, max_commits: int) -> str:
    counts = entry.get("change_counts", {})
    summary = (
        f"Files: {counts.get('total', 0)} (code={counts.get('code', 0)}, tests={counts.get('tests', 0)}, resources={counts.get('resources', 0)})"
    )
    risk = entry.get("risk_score", "unknown")
    recorded = entry.get("recorded_commit") or entry.get("diff_base")
    lines = [AUTO_START, f"## {heading}", "", f"- Snapshot: {recorded}", f"- Risk: {risk}", f"- {summary}"]

    changes = entry.get("changes", [])
    if changes:
        lines.append("\n### File Highlights")
        for change in changes[:max_files]:
            status = change.get("status", "?")
            rel = change.get("path", "")
            kind = change.get("kind", "")
            ext = change.get("ext", "")
            lines.append(f"- {status} {rel} ({kind or 'code'} {ext})")
        if len(changes) > max_files:
            lines.append(f"- ... {len(changes) - max_files} more file(s)")

    log_entries = entry.get("commit_log", [])
    if log_entries:
        lines.append("\n### Recent Commits")
        for commit in log_entries[:max_commits]:
            short = commit.get("hash", "")[:8]
            date = commit.get("date", "")
            summary_line = commit.get("summary", "")
            lines.append(f"- {short} {date} — {summary_line}")
        if len(log_entries) > max_commits:
            lines.append(f"- ... {len(log_entries) - max_commits} more commit(s)")

    prompts = entry.get("prompts", {}).get("doc-refresh", [])
    if prompts:
        lines.append("\n### Prompt seeds")
        for prompt in prompts:
            lines.append(f"- {prompt}")

    lines.append(AUTO_END)
    lines.append("")
    return "\n".join(lines)


def find_frontmatter_end(text: str) -> int:
    lines = text.splitlines(keepends=True)
    if not lines or not lines[0].strip().startswith("---"):
        return 0
    for idx in range(1, min(len(lines), 200)):
        if lines[idx].strip().startswith("---"):
            return sum(len(line) for line in lines[: idx + 1])
    return 0


def inject_block(text: str, block: str) -> str:
    start = text.find(AUTO_START)
    if start != -1:
        end = text.find(AUTO_END, start)
        if end == -1:
            end = start
        else:
            end += len(AUTO_END)
        before = text[:start].rstrip()
        after = text[end:].lstrip()
        pieces = [before, block, after]
        return "\n\n".join(piece for piece in pieces if piece).strip() + "\n"
    fm_end = find_frontmatter_end(text)
    before = text[:fm_end].rstrip()
    after = text[fm_end:].lstrip()
    parts = [before, block, after]
    return "\n\n".join(part for part in parts if part).strip() + "\n"


def apply_auto_block(copath: Path, block: str, dry_run: bool) -> bool:
    if not copath.exists():
        return False
    original = copath.read_text(encoding="utf-8", errors="replace")
    updated = inject_block(original, block)
    if updated == original:
        return False
    if dry_run:
        return True
    copath.write_text(updated, encoding="utf-8")
    return True


def main() -> int:
    ap = argparse.ArgumentParser(description="Inject auto change-log sections into AGENTS.md files")
    ap.add_argument("--root", default=str(Path.cwd()))
    ap.add_argument("--plan", default=".cache/copilot/diff-plan.json")
    ap.add_argument("--folders", nargs="*", help="Subset of folders to update")
    ap.add_argument("--heading", default="Change Log (auto)")
    ap.add_argument("--max-files", type=int, default=25)
    ap.add_argument("--max-commits", type=int, default=10)
    ap.add_argument("--dry-run", action="store_true")
    args = ap.parse_args()

    root = Path(args.root).resolve()
    plan_path = (root / args.plan) if not Path(args.plan).is_absolute() else Path(args.plan)
    if not plan_path.exists():
        print(f"Plan file not found: {plan_path}")
        return 1

    plan = read_plan(plan_path)
    entries = select_entries(plan, args.folders)
    if not entries:
        print("No matching entries to apply.")
        return 0

    applied = 0
    for entry in entries:
        copilot_rel = entry.get("copilot_path")
        if not copilot_rel:
            continue
        copath = (root / copilot_rel).resolve()
        block = build_auto_block(entry, args.heading, args.max_files, args.max_commits)
        changed = apply_auto_block(copath, block, args.dry_run)
        if changed:
            applied += 1
            action = "(dry run)" if args.dry_run else ""
            print(f"Updated {copath} {action}")
        else:
            print(f"No changes needed for {copath}")

    if args.dry_run:
        print(f"Dry run complete. {applied} file(s) would change.")
    else:
        print(f"Applied auto blocks to {applied} file(s).")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())

