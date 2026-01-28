#!/usr/bin/env python3
"""
check_copilot_docs.py — Validate Src/**/AGENTS.md against the canonical skeleton

Checks:
- Frontmatter: last-reviewed, last-reviewed-tree, status
- last-reviewed-tree not FIXME and matches the current git tree hash
- Required headings present
- References entries appear to map to real files in repo (best-effort)

Usage:
    python .github/check_copilot_docs.py [--root <repo-root>] [--fail] [--json <out>] [--verbose]
                                                                            [--only-changed] [--base <ref>] [--head <ref>] [--since <ref>]

Exit codes:
  0 = no issues
  1 = warnings (non-fatal) and no --fail provided
  2 = failures when --fail provided
"""
import argparse
import json
import os
import re
import sys
import subprocess
from pathlib import Path

from copilot_tree_hash import compute_folder_tree_hash

REQUIRED_HEADINGS = [
    "Purpose",
    "Architecture",
    "Key Components",
    "Technology Stack",
    "Dependencies",
    "Interop & Contracts",
    "Threading & Performance",
    "Config & Feature Flags",
    "Build Information",
    "Interfaces and Data Models",
    "Entry Points",
    "Test Index",
    "Usage Hints",
    "Related Folders",
    "References",
]

ORGANIZATIONAL_REQUIRED_HEADINGS = [
    "Purpose",
    "Subfolder Map",
    "When Updating This Folder",
    "Related Guidance",
]

