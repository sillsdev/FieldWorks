#!/usr/bin/env python3
"""
Compare C# files ignoring formatting differences.

This script normalizes C# code by:
1. Removing all whitespace differences (spaces, tabs, newlines)
2. Removing BOM markers
3. Normalizing line endings
4. Collapsing multiple spaces to single space

Then compares the normalized versions to find files with actual semantic changes.

It also identifies whether differences are conversion-related (Assert pattern changes)
or non-conversion changes (actual test logic changes).
"""

import re
import subprocess
import sys
from pathlib import Path


# Patterns that indicate NUnit conversion changes
CONVERSION_PATTERNS = [
    r'Assert\.That\([^,]+,\s*Is\.EqualTo',
    r'Assert\.That\([^,]+,\s*Is\.Not\.EqualTo',
    r'Assert\.That\([^,]+,\s*Is\.GreaterThan',
    r'Assert\.That\([^,]+,\s*Is\.LessThan',
    r'Assert\.That\([^,]+,\s*Is\.GreaterThanOrEqualTo',
    r'Assert\.That\([^,]+,\s*Is\.LessThanOrEqualTo',
    r'Assert\.That\([^,]+,\s*Is\.True',
    r'Assert\.That\([^,]+,\s*Is\.False',
    r'Assert\.That\([^,]+,\s*Is\.Null',
    r'Assert\.That\([^,]+,\s*Is\.Not\.Null',
    r'Assert\.That\([^,]+,\s*Is\.Empty',
    r'Assert\.That\([^,]+,\s*Contains\.Substring',
    r'Assert\.That\([^,]+,\s*Does\.Contain',
    r'Assert\.That\([^,]+,\s*Has\.Count',
]


def normalize_csharp(content: str) -> str:
    """Normalize C# code for comparison, ignoring formatting."""
    # Remove BOM
    content = content.lstrip('\ufeff')

    # Normalize line endings
    content = content.replace('\r\n', '\n').replace('\r', '\n')

    # Remove single-line comments (but preserve their existence for semantic comparison)
    # Actually, let's keep comments as they might be semantically important

    # Collapse all whitespace (spaces, tabs, newlines) to single space
    content = re.sub(r'\s+', ' ', content)

    # Remove spaces around common operators/punctuation
    content = re.sub(r'\s*([{}\[\]();,<>])\s*', r'\1', content)
    content = re.sub(r'\s*([=+\-*/&|!<>])\s*', r'\1', content)

    # Normalize multiple spaces to one
    content = re.sub(r' +', ' ', content)

    return content.strip()


def get_file_from_head(filepath: str) -> str:
    """Get file content from HEAD."""
    try:
        result = subprocess.run(
            ['git', 'show', f'HEAD:{filepath}'],
            capture_output=True,
            text=True,
            encoding='utf-8',
            errors='replace'
        )
        return result.stdout if result.returncode == 0 else ""
    except Exception:
        return ""


def get_working_file(filepath: str) -> str:
    """Get file content from working directory."""
    try:
        path = Path(filepath)
        if path.exists():
            return path.read_text(encoding='utf-8', errors='replace')
    except Exception:
        pass
    return ""


def compare_files(filepath: str) -> dict:
    """Compare a file between working dir and HEAD, ignoring formatting."""
    head_content = get_file_from_head(filepath)
    work_content = get_working_file(filepath)

    if not head_content and not work_content:
        return {"status": "missing", "has_semantic_diff": False}

    if not head_content:
        return {"status": "new_in_working", "has_semantic_diff": True}

    if not work_content:
        return {"status": "deleted", "has_semantic_diff": True}

    head_normalized = normalize_csharp(head_content)
    work_normalized = normalize_csharp(work_content)

    if head_normalized == work_normalized:
        return {"status": "format_only", "has_semantic_diff": False}
    else:
        # Find the actual differences
        return {
            "status": "semantic_diff",
            "has_semantic_diff": True,
            "head_len": len(head_normalized),
            "work_len": len(work_normalized),
            "diff_chars": abs(len(head_normalized) - len(work_normalized))
        }


def main():
    # Get list of changed files from git
    result = subprocess.run(
        ['git', 'diff', '--name-only', 'HEAD', '--', 'Src/**/*Tests*.cs', 'Lib/**/*Tests*.cs'],
        capture_output=True,
        text=True
    )

    files = [f.strip() for f in result.stdout.strip().split('\n') if f.strip()]

    # Filter out AssemblyInfo files
    files = [f for f in files if 'AssemblyInfo.cs' not in f]

    print(f"Analyzing {len(files)} files...")
    print()

    format_only = []
    semantic_diff = []

    for filepath in files:
        result = compare_files(filepath)
        if result["has_semantic_diff"]:
            semantic_diff.append((filepath, result))
        else:
            format_only.append(filepath)

    print(f"=== Files with FORMAT-ONLY differences ({len(format_only)}): ===")
    for f in format_only:
        print(f"  {f}")

    print()
    print(f"=== Files with SEMANTIC differences ({len(semantic_diff)}): ===")
    for f, info in semantic_diff:
        extra = ""
        if "diff_chars" in info:
            extra = f" (delta: {info['diff_chars']} chars)"
        print(f"  {f}{extra}")

    return len(semantic_diff)


if __name__ == "__main__":
    sys.exit(main())
