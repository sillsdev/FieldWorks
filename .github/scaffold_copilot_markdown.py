#!/usr/bin/env python3
"""
scaffold_copilot_markdown.py - Prepare or normalize COPILOT.md files to match the canonical structure.

Behavior:
- Detect impacted Src/<Folder> from git diff (same logic as detect_copilot_needed.py, or via --folders).
- For each folder:
  - Ensure frontmatter exists (status/last-reviewed/last-verified-commit).
  - Normalize ordering of canonical sections.
  - Insert placeholder sections when missing.
  - Regenerate "References (auto-generated hints)" with current filesystem hints.
- Never delete unknown sections; they are appended after canonical sections.

Usage examples:
  python .github/scaffold_copilot_markdown.py --base origin/release/9.3
  python .github/scaffold_copilot_markdown.py --folders Src/Common/Controls Src/Cellar
"""
from __future__ import annotations

import argparse
import datetime as dt
import os
import subprocess
from collections import OrderedDict
from pathlib import Path
from typing import Dict, Iterable, List, Optional, Tuple

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

AUTO_HINT_HEADING = "## References (auto-generated hints)"
PLACEHOLDER_TEXT = "TBD - populate from code. See auto-generated hints below."


def run(cmd: List[str], cwd: Optional[str] = None) -> str:
    return subprocess.check_output(cmd, cwd=cwd, stderr=subprocess.STDOUT).decode(
        "utf-8", errors="replace"
    )


def git_changed_files(
    root: Path,
    base: Optional[str] = None,
    head: str = "HEAD",
    since: Optional[str] = None,
) -> List[str]:
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


def top_level_src_folder(path: str) -> Optional[str]:
    parts = path.split("/")
    if len(parts) >= 2 and parts[0] == "Src":
        return "/".join(parts[:2])
    return None


def parse_frontmatter(text: str) -> Tuple[Optional[Dict[str, str]], str]:
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
        body = "\n".join(lines[end_idx + 1 :])
        return fm, body
    return None, text


def render_frontmatter(fm: Dict[str, str]) -> str:
    lines = ["---"]
    for key in ["last-reviewed", "last-verified-commit", "status"]:
        lines.append(f"{key}: {fm[key]}")
    lines.append("---")
    return "\n".join(lines) + "\n\n"


class CopilotDoc:
    def __init__(self, text: str, folder_name: str, commit: str, status: str = "draft"):
        self.folder_name = folder_name
        self.commit = commit
        self.status = status or "draft"

        fm, body = parse_frontmatter(text)
        if not fm:
            today = dt.date.today().strftime("%Y-%m-%d")
            fm = {
                "last-reviewed": today,
                "last-verified-commit": commit or "FIXME(set-sha)",
                "status": self.status,
            }
            body = text
        else:
            if not fm.get("last-reviewed"):
                fm["last-reviewed"] = dt.date.today().strftime("%Y-%m-%d")
            if not fm.get("last-verified-commit"):
                fm["last-verified-commit"] = commit or "FIXME(set-sha)"
            if not fm.get("status"):
                fm["status"] = self.status
        self.frontmatter = fm

        self.title, body_after_title = self._extract_title(body)
        self.sections = self._split_sections(body_after_title)

    def _extract_title(self, body: str) -> Tuple[str, str]:
        lines = body.splitlines()
        title = ""
        remainder_start = 0
        for idx, line in enumerate(lines):
            if line.startswith("# "):
                title = line.strip()
                remainder_start = idx + 1
                break
            if line.strip():
                # Ignore non-empty pre-title text but preserve remainder
                remainder_start = idx
        if not title:
            title = f"# {self.folder_name} COPILOT summary"
        remainder = "\n".join(lines[remainder_start:])
        return title, remainder

    def _split_sections(self, text: str) -> "OrderedDict[str, str]":
        sections: "OrderedDict[str, List[str]]" = OrderedDict()
        current_heading: Optional[str] = None
        buffer: List[str] = []
        lines = text.splitlines()
        for line in lines:
            if line.startswith("## "):
                if current_heading is not None:
                    sections[current_heading] = buffer
                current_heading = line[3:].strip()
                buffer = [line]
            else:
                if current_heading is None:
                    # Skip stray text before first heading
                    continue
                buffer.append(line)
        if current_heading is not None:
            sections[current_heading] = buffer
        return OrderedDict(
            (heading, "\n".join([l.rstrip() for l in lines]).rstrip() + "\n")
            for heading, lines in sections.items()
        )

    def _strip_auto_hints(self):
        if "References (auto-generated hints)" in self.sections:
            self.sections.pop("References (auto-generated hints)")

    def ensure_canonical_sections(self):
        self._strip_auto_hints()
        new_sections: "OrderedDict[str, str]" = OrderedDict()
        for heading in REQUIRED_HEADINGS:
            if heading in self.sections:
                new_sections[heading] = self.sections.pop(heading)
            else:
                new_sections[heading] = f"## {heading}\n{PLACEHOLDER_TEXT}\n\n"
        # Append any leftover, non-canonical sections
        for heading, content in self.sections.items():
            new_sections[heading] = content
        self.sections = new_sections

    def render(self, auto_hint_block: str) -> str:
        document = []
        document.append(render_frontmatter(self.frontmatter))
        document.append(f"{self.title}\n\n")
        for heading, content in self.sections.items():
            # Ensure each block ends with a blank line
            block = content.rstrip() + "\n\n"
            document.append(block)
            if heading == "References" and auto_hint_block:
                document.append(auto_hint_block)
        return "".join(document).rstrip() + "\n"