REFERENCE_EXTS = {
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

PLACEHOLDER_PREFIXES = ("tbd",)


def find_repo_root(start: Path) -> Path:
    p = start.resolve()
    while p != p.parent:
        if (p / ".git").exists():
            return p
        p = p.parent
    return start.resolve()


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
        # Ensure origin/ prefix if a bare branch name is provided
        if not base.startswith("origin/") and "/" not in base:
            base = f"origin/{base}"
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


def index_repo_files(root: Path):
    index = {}
    for dirpath, dirnames, filenames in os.walk(root):
        # Skip some big or irrelevant directories
        rel = Path(dirpath).relative_to(root)
        parts = rel.parts
        if parts and parts[0] in {
            ".git",
            "packages",
            "Obj",
            "Output",
            "Downloads",
        }:
            continue
        for f in filenames:
            index.setdefault(f, []).append(os.path.join(dirpath, f))
    return index


def parse_frontmatter(text: str):
    lines = text.splitlines()
    fm = {}
    if len(lines) >= 3 and lines[0].strip() == "---":
        # Find closing '---'
        try:
            end_idx = lines[1:].index("---") + 1
        except ValueError:
            # Not properly closed; try to find a line that is just '---'
            end_idx = -1
            for i in range(1, min(len(lines), 100)):
                if lines[i].strip() == "---":
                    end_idx = i
                    break
            if end_idx == -1:
                return None, text
        fm_lines = lines[1:end_idx]
        body = "\n".join(lines[end_idx + 1 :])
        for l in fm_lines:
            l = l.strip()
            if not l or l.startswith("#"):
                continue
            if ":" in l:
                k, v = l.split(":", 1)
                fm[k.strip()] = v.strip().strip('"')
        return fm, body
    return None, text


def split_sections(text: str):
    sections = {}
    current = None
    buffer = []
    for line in text.splitlines():
        if line.startswith("## "):
            if current is not None:
                sections[current] = "\n".join(buffer).strip()
            current = line[3:].strip()
            buffer = []
        else:
            if current is not None:
                buffer.append(line)
    if current is not None:
        sections[current] = "\n".join(buffer).strip()
    return sections


def is_organizational_doc(sections):
    return (
        "Subfolder Map" in sections
        and "When Updating This Folder" in sections
        and "Related Guidance" in sections
    )


def extract_references(reference_section: str):
    refs = []
    for line in reference_section.splitlines():
        for token in re.split(r"[\s,()]+", line):
            if any(token.endswith(ext) for ext in REFERENCE_EXTS):
                token = token.rstrip(".,;:")
                refs.append(token)
    return list(dict.fromkeys(refs))


def maybe_placeholder(text: str) -> bool:
    stripped = text.strip()
    if not stripped:
        return True
    lowered = stripped.lower()
    return any(lowered.startswith(prefix) for prefix in PLACEHOLDER_PREFIXES)


def validate_file(path: Path, repo_index: dict, verbose=False):
    result = {
        "path": str(path),
        "frontmatter": {
            "missing": [],
            "tree_missing": False,
            "tree_placeholder": False,
            "tree_value": "",
        },
        "headings_missing": [],
        "references_missing": [],
        "empty_sections": [],
        "warnings": [],
        "tree_mismatch": False,
        "current_tree": "",
        "ok": True,
    }
    text = path.read_text(encoding="utf-8", errors="replace")
    fm, body = parse_frontmatter(text)
    if not fm:
        result["frontmatter"]["missing"] = [
            "last-reviewed",
            "last-reviewed-tree",
            "status",
        ]
        result["frontmatter"]["tree_missing"] = True
        result["ok"] = False
    else:
        for key in ["last-reviewed", "last-reviewed-tree", "status"]:
            if key not in fm or not fm[key]:
                result["frontmatter"]["missing"].append(key)
        tree_value = fm.get("last-reviewed-tree", "")
        result["frontmatter"]["tree_value"] = tree_value
        if not tree_value:
            result["frontmatter"]["tree_missing"] = True
            result["ok"] = False
        elif tree_value.startswith("FIXME"):
            result["frontmatter"]["tree_placeholder"] = True
            result["ok"] = False
            result["warnings"].append(
                "last-reviewed-tree placeholder; regenerate frontmatter"
            )
        if fm.get("last-verified-commit"):
            result["warnings"].append(
                "legacy last-verified-commit entry detected; rerun scaffolder"
            )
        if result["frontmatter"]["missing"]:
            result["ok"] = False

    sections = split_sections(body)
    organizational = is_organizational_doc(sections)
    required_headings = (
        ORGANIZATIONAL_REQUIRED_HEADINGS if organizational else REQUIRED_HEADINGS
    )

    for h in required_headings:
        if h not in sections:
            result["headings_missing"].append(h)
    if result["headings_missing"]:
        result["ok"] = False

    for h in required_headings:
        if h in sections:
            if maybe_placeholder(sections[h]):
                result["empty_sections"].append(h)
    if result["empty_sections"]:
        for h in result["empty_sections"]:
            result["warnings"].append(f"Section '{h}' is empty or placeholder text")

    refs = []
    if not organizational:
        refs = extract_references(sections.get("References", ""))
        for r in refs:
            base = os.path.basename(r)
            if base not in repo_index:
                result["references_missing"].append(r)
        # references_missing doesn't necessarily fail; treat as warning unless all missing
        if refs and len(result["references_missing"]) == len(refs):
            result["ok"] = False

    if verbose:
        print(f"Checked {path}")
    return result


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--root", default=str(find_repo_root(Path.cwd())))
    ap.add_argument("--fail", action="store_true", help="Exit non-zero on failures")
    ap.add_argument(
        "--json", dest="json_out", default=None, help="Write JSON report to file"
    )
    ap.add_argument("--verbose", action="store_true")
    ap.add_argument(
        "--only-changed",
        action="store_true",
        help="Validate only changed AGENTS.md files",
    )
    ap.add_argument(
        "--paths",
        nargs="*",
        help="Specific AGENTS.md paths to validate (relative to repo root)",
    )
    ap.add_argument(
        "--base",
        default=None,
        help="Base git ref (e.g., origin/<branch> or branch name)",
    )
    ap.add_argument("--head", default="HEAD", help="Head ref (default HEAD)")
    ap.add_argument(
        "--since", default=None, help="Alternative to base/head: since this ref"
    )
    args = ap.parse_args()

    root = Path(args.root).resolve()
    src = root / "Src"
    if not src.exists():
        print(f"ERROR: Src/ not found under {root}")
        return 2

    repo_index = index_repo_files(root)

    paths_to_check = []
    if args.paths:
        for rel in args.paths:
            candidate = Path(rel)
            if not candidate.is_absolute():
                candidate = root / rel
            paths_to_check.append(candidate)
    elif args.only_changed:
        changed = git_changed_files(
            root, base=args.base, head=args.head, since=args.since
        )
        for p in changed:
            if p.endswith("/AGENTS.md") and p.startswith("Src/"):
                paths_to_check.append(root / p)
    if not paths_to_check:
        paths_to_check = list(src.rglob("AGENTS.md"))

    results = []
    for copath in paths_to_check:
        result = validate_file(copath, repo_index, verbose=args.verbose)
        rel_parts = Path(result["path"]).relative_to(root).parts
        folder_key = "/".join(rel_parts[:-1])
        result["folder"] = folder_key
        results.append(result)

    for r in results:
        folder_key = r.get("folder")
        folder_path = root / folder_key if folder_key else None
        if not folder_path or not folder_path.exists():
            r["warnings"].append(
                "Folder missing for tree hash computation; verify path"
            )
            r["ok"] = False
            continue
        try:
            current_hash = compute_folder_tree_hash(root, folder_path, ref=args.head)
            r["current_tree"] = current_hash
        except Exception as exc:
            r["warnings"].append(f"Unable to compute tree hash: {exc}")
            r["ok"] = False
            continue

        tree_value = r["frontmatter"].get("tree_value", "")
        if tree_value and not tree_value.startswith("FIXME"):
            if current_hash != tree_value:
                r["tree_mismatch"] = True
                r["warnings"].append(
                    "last-reviewed-tree mismatch with current folder state"
                )
                r["ok"] = False

    failures = [r for r in results if not r["ok"]]
    print(f"Checked {len(results)} AGENTS.md files. Failures: {len(failures)}")
    for r in failures:
        print(f"- {r['path']}")
        if r["frontmatter"]["missing"]:
            print(f"  frontmatter missing: {', '.join(r['frontmatter']['missing'])}")
        if r["frontmatter"].get("tree_missing"):
            print("  last-reviewed-tree missing")
        if r["frontmatter"].get("tree_placeholder"):
            print("  last-reviewed-tree placeholder; update via scaffolder")
        if r.get("tree_mismatch"):
            print("  last-reviewed-tree does not match current folder hash")
        if r["headings_missing"]:
            print(f"  headings missing: {', '.join(r['headings_missing'])}")
        if r["references_missing"]:
            print(
                f"  unresolved references: {', '.join(r['references_missing'][:10])}{' …' if len(r['references_missing'])>10 else ''}"
            )
    warnings = [r for r in results if r["warnings"]]
    for r in warnings:
        print(f"- WARN {r['path']}: { '; '.join(r['warnings']) }")

    if args.json_out:
        with open(args.json_out, "w", encoding="utf-8") as f:
            json.dump(results, f, indent=2)

    if args.fail and failures:
        return 2
    return 0 if not failures else 1


if __name__ == "__main__":
    sys.exit(main())

