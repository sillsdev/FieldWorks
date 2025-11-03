#!/usr/bin/env python3
"""
detect_copilot_needed.py — Identify folders with code/config changes that likely require COPILOT.md updates.

Intended for CI (advisory or failing), and for local pre-commit checks.

Logic:
- Compute changed files between a base and head ref (or since a rev).
- Consider only changes under Src/** that match code/config extensions.
- Group by top-level folder: Src/<Folder>/...
- For each impacted folder, compare the folder's current git tree hash to the
    `last-reviewed-tree` recorded in COPILOT.md and track whether the doc changed.
- Report folders whose hashes no longer match or whose docs are missing.
- Optionally validate changed COPILOT.md files with check_copilot_docs.py.

Exit codes:
  0 = no issues (either no impacted folders or all have COPILOT.md changes, and validations passed)
  1 = advisory warnings (impacted folders without COPILOT.md updated), when --strict not set
  2 = strict failure when --strict is set and there are issues, or validation fails

Examples:
  python .github/detect_copilot_needed.py --base origin/release/9.3 --head HEAD --strict
  python .github/detect_copilot_needed.py --since origin/release/9.3
"""
import argparse
import json
import os
import subprocess
from pathlib import Path
from typing import Dict, Optional, Tuple

from copilot_tree_hash import compute_folder_tree_hash

CODE_EXTS = {
    ".cs",
    ".cpp",
    ".cc",
    ".c",
    ".h",
    ".hpp",
    ".ixx",
    ".xml",
    ".xsl",
    ".xslt",
    ".xsd",
    ".dtd",
    ".xaml",
    ".resx",
    ".config",
    ".csproj",
    ".vcxproj",
    ".props",
    ".targets",
}


def run(cmd, cwd=None):
    return subprocess.check_output(cmd, cwd=cwd, stderr=subprocess.STDOUT).decode(
        "utf-8", errors="replace"
    )


def git_changed_files(
    root: Path, base: str = None, head: str = "HEAD", since: str = None
):
    if since:
        diff_range = f"{since}..{head}"
    elif base:
        diff_range = f"{base}..{head}"
    else:
        # Fallback: compare to merge-base with origin/HEAD (best effort)
        try:
            mb = run(["git", "merge-base", head, "origin/HEAD"], cwd=str(root)).strip()
            diff_range = f"{mb}..{head}"
        except Exception:
            diff_range = f"HEAD~1..{head}"
    out = run(["git", "diff", "--name-only", diff_range], cwd=str(root))
    return [l.strip().replace("\\", "/") for l in out.splitlines() if l.strip()]


def top_level_src_folder(path: str):
    # Expect paths like Src/<Folder>/...
    parts = path.split("/")
    if len(parts) >= 2 and parts[0] == "Src":
        return "/".join(parts[:2])  # Src/Folder
    return None


