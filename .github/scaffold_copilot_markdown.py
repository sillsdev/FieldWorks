#!/usr/bin/env python3
"""Normalize AGENTS.md files so Copilot agents receive predictable structure.

Key behaviors (aligned with the detect → plan → draft workflow):

* Detect impacted `Src/<Folder>` entries from git diff, or honor `--folders` / `--all`.
* Ensure the YAML frontmatter includes `last-reviewed`, `last-reviewed-tree`, and `status`,
    refreshing the tree hash with the latest git data.
* Create and order the canonical section list so humans keep narrative content short.
* Drop legacy "References (auto-generated hints)" blocks and insert the fenced
    `<!-- copilot:auto-change-log -->` placeholder when missing.
* Leave any unknown/custom sections intact by appending them after the canonical list.

Usage examples:

```powershell
python .github/scaffold_copilot_markdown.py --base origin/release/9.3
python .github/scaffold_copilot_markdown.py --folders Src/Common/Controls Src/Cellar
python .github/scaffold_copilot_markdown.py --all
```
"""
from __future__ import annotations

import argparse
import subprocess
from collections import OrderedDict
from pathlib import Path
from typing import Dict, Iterable, List, Optional, Tuple

import datetime as dt

from copilot_doc_utils import ensure_auto_change_log_block, remove_legacy_auto_hint
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

PLACEHOLDER_TEXT = "TBD - populate from code. Use planner output for context."


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
    for key in ["last-reviewed", "last-reviewed-tree", "status"]:
        value = fm.get(key, "")
        lines.append(f"{key}: {value}")
    lines.append("---")
    return "\n".join(lines) + "\n\n"


class CopilotDoc:
    def __init__(
        self,
        text: str,
        folder_name: str,
        tree_hash: Optional[str],
        status: str = "draft",
    ):
        self.folder_name = folder_name
        self.status = status or "draft"
        today = dt.date.today().strftime("%Y-%m-%d")
        self.tree_hash = tree_hash or "FIXME(set-tree-hash)"

        fm, body = parse_frontmatter(text)
        if not fm:
            fm = {
                "last-reviewed": today,
                "last-reviewed-tree": self.tree_hash,
                "status": self.status,
            }
            body = text
        else:
            if fm.pop("last-verified-commit", None) is not None:
                fm.setdefault("last-reviewed-tree", self.tree_hash)
            if not fm.get("last-reviewed"):
                fm["last-reviewed"] = today
            if not fm.get("last-reviewed-tree"):
                fm["last-reviewed-tree"] = self.tree_hash
            else:
                # Always overwrite with the latest computed hash when available.
                if tree_hash:
                    fm["last-reviewed-tree"] = tree_hash
            if not fm.get("status"):
                fm["status"] = self.status
        self.frontmatter = {
            "last-reviewed": fm.get("last-reviewed", today),
            "last-reviewed-tree": fm.get("last-reviewed-tree", self.tree_hash),
            "status": fm.get("status", self.status),
        }

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

    def ensure_canonical_sections(self):
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

    def render(self) -> str:
        document = []
        document.append(render_frontmatter(self.frontmatter))
        document.append(f"{self.title}\n\n")
        for heading, content in self.sections.items():
            # Ensure each block ends with a blank line
            block = content.rstrip() + "\n\n"
            document.append(block)
        return "".join(document).rstrip() + "\n"


def ensure_copilot_doc(
    root: Path, folder: Path, status: str, ref: str
) -> Tuple[str, Optional[str]]:
    copath = folder / "AGENTS.md"
    if copath.exists():
        original = copath.read_text(encoding="utf-8", errors="replace")
    else:
        original = ""
    try:
        tree_hash = compute_folder_tree_hash(root, folder, ref=ref)
    except Exception as exc:  # pragma: no cover - diagnostics only
        print(f"WARNING: Unable to compute tree hash for {folder}: {exc}")
        tree_hash = None
    doc = CopilotDoc(original, folder.name, tree_hash, status=status)
    doc.ensure_canonical_sections()
    rendered = doc.render()
    rendered, _ = remove_legacy_auto_hint(rendered)
    rendered, _ = ensure_auto_change_log_block(rendered)
    return rendered, tree_hash


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
    ap.add_argument(
        "--ref",
        default=None,
        help="Git ref used when computing folder tree hashes (default: head ref)",
    )
    ap.add_argument(
        "--all",
        action="store_true",
        help="Normalize every AGENTS.md under Src/ regardless of git changes",
    )
    args = ap.parse_args()

    root = Path(args.root).resolve()

    if args.folders:
        folders = []
        for f in args.folders:
            candidate = Path(f.replace("\\", "/"))
            if not candidate.is_absolute():
                candidate = (root / candidate).resolve()
            folders.append(candidate)
    else:
        if args.all:
            folders = sorted((path.parent for path in root.glob("Src/**/AGENTS.md")))
        else:
            changed = git_changed_files(
                root, base=args.base, head=args.head, since=args.since
            )
            tops = set()
            for path in changed:
                if not path.startswith("Src/"):
                    continue
                if path.endswith("/AGENTS.md"):
                    continue
                parts = path.split("/")
                if len(parts) >= 2:
                    tops.add("/".join(parts[:2]))
            folders = [root / t for t in sorted(tops)]

    ref = args.ref or args.head
    updated = 0
    for folder in folders:
        if not folder.exists():
            continue
        content, tree_hash = ensure_copilot_doc(root, folder, args.status, ref)
        copath = folder / "AGENTS.md"
        copath.write_text(content, encoding="utf-8")
        suffix = f" (tree {tree_hash})" if tree_hash else ""
        print(f"Prepared {copath}{suffix}")
        updated += 1

    print(f"Prepared/updated {updated} AGENTS.md files.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())

