#!/usr/bin/env python3
"""
NUnit 3 to NUnit 4 Converter

Converts NUnit 3 style assertions to NUnit 4 constraint model.
Also fixes NUnit 4 breaking changes like:
- .Within(message) -> proper message argument
- Assert.Fail with format strings -> interpolated strings
- Assert.That with format string params -> interpolated strings

Usage:
    python convert_nunit.py <folder_path|file_paths...>
    python convert_nunit.py Src
    python convert_nunit.py Src/Common/FwUtils/FwUtilsTests
"""
from __future__ import annotations

import sys
from pathlib import Path
from typing import List

from .nunit_parsing import replace_assert_invocations
from .nunit_converters import CONVERTERS
from .nunit_fixers import (
    fix_within_with_message,
    fix_assert_that_format_strings,
    fix_assert_that_wrong_argument_order,
)


def get_files_from_folder(folder: str) -> List[str]:
    """Recursively find all .cs files in a folder."""
    folder_path = Path(folder)
    if not folder_path.is_dir():
        print(f"Error: {folder} is not a valid directory")
        return []
    return [str(p) for p in folder_path.rglob("*.cs")]


def convert_content(content: str) -> str:
    """Apply all conversions to the content."""
    updated = content

    # First, fix .Within(message) patterns
    updated = fix_within_with_message(updated)

    # Then, fix Assert.That with format string params
    updated = fix_assert_that_format_strings(updated)

    # Fix Assert.That with wrong argument order (message before constraint)
    updated = fix_assert_that_wrong_argument_order(updated)

    # Finally, apply the classic Assert.* to Assert.That conversions
    for method, converter in CONVERTERS:
        updated = replace_assert_invocations(updated, method, converter)

    return updated


def print_help() -> None:
    """Print help message."""
    print("NUnit 3 to NUnit 4 Converter")
    print("=" * 50)
    print()
    print("Usage: python -m scripts.tests.convert_nunit [folder_path|file_paths...]")
    print()
    print("Description:")
    print("  Converts NUnit 3 style assertions to NUnit 4 constraint model.")
    print("  Also fixes NUnit 4 breaking changes like:")
    print("    - .Within(message) -> proper message argument")
    print("    - Assert.Fail with format strings -> interpolated strings")
    print("    - Assert.That with format string params -> interpolated strings")
    print("    - Assert.That wrong argument order (message, constraint) -> (constraint, message)")
    print()
    print("Arguments:")
    print("  folder_path  Path to folder containing C# test files to convert.")
    print("  file_paths   One or more specific file paths to convert.")
    print()
    print("Examples:")
    print("  python -m scripts.tests.convert_nunit Src")
    print("  python -m scripts.tests.convert_nunit Src/Common/FwUtils/FwUtilsTests")
    print("  python -m scripts.tests.convert_nunit Src/MyTests/Test1.cs")
    print()
    print("Converts:")
    print("  - Assert.AreEqual, IsTrue, IsNull, Contains, Greater, etc.")
    print("  - StringAssert.Contains, StartsWith, EndsWith, etc.")
    print("  - CollectionAssert.IsEmpty, Contains, AreEquivalent, etc.")
    print("  - FileAssert.AreEqual")
    print("  - .Within(String.Format(...)) -> interpolated message")
    print("  - .Within(\"message\") -> proper third argument")
    print("  - Assert.Fail(\"msg {0}\", arg) -> Assert.Fail($\"msg {arg}\")")
    print("  - Assert.That(x, \"msg\", Is.True) -> Assert.That(x, Is.True, \"msg\")")


def main() -> None:
    """Main entry point."""
    if len(sys.argv) > 1 and sys.argv[1] in ("-h", "--help"):
        print_help()
        return

    if len(sys.argv) < 2:
        print("Usage: python -m scripts.tests.convert_nunit <folder_path|file_paths...>")
        print("Run with --help for more information.")
        return

    # Check if first arg is a folder
    if len(sys.argv) == 2 and Path(sys.argv[1]).is_dir():
        folder = sys.argv[1]
        files_to_process = get_files_from_folder(folder)
        if not files_to_process:
            print("No .cs files found in the specified folder")
            return
    else:
        # Assume arguments are file paths
        files_to_process = sys.argv[1:]

    converted_count = 0
    for relative_path in files_to_process:
        path = Path(relative_path)
        if not path.exists():
            print(f"File not found: {relative_path}")
            continue

        try:
            original = path.read_text(encoding="utf-8")
        except UnicodeDecodeError:
            original = path.read_text(encoding="latin-1")

        converted = convert_content(original)
        if converted != original:
            path.write_text(converted, encoding="utf-8")
            print(f"[OK] Converted {relative_path}")
            converted_count += 1

    print(f"\nConverted {converted_count} file(s)")


if __name__ == "__main__":
    main()
