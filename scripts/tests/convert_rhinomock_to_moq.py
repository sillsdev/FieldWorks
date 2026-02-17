#!/usr/bin/env python3
"""
Convert RhinoMocks usage to Moq in C# test files.
This script handles the common patterns used in FieldWorks test files.
"""

import re
import sys
from pathlib import Path
from typing import List, Tuple


def convert_file(file_path: Path) -> Tuple[bool, List[str]]:
    """
    Convert RhinoMocks usage to Moq in a single file.

    Returns:
        Tuple of (was_modified, changes_made)
    """
    was_modified, changes, _ = convert_file_with_content(file_path)
    return was_modified, changes


def convert_file_with_content(file_path: Path) -> Tuple[bool, List[str], str]:
    """
    Convert RhinoMocks usage to Moq in a single file.

    Returns:
        Tuple of (was_modified, changes_made, modified_content)
    """
    with open(file_path, 'r', encoding='utf-8') as f:
        content = f.read()

    original_content = content
    changes = []

    # 1. Replace using statement
    if 'using Rhino.Mocks;' in content:
        content = content.replace('using Rhino.Mocks;', 'using Moq;')
        changes.append('Replaced using Rhino.Mocks with using Moq')

    # 2. Replace MockRepository.GenerateStub<T>() with new Mock<T>().Object
    # BUT we need to track which variables were stubs so we can handle .Stub() calls on them
    pattern = r'MockRepository\.GenerateStub<([^>]+)>\(\)'
    replacement = r'new Mock<\1>().Object'
    if re.search(pattern, content):
        content = re.sub(pattern, replacement, content)
        changes.append('Converted GenerateStub to Mock<T>().Object')

    # 3. Replace MockRepository.GenerateStrictMock<T>() with new Mock<T>(MockBehavior.Strict)
    # These return the mock itself, so we need to handle .Object access
    pattern = r'MockRepository\.GenerateStrictMock<([^>]+)>\(\)'
    replacement = r'new Mock<\1>(MockBehavior.Strict)'
    if re.search(pattern, content):
        content = re.sub(pattern, replacement, content)
        changes.append('Converted GenerateStrictMock to Mock<T>(MockBehavior.Strict)')

    # 4. Replace MockRepository.GenerateMock<T>() with new Mock<T>()
    pattern = r'MockRepository\.GenerateMock<([^>]+)>\(\)'
    replacement = r'new Mock<\1>()'
    if re.search(pattern, content):
        content = re.sub(pattern, replacement, content)
        changes.append('Converted GenerateMock to Mock<T>()')

    # 5. Convert .Expect(x => x.Method).Return(value) to .Setup(x => x.Method).Returns(value)
    # Must be before .Stub conversion
    pattern = r'\.Expect\(([^)]+)\)\.Return\('
    replacement = r'.Setup(\1).Returns('
    if re.search(pattern, content):
        content = re.sub(pattern, replacement, content)
        changes.append('Converted .Expect().Return() to .Setup().Returns()')

    # 6. Convert .Expect(x => x.Method).IgnoreArguments().Return(value)
    pattern = r'\.Expect\(([^)]+)\)\.IgnoreArguments\(\)\.Return\('
    replacement = r'.Setup(\1).Returns('
    if re.search(pattern, content):
        content = re.sub(pattern, replacement, content)
        changes.append('Converted .Expect().IgnoreArguments().Return() to .Setup().Returns() - review argument matchers')

    # 7. Convert .Stub(x => x.Property).Return(value) to .Setup(x => x.Property).Returns(value)
    pattern = r'\.Stub\(([^)]+)\)\.Return\('
    replacement = r'.Setup(\1).Returns('
    if re.search(pattern, content):
        content = re.sub(pattern, replacement, content)
        changes.append('Converted .Stub().Return() to .Setup().Returns()')

    # 8. Convert Arg<T>.Is.Anything to It.IsAny<T>()
    pattern = r'Arg<([^>]+)>\.Is\.Anything'
    replacement = r'It.IsAny<\1>()'
    if re.search(pattern, content):
        content = re.sub(pattern, replacement, content)
        changes.append('Converted Arg<T>.Is.Anything to It.IsAny<T>()')

    # 9. Convert Arg<T>.Is.Equal(value) to value (for simple cases)
    # This is context-sensitive - leave for manual review
    pattern = r'Arg<([^>]+)>\.Is\.Equal'
    if re.search(pattern, content):
        changes.append('WARNING: Found Arg<T>.Is.Equal - needs manual conversion to specific value or It.Is')

    # 10. Convert Arg<T>.Is.Null to It.IsAny<T>() or null depending on context
    pattern = r'Arg<([^>]+)>\.Is\.Null'
    if re.search(pattern, content):
        changes.append('WARNING: Found Arg<T>.Is.Null - needs manual review')

    # 11. Handle out parameters - Arg<T>.Out(...).Dummy
    # In Moq, we use callback or setup out parameters differently
    pattern = r'out Arg<([^>]+)>\.Out\(([^)]+)\)\.Dummy'
    if re.search(pattern, content):
        changes.append('WARNING: Found out Arg<T>.Out().Dummy - needs manual conversion using callback')

    # 12. Handle .OutRef() - RhinoMocks specific, needs manual conversion
    pattern = r'\.OutRef\('
    if re.search(pattern, content):
        changes.append('WARNING: Found .OutRef() - needs manual conversion for multiple out parameters')

    # 13. Convert GetArgumentsForCallsMadeOn() - RhinoMocks specific
    pattern = r'\.GetArgumentsForCallsMadeOn\('
    if re.search(pattern, content):
        changes.append('WARNING: Found .GetArgumentsForCallsMadeOn() - needs conversion to Moq verification')

    was_modified = content != original_content
    return was_modified, changes, content


def process_files(file_paths: List[str]) -> None:
    """Process multiple files and report results."""
    total_modified = 0

    for file_path_str in file_paths:
        file_path = Path(file_path_str)
        if not file_path.exists():
            print(f"Warning: {file_path} does not exist", file=sys.stderr)
            continue

        print(f"\nProcessing: {file_path}")
        # convert_file returns tuple (was_modified, changes) and modifies in place
        was_modified, changes, modified_content = convert_file_with_content(file_path)

        if was_modified:
            # Write back the modified content
            with open(file_path, 'w', encoding='utf-8') as f:
                f.write(modified_content)

            print(f"  ✓ Modified")
            for change in changes:
                print(f"    - {change}")
            total_modified += 1
        else:
            print(f"  - No changes needed")

    print(f"\n{'='*60}")
    print(f"Summary: Modified {total_modified} of {len(file_paths)} files")


def main():
    """Main entry point."""
    if len(sys.argv) < 2:
        print("Usage: python convert_rhinomocks_to_moq.py <file1.cs> [file2.cs ...]")
        sys.exit(1)

    file_paths = sys.argv[1:]
    process_files(file_paths)


if __name__ == '__main__':
    main()