def parse_frontmatter(path: Path) -> Tuple[Optional[Dict[str, str]], str]:
    if not path.exists():
        return None, ""
    text = path.read_text(encoding="utf-8", errors="replace")
    lines = text.splitlines()
    if len(lines) >= 3 and lines[0].strip() == "---":
        end_idx = -1
        for i in range(1, min(len(lines), 200)):
            if lines[i].strip() == "---":
                end_idx = i
                break
        if end_idx == -1:
            return None, text
        fm_lines = lines[1:end_idx]
        fm: Dict[str, str] = {}
        for l in fm_lines:
            l = l.strip()
            if not l or l.startswith("#"):
                continue
            if ":" in l:
                k, v = l.split(":", 1)
                fm[k.strip()] = v.strip().strip('"')
        return fm, "\n".join(lines[end_idx + 1 :])
    return None, ""


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--root", default=str(Path.cwd()))
    ap.add_argument(
        "--base", default=None, help="Base git ref (e.g., origin/release/9.3)"
    )
    ap.add_argument("--head", default="HEAD", help="Head ref (default HEAD)")
    ap.add_argument(
        "--since", default=None, help="Alternative to base/head: since this ref"
    )
    ap.add_argument("--json", dest="json_out", default=None)
    ap.add_argument(
        "--validate-changed",
        action="store_true",
        help="Validate changed COPILOT.md with check_copilot_docs.py",
    )
    ap.add_argument("--strict", action="store_true", help="Exit non-zero on issues")
    args = ap.parse_args()

    root = Path(args.root).resolve()
    changed = git_changed_files(root, base=args.base, head=args.head, since=args.since)

    impacted: Dict[str, set] = {}
    copilot_changed = set()
    for p in changed:
        if p.endswith("/COPILOT.md"):
            copilot_changed.add(p)
        # Only care about Src/** files that look like code/config
        if not p.startswith("Src/"):
            continue
        if p.endswith("/COPILOT.md"):
            continue
        _, ext = os.path.splitext(p)
        if ext.lower() not in CODE_EXTS:
            continue
        folder = top_level_src_folder(p)
        if folder:
            impacted.setdefault(folder, set()).add(p)

    results = []
    issues = 0
    for folder, files in sorted(impacted.items()):
        copath_rel = f"{folder}/COPILOT.md"
        copath = root / copath_rel
        folder_path = root / folder
        doc_changed = copath_rel in copilot_changed
        reasons = []
        recorded_hash: Optional[str] = None
        fm, _ = parse_frontmatter(copath)
        if not copath.exists():
            reasons.append("COPILOT.md missing")
        elif not fm:
            reasons.append("frontmatter missing")
        else:
            recorded_hash = fm.get("last-reviewed-tree")
            if not recorded_hash or recorded_hash.startswith("FIXME"):
                reasons.append("last-reviewed-tree missing or placeholder")
        current_hash: Optional[str] = None
        hash_error: Optional[str] = None
        if folder_path.exists():
            try:
                current_hash = compute_folder_tree_hash(
                    root, folder_path, ref=args.head
                )
            except Exception as exc:  # pragma: no cover - diagnostics only
                hash_error = str(exc)
                reasons.append("unable to compute tree hash")
        else:
            reasons.append("folder missing at head ref")

        if current_hash and recorded_hash and current_hash == recorded_hash:
            up_to_date = True
        else:
            up_to_date = False
            if current_hash and recorded_hash and current_hash != recorded_hash:
                reasons.append("tree hash mismatch")
            if not doc_changed and not reasons:
                # Defensive catch-all
                reasons.append("COPILOT.md not updated")

        entry = {
            "folder": folder,
            "files_changed": sorted(files),
            "copilot_path": copath_rel,
            "copilot_changed": doc_changed,
            "last_reviewed_tree": recorded_hash,
            "current_tree": current_hash,
            "status": "OK" if up_to_date else "STALE",
            "reasons": reasons,
        }
        if hash_error:
            entry["hash_error"] = hash_error
        if not up_to_date:
            issues += 1
        results.append(entry)

    # Optional validation for changed COPILOT.md files
    validation_failures = []
    if args.validate_changed and copilot_changed:
        try:
            cmd = ["python", ".github/check_copilot_docs.py", "--fail"]
            # Limit to changed files by setting CWD and relying on script to scan all; keep simple
            run(cmd, cwd=str(root))
        except subprocess.CalledProcessError as e:
            validation_failures.append(e.output.decode("utf-8", errors="replace"))
            issues += 1

    print(f"Impacted folders: {len(impacted)}")
    for e in results:
        if e["status"] == "OK":
            detail = "hash aligned"
        else:
            detail = ", ".join(e["reasons"]) if e["reasons"] else "hash mismatch"
        print(f"- {e['folder']}: {e['status']} ({detail})")

    if validation_failures:
        print("\nValidation failures from check_copilot_docs.py:")
        for vf in validation_failures:
            print(vf)

    if args.json_out:
        with open(args.json_out, "w", encoding="utf-8") as f:
            json.dump({"impacted": results}, f, indent=2)

    if args.strict and issues:
        return 2
    return 0 if not issues else 1


if __name__ == "__main__":
    raise SystemExit(main())
