#!/usr/bin/env python3
"""Plan focused COPILOT.md refreshes using cached diffs."""
from __future__ import annotations

import argparse
import json
import subprocess
from datetime import datetime, timezone
import os
from pathlib import Path
from typing import Dict, List, Optional, Sequence, Tuple

FILE_GROUPS = {
    "Project files": {".csproj", ".vcxproj", ".props", ".targets"},
    "Key C# files": {".cs"},
    "Key C++ files": {".cpp", ".cc", ".c"},
    "Key headers": {".h", ".hpp", ".ixx"},
    "Data contracts/transforms": {
        ".xml",
        ".xsl",
        ".xslt",
        ".xsd",
        ".dtd",
        ".xaml",
        ".resx",
        ".config",
    },
}
def collect_reference_groups(folder: Path, root: Path, limit_per_group: int = 25) -> Dict[str, List[str]]:

    groups: Dict[str, List[str]] = {k: [] for k in FILE_GROUPS}
    skip = {"obj", "bin", "packages", "output", "downloads"}
    for dirpath, dirnames, filenames in os.walk(folder):
        rel_parts = {p.lower() for p in Path(dirpath).parts}
        if rel_parts & skip:
            continue
        for filename in filenames:
            ext = os.path.splitext(filename)[1].lower()
            for group_name, exts in FILE_GROUPS.items():
                if ext in exts:
                    rel = Path(dirpath, filename).relative_to(root)
                    groups[group_name].append(str(rel).replace("\\", "/"))
                    break
    for key in groups:
        groups[key] = sorted(groups[key])[:limit_per_group]
    return groups

from copilot_cache import CopilotCache
from copilot_change_utils import (
    classify_with_status,
    compute_risk_score,
    summarize_paths,
)
from copilot_tree_hash import compute_folder_tree_hash


def run_git(cmd: Sequence[str], cwd: Path) -> str:
    return subprocess.check_output(cmd, cwd=str(cwd), stderr=subprocess.STDOUT).decode(
        "utf-8", errors="replace"
    )


def parse_frontmatter(path: Path) -> Tuple[Optional[Dict[str, str]], str]:
    if not path.exists():
        return None, ""
    text = path.read_text(encoding="utf-8", errors="replace")
    lines = text.splitlines()
    if len(lines) < 3 or lines[0].strip() != "---":
        return None, text
    end_idx = -1
    for idx in range(1, min(len(lines), 200)):
        if lines[idx].strip() == "---":
            end_idx = idx
            break
    if end_idx == -1:
        return None, text
    fm: Dict[str, str] = {}
    for raw in lines[1:end_idx]:
        stripped = raw.strip()
        if not stripped or stripped.startswith("#"):
            continue
        if ":" in stripped:
            key, value = stripped.split(":", 1)
            fm[key.strip()] = value.strip().strip('"')
    body = "\n".join(lines[end_idx + 1 :])
    return fm, body


def read_detect_json(path: Optional[Path]) -> List[Dict[str, object]]:
    if not path:
        return []
    data = json.loads(path.read_text(encoding="utf-8"))
    return data.get("impacted", [])


def find_matching_commit(
    root: Path, folder_rel: str, folder_path: Path, target_hash: Optional[str], head: str, limit: int
) -> Optional[str]:
    if not target_hash or target_hash.startswith("FIXME"):
        return None
    try:
        revs = run_git(
            [
                "git",
                "rev-list",
                "--max-count",
                str(limit),
                head,
                "--",
                folder_rel,
            ],
            root,
        ).splitlines()
    except subprocess.CalledProcessError:
        return None
    for commit in revs:
        try:
            digest = compute_folder_tree_hash(root, folder_path, ref=commit)
        except Exception:
            continue
        if digest == target_hash:
            return commit
    return None


def determine_fallback_base(root: Path, head: str) -> str:
    try:
        mb = run_git(["git", "merge-base", head, "origin/HEAD"], root).strip()
        if mb:
            return mb
    except Exception:
        pass
    return f"{head}~1"


def git_diff_lines(root: Path, base: str, head: str, folder_rel: str) -> List[str]:
    try:
        output = run_git(
            [
                "git",
                "diff",
                "--name-status",
                f"{base}..{head}",
                "--",
                folder_rel,
            ],
            root,
        )
    except subprocess.CalledProcessError as exc:
        raise RuntimeError(exc.output.decode("utf-8", errors="replace")) from exc
    return [line.strip() for line in output.splitlines() if line.strip()]


def git_log(root: Path, base: Optional[str], head: str, folder_rel: str) -> List[Dict[str, str]]:
    if base:
        rev_range = f"{base}..{head}"
    else:
        rev_range = head
    try:
        output = run_git(
            [
                "git",
                "log",
                "--date=iso",
                "--pretty=format:%H\t%ad\t%s",
                rev_range,
                "--",
                folder_rel,
            ],
            root,
        )
    except subprocess.CalledProcessError:
        return []
    entries = []
    for line in output.splitlines():
        parts = line.split("\t", 2)
        if len(parts) != 3:
            continue
        entries.append({"hash": parts[0], "date": parts[1], "summary": parts[2]})
    return entries


def build_prompts(folder: str, counts: Dict[str, int], risk: str) -> Dict[str, List[str]]:
    total = counts.get("total", 0)
    code = counts.get("code", 0)
    tests = counts.get("tests", 0)
    resources = counts.get("resources", 0)
    summary = (
        f"{total} files changed (code={code}, tests={tests}, resources={resources}); risk={risk}."
    )
    return {
        "doc-refresh": [
            f"Update COPILOT.md for {folder}. Prioritize Purpose/Architecture sections using planner data.",
            f"Highlight API or UI updates, then confirm Usage/Test sections reflect {summary}",
            "Finish with verification notes and TODOs for manual testing.",
        ]
    }


