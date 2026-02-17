#!/usr/bin/env python3
"""Shared markdown helpers for COPILOT documents."""
from __future__ import annotations

from pathlib import Path
from typing import Tuple

AUTO_START = "<!-- copilot:auto-change-log start -->"
AUTO_END = "<!-- copilot:auto-change-log end -->"
AUTO_PLACEHOLDER = """<!-- copilot:auto-change-log start -->\n## Change Log (auto)\n\nThis section is populated by running:\n1. `python .github/plan_copilot_updates.py --folders <Folder>`\n2. `python .github/copilot_apply_updates.py --folders <Folder>`\n\nDo not edit this block manually; rerun the scripts above after code or doc updates.\n<!-- copilot:auto-change-log end -->\n"""
AUTO_HINT_HEADING = "## References (auto-generated hints)"


def _split_frontmatter(text: str) -> Tuple[str, str]:
    if not text.startswith("---\n"):
        return "", text
    end = text.find("\n---", 3)
    if end == -1:
        return "", text
    end += len("\n---\n")
    return text[:end], text[end:]


def ensure_auto_change_log_block(text: str) -> Tuple[str, bool]:
    if AUTO_START in text and AUTO_END in text:
        return text, False
    front, body = _split_frontmatter(text)
    before = front.strip()
    after = body.strip()
    pieces = [part for part in (before, AUTO_PLACEHOLDER.strip(), after) if part]
    updated = "\n\n".join(pieces) + "\n"
    return updated, True


def remove_legacy_auto_hint(text: str) -> Tuple[str, bool]:
    if AUTO_HINT_HEADING not in text:
        return text, False
    start = text.find(AUTO_HINT_HEADING)
    if start == -1:
        return text, False
    after_heading = text[start:]
    end = after_heading.find("\n## ", len(AUTO_HINT_HEADING))
    if end == -1:
        trimmed = text[:start].rstrip()
        return (trimmed + "\n").rstrip() + "\n", True
    end_index = start + end
    updated = (text[:start].rstrip() + "\n\n" + text[end_index:].lstrip()).strip() + "\n"
    return updated, True


def split_after_auto_block(text: str) -> Tuple[str, str]:
    start = text.find(AUTO_START)
    if start == -1:
        return text, ""
    end = text.find(AUTO_END, start)
    if end == -1:
        return text, ""
    end += len(AUTO_END)
    newline_idx = text.find("\n", end)
    if newline_idx == -1:
        newline_idx = len(text)
    prefix = text[:newline_idx].rstrip() + "\n\n"
    suffix = text[newline_idx:].lstrip()
    return prefix, suffix


def load_template(path: Path) -> str:
    return path.read_text(encoding="utf-8").strip()
