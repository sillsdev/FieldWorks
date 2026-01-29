#!/usr/bin/env python3
"""
RhinoMocks to Moq Migration Script

This script performs regex-based transformations to migrate common RhinoMocks
patterns to Moq equivalents. It handles the most common patterns but may require
manual review for complex cases.

Usage:
    python migrate_rhinomocks_to_moq.py <file_or_directory> [--dry-run]

Common Patterns Migrated:
    - MockRepository.GenerateStub<T>() → new Mock<T>()
    - MockRepository.GenerateMock<T>() → new Mock<T>()
    - .Stub(x => x.Method()).Return(value) → .Setup(x => x.Method()).Returns(value)
    - .Expect(x => x.Method()).Return(value) → .Setup(x => x.Method()).Returns(value)
    - Arg<T>.Is.Anything → It.IsAny<T>()
    - Arg<T>.Is.Equal(value) → It.Is<T>(x => x == value) or just 'value'
    - .VerifyAllExpectations() → .Verify() or .VerifyAll()
    - using Rhino.Mocks → using Moq
"""

import re
import sys
import os
from pathlib import Path
from typing import List, Tuple

# Regex patterns for RhinoMocks → Moq transformations
TRANSFORMATIONS: List[Tuple[str, str, str]] = [
    # Using statements
    (r'using\s+Rhino\.Mocks\s*;', 'using Moq;', 'Replace Rhino.Mocks using with Moq'),
    (r'using\s+Rhino\.Mocks\.Constraints\s*;', '// Removed: Rhino.Mocks.Constraints (use Moq It.Is<T>)', 'Remove Rhino.Mocks.Constraints'),

    # MockRepository patterns - with arguments
    (r'MockRepository\.GenerateStub<([^>]+)>\s*\(\s*([^)]+)\s*\)', r'new Mock<\1>(\2).Object', 'GenerateStub<T>(args) → new Mock<T>(args).Object'),
    (r'MockRepository\.GenerateMock<([^>]+)>\s*\(\s*([^)]+)\s*\)', r'new Mock<\1>(\2).Object', 'GenerateMock<T>(args) → new Mock<T>(args).Object'),
    # MockRepository patterns - no arguments (must come after patterns with args)
    (r'MockRepository\.GenerateStub<([^>]+)>\s*\(\s*\)', r'new Mock<\1>().Object', 'GenerateStub<T>() → new Mock<T>().Object'),
    (r'MockRepository\.GenerateMock<([^>]+)>\s*\(\s*\)', r'new Mock<\1>().Object', 'GenerateMock<T>() → new Mock<T>().Object'),
    (r'MockRepository\.GeneratePartialMock<([^>]+)>\s*\(([^)]*)\)', r'new Mock<\1>(\2) { CallBase = true }.Object', 'GeneratePartialMock<T>() → new Mock<T>() { CallBase = true }.Object'),

    # Stub/Expect with Return - simple cases
    # Note: This is tricky because RhinoMocks uses .Return() but Moq uses .Returns()
    (r'\.Stub\(([^)]+)\)\.Return\(', r'.Setup(\1).Returns(', '.Stub().Return() → .Setup().Returns()'),
    (r'\.Expect\(([^)]+)\)\.Return\(', r'.Setup(\1).Returns(', '.Expect().Return() → .Setup().Returns()'),

    # Expect with WhenCalled and Repeat (complex RhinoMocks pattern)
    # Pattern: .Expect(x => x.Method()).WhenCalled(a => { }).Repeat.Once();
    (r'\.Expect\(([^)]+)\)\.WhenCalled\(([^)]+)\)\.Repeat\.Once\(\)', r'.Setup(\1).Callback(\2).Verifiable()', '.Expect().WhenCalled().Repeat.Once() → .Setup().Callback().Verifiable()'),
    (r'\.Expect\(([^)]+)\)\.WhenCalled\(([^)]+)\)\.Repeat\.Times\((\d+)\)', r'.Setup(\1).Callback(\2).Verifiable()', '.Expect().WhenCalled().Repeat.Times(n) → .Setup().Callback().Verifiable()'),

    # Stub without Return (void methods)
    (r'\.Stub\(([^)]+)\)\s*;', r'.Setup(\1);', '.Stub() → .Setup() for void methods'),

    # Arg constraints
    (r'Arg<([^>]+)>\.Is\.Anything', r'It.IsAny<\1>()', 'Arg<T>.Is.Anything → It.IsAny<T>()'),
    (r'Arg<([^>]+)>\.Is\.Equal\(([^)]+)\)', r'\2', 'Arg<T>.Is.Equal(value) → value (Moq matches exact values)'),
    (r'Arg<([^>]+)>\.Is\.NotNull', r'It.Is<\1>(x => x != null)', 'Arg<T>.Is.NotNull → It.Is<T>(x => x != null)'),
    (r'Arg<([^>]+)>\.Is\.Null', r'It.Is<\1>(x => x == null)', 'Arg<T>.Is.Null → It.Is<T>(x => x == null)'),
    (r'Arg<([^>]+)>\.Is\.Same\(([^)]+)\)', r'It.Is<\1>(x => ReferenceEquals(x, \2))', 'Arg<T>.Is.Same(obj) → It.Is<T>(x => ReferenceEquals(x, obj))'),
    (r'Arg<([^>]+)>\.Matches\(([^)]+)\)', r'It.Is<\1>(\2)', 'Arg<T>.Matches(predicate) → It.Is<T>(predicate)'),

    # Verification
    (r'\.VerifyAllExpectations\(\)', '.VerifyAll()', '.VerifyAllExpectations() → .VerifyAll()'),
    (r'\.AssertWasCalled\(([^)]+)\)', r'.Verify(\1)', '.AssertWasCalled() → .Verify()'),
    (r'\.AssertWasNotCalled\(([^)]+)\)', r'.Verify(\1, Times.Never())', '.AssertWasNotCalled() → .Verify(, Times.Never())'),

    # Property stubs
    (r'\.Stub\(([^=]+)=>([^.]+)\.([A-Za-z_][A-Za-z0-9_]*)\)\.Return\(', r'.Setup(\1=>\2.\3).Returns(', 'Property stub → Setup'),

    # Throw patterns
    (r'\.Throw\(([^)]+)\)', r'.Throws(\1)', '.Throw() → .Throws()'),

    # Do patterns (callbacks) - Only match RhinoMocks .Do() which follows .Stub()/.Expect()
    # Don't match LINQ .Do() or other uses
    # (r'\.Do\(([^)]+)\)', r'.Callback(\1)', '.Do() → .Callback()'),  # DISABLED - too many false positives

    # WhenCalled patterns (standalone)
    (r'\.WhenCalled\(([^)]+)\)', r'.Callback(\1)', '.WhenCalled() → .Callback()'),

    # Repeat patterns (standalone - remove them as Moq doesn't need these)
    (r'\.Repeat\.Any\(\)', '', '.Repeat.Any() → (removed, Moq default is any)'),
    (r'\.Repeat\.Once\(\)', '.Verifiable()', '.Repeat.Once() → .Verifiable()'),
    (r'\.Repeat\.Times\((\d+)\)', '.Verifiable()', '.Repeat.Times(n) → .Verifiable()'),
]

