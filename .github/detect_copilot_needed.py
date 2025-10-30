#!/usr/bin/env python3
"""
detect_copilot_needed.py — Identify folders with code/config changes that likely require COPILOT.md updates.

Intended for CI (advisory or failing), and for local pre-commit checks.

Logic:
- Compute changed files between a base and head ref (or since a rev).
- Consider only changes under Src/** that match code/config extensions.
- Group by top-level folder: Src/<Folder>/...
- For each impacted folder, check whether its COPILOT.md changed in the same diff.
- Print a summary and optionally emit JSON.
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

    impacted = {}
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
        copath = f"{folder}/COPILOT.md"
        updated = copath in copilot_changed
        entry = {
            "folder": folder,
            "files_changed": sorted(files),
            "copilot_path": copath,
            "copilot_changed": updated,
            "validation": None,
        }
        if not updated:
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
        status = "OK" if e["copilot_changed"] else "COPILOT.md NOT UPDATED"
        print(f"- {e['folder']}: {status}")

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
