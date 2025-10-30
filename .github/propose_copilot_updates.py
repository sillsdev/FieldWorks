#!/usr/bin/env python3
"""
propose_copilot_updates.py — Assist developers by preparing/patching COPILOT.md for changed folders.

Behavior:
- Detect impacted Src/<Folder> from git diff (same logic as detect_copilot_needed.py, or via --folders).
- For each folder:
  - Ensure frontmatter exists (status/last-reviewed/last-verified-commit).
  - Ensure required headings exist; add missing headings with brief TODO stubs.
  - Append a "References (auto-generated hints)" section listing key files (project files, key C# and C++ files, XML/XAML etc.) if not already present.
- Never delete existing content; append missing parts to the end.

Usage examples:
  python .github/propose_copilot_updates.py --base origin/release/9.3
  python .github/propose_copilot_updates.py --folders Src/Common/Controls Src/Cellar
"""
import argparse
import os
import subprocess
from pathlib import Path

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
        try:
            mb = run(["git", "merge-base", head, "origin/HEAD"], cwd=str(root)).strip()
            diff_range = f"{mb}..{head}"
        except Exception:
            diff_range = f"HEAD~1..{head}"
    out = run(["git", "diff", "--name-only", diff_range], cwd=str(root))
    return [l.strip().replace("\\", "/") for l in out.splitlines() if l.strip()]


def top_level_src_folder(path: str):
    parts = path.split("/")
    if len(parts) >= 2 and parts[0] == "Src":
        return "/".join(parts[:2])
    return None


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


def ensure_frontmatter(text: str, commit: str, status: str):
    fm, body = parse_frontmatter(text)
    import datetime as dt

    today = dt.date.today().strftime("%Y-%m-%d")
    changed = False
    if not fm:
        fm = {
            "last-reviewed": today,
            "last-verified-commit": commit or "FIXME(set-sha)",
            "status": status or "draft",
        }
        changed = True
    else:
        if not fm.get("last-reviewed"):
            fm["last-reviewed"] = today
            changed = True
        if not fm.get("last-verified-commit"):
            fm["last-verified-commit"] = commit or "FIXME(set-sha)"
            changed = True
        if not fm.get("status"):
            fm["status"] = status or "draft"
            changed = True

    def render_frontmatter(fm: dict) -> str:
        lines = ["---"]
        for k in ["last-reviewed", "last-verified-commit", "status"]:
            lines.append(f"{k}: {fm[k]}")
        lines.append("---")
        return "\n".join(lines) + "\n"

    if changed:
        return render_frontmatter(fm) + body, True
    # Reconstruct original
    return text, False


def ensure_headings(text: str):
    present = set()
    for line in text.splitlines():
        if line.startswith("## "):
            present.add(line[3:].strip())
    missing = [h for h in REQUIRED_HEADINGS if h not in present]
    if not missing:
        return text, []
    # Append missing sections at the end with brief stubs
    parts = [text.rstrip(), ""]
    for h in missing:
        parts.append(f"## {h}")
        parts.append("TBD — populate from code. See auto-generated hints below.")
        parts.append("")
    return "\n".join(parts), missing


def list_files_for_references(folder: Path, limit_per_group=25):
    groups = {k: [] for k in FILE_GROUPS}
    for dirpath, dirnames, filenames in os.walk(folder):
        # Skip deep bin/obj like folders
        skip = {"obj", "bin", "packages", "Output", "Downloads"}
        if any(p.lower() in skip for p in Path(dirpath).parts):
            continue
        for f in filenames:
            ext = os.path.splitext(f)[1].lower()
            for gname, exts in FILE_GROUPS.items():
                if ext in exts:
                    rel = str(
                        Path(dirpath, f).relative_to(folder.parent.parent)
                    )  # relative to repo root
                    groups[gname].append(rel)
                    break
    # Trim and sort each group
    for k in groups:
        groups[k] = sorted(groups[k])[:limit_per_group]
    return groups


def append_auto_refs(text: str, folder: Path):
    # If a section titled exactly '## References (auto-generated hints)' exists, replace it
    marker = "## References (auto-generated hints)"
    groups = list_files_for_references(folder)
    lines = text.splitlines()
    start = end = -1
    for i, l in enumerate(lines):
        if l.strip() == marker:
            start = i
            # find next h2 or end
            for j in range(i + 1, len(lines)):
                if lines[j].startswith("## "):
                    end = j
                    break
            if end == -1:
                end = len(lines)
            break
    block = [marker]
    for gname, items in groups.items():
        if not items:
            continue
        block.append(f"- {gname}:")
        for it in items:
            block.append(f"  - {it}")
    block.append("")
    if start != -1:
        new_lines = lines[:start] + block + lines[end:]
    else:
        new_lines = lines + [""] + block
    return "\n".join(new_lines)


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


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--root", default=str(Path.cwd()))
    ap.add_argument("--base", default=None)
    ap.add_argument("--head", default="HEAD")
    ap.add_argument("--since", default=None)
    ap.add_argument(
        "--folders",
        nargs="*",
        default=None,
        help="Explicit Src/<Folder> paths to update",
    )
    ap.add_argument("--status", default="draft")
    args = ap.parse_args()

    root = Path(args.root).resolve()

    folders = []
    if args.folders:
        folders = [Path(f.replace("\\", "/")) for f in args.folders]
    else:
        changed = git_changed_files(
            root, base=args.base, head=args.head, since=args.since
        )
        tops = set()
        for p in changed:
            if not p.startswith("Src/"):
                continue
            if p.endswith("/COPILOT.md"):
                continue
            parts = p.split("/")
            if len(parts) >= 2:
                tops.add("/".join(parts[:2]))
        folders = [Path(root / t) for t in sorted(tops)]

    commit = git_rev_parse(root, "HEAD", short=True)
    updated = 0
    for folder in folders:
        copath = folder / "COPILOT.md"
        if not copath.exists():
            # Start a new file with the canonical heading sections
            content = (
                "---\n"
                f"last-reviewed: TBD\n"
                f"last-verified-commit: {commit}\n"
                f"status: {args.status}\n"
                "---\n\n"
                f"# {folder.name} COPILOT summary\n\n"
                "## Purpose\nTBD — summarize purpose grounded in code.\n\n"
                + "\n".join([f"## {h}\n" for h in REQUIRED_HEADINGS if h != "Purpose"])
                + "\n"
            )
        else:
            content = copath.read_text(encoding="utf-8", errors="replace")
            content, _ = ensure_frontmatter(content, commit, args.status)
            content, missing = ensure_headings(content)
        # Append auto-generated references
        content = append_auto_refs(content, folder)
        copath.write_text(content, encoding="utf-8")
        print(f"Prepared {copath}")
        updated += 1

    print(f"Prepared/updated {updated} COPILOT.md files.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