# Patterns that need manual review (we'll flag but not auto-convert)
MANUAL_REVIEW_PATTERNS = [
    (r'\.GetArgumentsForCallsMadeOn\(', 'GetArgumentsForCallsMadeOn needs manual migration to Moq Callback capture pattern'),
    (r'\.BackToRecord\(', 'BackToRecord() needs manual review - Moq uses different paradigm'),
    (r'\.Replay\(', 'Replay() needs manual review - Moq uses different paradigm'),
    (r'\.Ordered\(', 'Ordered() needs manual review - use Moq.Sequences or MockSequence'),
    (r'MockRepository\s+\w+\s*=\s*new\s+MockRepository', 'MockRepository instance creation needs manual migration'),
]


def transform_file(filepath: Path, dry_run: bool = False) -> Tuple[bool, List[str]]:
    """
    Transform a single file from RhinoMocks to Moq patterns.

    Returns:
        Tuple of (was_modified, list_of_changes)
    """
    try:
        content = filepath.read_text(encoding='utf-8')
    except Exception as e:
        return False, [f"Error reading file: {e}"]

    original_content = content
    changes = []

    # Check for manual review patterns first
    for pattern, message in MANUAL_REVIEW_PATTERNS:
        if re.search(pattern, content):
            changes.append(f"[!] MANUAL REVIEW: {message}")

    # Apply transformations
    for pattern, replacement, description in TRANSFORMATIONS:
        new_content, count = re.subn(pattern, replacement, content)
        if count > 0:
            changes.append(f"[+] {description}: {count} occurrence(s)")
            content = new_content

    was_modified = content != original_content

    if was_modified and not dry_run:
        try:
            filepath.write_text(content, encoding='utf-8')
            changes.append(f"[+] File saved: {filepath}")
        except Exception as e:
            changes.append(f"[X] Error saving file: {e}")
            return False, changes
    elif was_modified and dry_run:
        changes.append(f"[DRY RUN] Would modify: {filepath}")

    return was_modified, changes


def find_cs_files(path: Path) -> List[Path]:
    """Find all C# files in a directory recursively."""
    if path.is_file():
        return [path] if path.suffix == '.cs' else []
    return list(path.rglob('*.cs'))


def main():
    if len(sys.argv) < 2:
        print(__doc__)
        sys.exit(1)

    path = Path(sys.argv[1])
    dry_run = '--dry-run' in sys.argv

    if not path.exists():
        print(f"Error: Path does not exist: {path}")
        sys.exit(1)

    files = find_cs_files(path)
    if not files:
        print(f"No C# files found in: {path}")
        sys.exit(0)

    print(f"{'[DRY RUN] ' if dry_run else ''}Processing {len(files)} C# file(s)...")
    print("=" * 60)

    total_modified = 0
    for filepath in files:
        modified, changes = transform_file(filepath, dry_run)
        if changes:
            print(f"\n{filepath}:")
            for change in changes:
                print(f"  {change}")
            if modified:
                total_modified += 1

    print("\n" + "=" * 60)
    print(f"Summary: {total_modified} file(s) {'would be ' if dry_run else ''}modified out of {len(files)}")

    if dry_run:
        print("\nRun without --dry-run to apply changes.")


if __name__ == '__main__':
    main()