def build_plan_entry(
    root: Path,
    folder_rel: str,
    head: str,
    fallback_base: str,
    cache: CopilotCache,
    refresh_cache: bool,
    search_limit: int,
) -> Optional[Dict[str, object]]:
    folder_path = root / folder_rel
    if not folder_path.exists():
        return None
    copilot_path = folder_path / "COPILOT.md"
    fm, _ = parse_frontmatter(copilot_path)
    if fm is None:
        fm = {}
    recorded_tree = fm.get("last-reviewed-tree")
    notes: List[str] = []
    if not recorded_tree or recorded_tree.startswith("FIXME"):
        notes.append("missing last-reviewed-tree")
    try:
        current_tree = compute_folder_tree_hash(root, folder_path, ref=head)
    except Exception as exc:
        notes.append(f"unable to compute current tree: {exc}")
        current_tree = None

    cache_entry: Optional[Dict[str, object]] = None
    if not refresh_cache and recorded_tree and current_tree:
        cache_entry = cache.load_folder(folder_rel, recorded_tree, current_tree)
    if cache_entry:
        cache_entry = dict(cache_entry)
        cache_entry.setdefault("notes", []).extend(notes)
        cache_entry["from_cache"] = True
        return cache_entry

    recorded_commit = find_matching_commit(
        root, folder_rel, folder_path, recorded_tree, head, search_limit
    )
    if not recorded_commit and recorded_tree:
        notes.append("recorded hash commit not found (consider increasing --search-limit)")
    diff_base = recorded_commit or fallback_base
    diff_lines = git_diff_lines(root, diff_base, head, folder_rel)
    classified = classify_with_status(diff_lines)
    counts = summarize_paths([item["path"] for item in classified])
    status_counts: Dict[str, int] = {}
    for item in classified:
        status_counts[item["status"]] = status_counts.get(item["status"], 0) + 1
    risk = compute_risk_score(counts)
    commit_log = git_log(root, recorded_commit or fallback_base, head, folder_rel)

    entry = {
        "folder": folder_rel,
        "copilot_path": str(copilot_path.relative_to(root)),
        "recorded_tree": recorded_tree,
        "current_tree": current_tree,
        "recorded_commit": recorded_commit,
        "diff_base": diff_base,
        "risk_score": risk,
        "change_counts": counts,
        "status_counts": status_counts,
        "changes": classified,
        "commit_log": commit_log,
        "notes": notes,
        "prompts": build_prompts(folder_rel, counts, risk),
        "from_cache": False,
            "reference_groups": collect_reference_groups(folder_path, root),  # Add reference groups
    }
    cache.save_folder(folder_rel, entry)
    return entry


def resolve_folders(
    root: Path,
    detect_entries: List[Dict[str, object]],
    folders: Optional[List[str]],
    include_all: bool,
) -> List[str]:
    targets = set()
    if folders:
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
    elif detect_entries:
        for entry in detect_entries:
            folder = entry.get("folder")
            status = entry.get("status")
            if folder and status != "OK":
                targets.add(str(folder))
    if include_all:
        for path in sorted(root.glob("Src/**/COPILOT.md")):
            targets.add(path.parent.relative_to(root).as_posix())
    return sorted(targets)


def main() -> int:
    ap = argparse.ArgumentParser(description="Plan COPILOT.md updates with cached diffs")
    ap.add_argument("--root", default=str(Path.cwd()))
    ap.add_argument("--head", default="HEAD")
    ap.add_argument("--detect-json", help="Path to detect_copilot_needed --json output")
    ap.add_argument("--folders", nargs="*", help="Explicit Src/<Folder> paths")
    ap.add_argument("--all", action="store_true", help="Plan for every COPILOT.md folder")
    ap.add_argument("--out", default=".cache/copilot/diff-plan.json")
    ap.add_argument("--fallback-base", help="Fallback git ref if recorded hash commit is unknown")
    ap.add_argument("--refresh-cache", action="store_true")
    ap.add_argument("--search-limit", type=int, default=800, help="Max commits to scan per folder")
    args = ap.parse_args()

    root = Path(args.root).resolve()
    detect_entries = read_detect_json(Path(args.detect_json)) if args.detect_json else []
    targets = resolve_folders(root, detect_entries, args.folders, args.all)
    if not targets:
        print("No folders to plan. Provide --folders, --all, or --detect-json input.")
        return 0

    fallback_base = args.fallback_base or determine_fallback_base(root, args.head)
    cache = CopilotCache(root)
    entries: List[Dict[str, object]] = []

    for folder in targets:
        entry = build_plan_entry(
            root,
            folder,
            args.head,
            fallback_base,
            cache,
            args.refresh_cache,
            args.search_limit,
        )
        if entry:
            entries.append(entry)
            print(
                f"Planned {folder}: {entry['change_counts'].get('total', 0)} files, risk={entry['risk_score']}"
            )
        else:
            print(f"Skipped {folder} (unable to build entry)")

    output_path = (root / args.out) if not Path(args.out).is_absolute() else Path(args.out)
    output_path.parent.mkdir(parents=True, exist_ok=True)
    payload = {
        "generated_at": datetime.now(timezone.utc).isoformat(),
        "head": args.head,
        "fallback_base": fallback_base,
        "folders": entries,
    }
    output_path.write_text(json.dumps(payload, indent=2), encoding="utf-8")
    print(f"Wrote plan with {len(entries)} folder(s) to {output_path}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