def list_files_for_references(
    folder: Path, limit_per_group: int = 25
) -> Dict[str, List[str]]:
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
                    rel = Path(dirpath, filename).relative_to(folder.parent.parent)
                    groups[group_name].append(str(rel).replace("\\", "/"))
                    break
    for key in groups:
        groups[key] = sorted(groups[key])[:limit_per_group]
    return groups


def format_auto_hint_block(groups: Dict[str, Iterable[str]]) -> str:
    lines: List[str] = [f"{AUTO_HINT_HEADING}"]
    for group_name, items in groups.items():
        items = list(items)
        if not items:
            continue
        lines.append(f"- {group_name}:")
        for item in items:
            lines.append(f"  - {item}")
    lines.append("")
    return "\n".join(lines)


def ensure_copilot_doc(folder: Path, commit: str, status: str) -> str:
    copath = folder / "COPILOT.md"
    if copath.exists():
        original = copath.read_text(encoding="utf-8", errors="replace")
    else:
        original = ""
    doc = CopilotDoc(original, folder.name, commit, status=status)
    doc.ensure_canonical_sections()
    auto_hint_block = format_auto_hint_block(list_files_for_references(folder))
    return doc.render(auto_hint_block)


def git_rev_parse(root: Path, ref: str = "HEAD", short: bool = True) -> str:
    try:
        args = ["git", "rev-parse"]
        if short:
            args.append("--short")
        args.append(ref)
        return run(args, cwd=str(root)).strip()
    except Exception:
        return ""


def main() -> int:
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

    if args.folders:
        folders = [Path(f.replace("\\", "/")) for f in args.folders]
    else:
        changed = git_changed_files(
            root, base=args.base, head=args.head, since=args.since
        )
        tops = set()
        for path in changed:
            if not path.startswith("Src/"):
                continue
            if path.endswith("/COPILOT.md"):
                continue
            parts = path.split("/")
            if len(parts) >= 2:
                tops.add("/".join(parts[:2]))
        folders = [root / t for t in sorted(tops)]

    commit = git_rev_parse(root, "HEAD", short=True)
    updated = 0
    for folder in folders:
        if not folder.exists():
            continue
        content = ensure_copilot_doc(folder, commit, args.status)
        copath = folder / "COPILOT.md"
        copath.write_text(content, encoding="utf-8")
        print(f"Prepared {copath}")
        updated += 1

    print(f"Prepared/updated {updated} COPILOT.md files.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
