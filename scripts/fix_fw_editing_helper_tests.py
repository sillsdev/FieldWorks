#!/usr/bin/env python3
"""
Fix RhinoMocks patterns in FwEditingHelperTests.cs.
Converts to proper Moq syntax.
"""

import re
import sys
from pathlib import Path

def fix_file(filepath: Path) -> int:
    """Fix RhinoMocks patterns in the file. Returns number of replacements made."""

    with open(filepath, 'r', encoding='utf-8') as f:
        content = f.read()

    original = content
    replacements = 0

    # Pattern 1: Replace SelectionHelper mock creation pattern
    pattern1 = r'var selHelper = SelectionHelper\.s_mockedSelectionHelper =\s*\n\s*new Mock<SelectionHelper>\(\)\.Object;\s*\n\s*selHelper\.Setup\(selH => selH\.Selection\)\.Returns\(selection\);'
    replacement1 = 'var selHelper = MakeMockSelectionHelper(selection);'
    content, n = re.subn(pattern1, replacement1, content)
    replacements += n
    print(f"  Pattern 1 (SelectionHelper mock creation): {n} replacements")

    # Pattern 2: Replace GetArgumentsForCallsMadeOn pattern (multi-line)
    pattern2 = r'IList<object\[\]> argsSentToSetTypingProps =\s*\n\s*selection\.GetArgumentsForCallsMadeOn\(sel => sel\.SetTypingProps\(null\)\);'
    replacement2 = 'IList<object[]> argsSentToSetTypingProps = GetSetTypingPropsArguments();'
    content, n = re.subn(pattern2, replacement2, content)
    replacements += n
    print(f"  Pattern 2 (GetArgumentsForCallsMadeOn multi-line): {n} replacements")

    # Pattern 3: Replace GetArgumentsForCallsMadeOn pattern (single-line)
    pattern3 = r'selection\.GetArgumentsForCallsMadeOn\(sel => sel\.SetTypingProps\(null\)\)'
    replacement3 = 'GetSetTypingPropsArguments()'
    content, n = re.subn(pattern3, replacement3, content)
    replacements += n
    print(f"  Pattern 3 (GetArgumentsForCallsMadeOn single-line): {n} replacements")

    # Pattern 4: Replace selHelper.Stub with m_selHelperMock.Setup
    # selHelper.Stub(...).Return(x) -> m_selHelperMock.Setup(...).Returns(x)
    pattern4 = r'selHelper\.Stub\('
    replacement4 = 'm_selHelperMock.Setup('
    content, n = re.subn(pattern4, replacement4, content)
    replacements += n
    print(f"  Pattern 4 (selHelper.Stub -> m_selHelperMock.Setup): {n} replacements")

    # Pattern 5: Replace .Return( with .Returns(
    pattern5 = r'\.Return\('
    replacement5 = '.Returns('
    content, n = re.subn(pattern5, replacement5, content)
    replacements += n
    print(f"  Pattern 5 (.Return -> .Returns): {n} replacements")

    # Pattern 6: Replace Arg<T>.Is.Equal(x) with It.Is<T>(v => v == x)
    # This is complex - for SelectionHelper.SelLimitType
    pattern6 = r'Arg<SelectionHelper\.SelLimitType>\.Is\.Equal\(\s*SelectionHelper\.SelLimitType\.(\w+)\)'
    replacement6 = r'It.Is<SelectionHelper.SelLimitType>(v => v == SelectionHelper.SelLimitType.\1)'
    content, n = re.subn(pattern6, replacement6, content)
    replacements += n
    print(f"  Pattern 6 (Arg<T>.Is.Equal -> It.Is<T>): {n} replacements")

    # Pattern 7: Replace mockStylesheet.Stub with mockStylesheetMock.Setup (if it exists)
    pattern7 = r'mockStylesheet\.Stub\('
    replacement7 = 'mockStylesheetMock.Setup('
    content, n = re.subn(pattern7, replacement7, content)
    replacements += n
    print(f"  Pattern 7 (mockStylesheet.Stub -> mockStylesheetMock.Setup): {n} replacements")

    if content != original:
        with open(filepath, 'w', encoding='utf-8') as f:
            f.write(content)
        print(f"  File updated with {replacements} total replacements")
    else:
        print("  No changes needed")

    return replacements

def main():
    repo_root = Path(__file__).parent.parent
    target_file = repo_root / "Src" / "Common" / "Framework" / "FrameworkTests" / "FwEditingHelperTests.cs"

    if not target_file.exists():
        print(f"Error: File not found: {target_file}")
        return 1

    print(f"Processing: {target_file}")
    fix_file(target_file)
    print("Done!")
    return 0

if __name__ == "__main__":
    sys.exit(main())
