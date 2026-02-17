#!/usr/bin/env python3
"""
Verify COPILOT.md files follow condensed structure.
Extracts section headings and checks line counts to identify files that need condensing.
"""

import os
import re
from pathlib import Path
from typing import Dict, List, Tuple

def extract_headings(file_path: Path) -> List[str]:
    """Extract all markdown headings from a file."""
    headings = []
    with open(file_path, 'r', encoding='utf-8') as f:
        for line in f:
            if line.startswith('#'):
                heading = line.strip()
                headings.append(heading)
    return headings

def count_lines(file_path: Path) -> int:
    """Count non-empty lines in a file."""
    with open(file_path, 'r', encoding='utf-8') as f:
        return sum(1 for line in f if line.strip())

def is_condensed(headings: List[str], line_count: int, is_organizational: bool) -> Tuple[bool, str]:
    """
    Determine if a file follows condensed structure.
    
    Organizational folders should be <100 lines.
    Leaf folders should have condensed sections with minimal verbose content.
    """
    if is_organizational:
        if line_count < 100:
            return True, f"Organizational: {line_count} lines (condensed)"
        else:
            return False, f"Organizational: {line_count} lines (needs condensing, target <100)"
    
    # For leaf folders, check for signs of verbose content
    # Look for typical verbose section patterns
    verbose_indicators = [
        'Key Components',
        'Technology Stack',
        'Dependencies',
        'Interop & Contracts',
        'Threading & Performance',
        'Build Information',
    ]
    
    has_verbose_sections = sum(1 for h in headings if any(ind in h for ind in verbose_indicators))
    
    # If file is very long (>200 lines) or has many verbose sections, likely not condensed
    if line_count > 200:
        return False, f"Leaf: {line_count} lines (needs condensing, target <150)"
    elif line_count > 150 and has_verbose_sections > 4:
        return False, f"Leaf: {line_count} lines with {has_verbose_sections} verbose sections (may need condensing)"
    else:
        return True, f"Leaf: {line_count} lines (likely condensed)"

def find_copilot_files(src_dir: Path) -> List[Path]:
    """Find all COPILOT.md files in Src/ directory."""
    copilot_files = []
    for root, dirs, files in os.walk(src_dir):
        if 'COPILOT.md' in files:
            copilot_files.append(Path(root) / 'COPILOT.md')
    return sorted(copilot_files)

def is_organizational_folder(file_path: Path) -> bool:
    """Determine if a COPILOT.md is in an organizational folder."""
    # Check if the folder has subfolders with their own COPILOT.md files
    parent_dir = file_path.parent
    subfolders_with_copilot = []
    
    for item in parent_dir.iterdir():
        if item.is_dir():
            copilot_path = item / 'COPILOT.md'
            if copilot_path.exists():
                subfolders_with_copilot.append(item.name)
    
    return len(subfolders_with_copilot) >= 2

def main():
    repo_root = Path('/home/runner/work/FieldWorks/FieldWorks')
    src_dir = repo_root / 'Src'
    
    if not src_dir.exists():
        print(f"Error: {src_dir} not found")
        return
    
    copilot_files = find_copilot_files(src_dir)
    print(f"Found {len(copilot_files)} COPILOT.md files\n")
    
    results = {}
    needs_condensing = []
    
    for file_path in copilot_files:
        rel_path = file_path.relative_to(repo_root)
        headings = extract_headings(file_path)
        line_count = count_lines(file_path)
        is_org = is_organizational_folder(file_path)
        
        condensed, status = is_condensed(headings, line_count, is_org)
        
        results[str(rel_path)] = {
            'line_count': line_count,
            'heading_count': len(headings),
            'is_organizational': is_org,
            'condensed': condensed,
            'status': status,
            'headings': headings
        }
        
        if not condensed:
            needs_condensing.append(str(rel_path))
    
    # Print summary
    print("=" * 80)
    print("SUMMARY")
    print("=" * 80)
    condensed_count = sum(1 for r in results.values() if r['condensed'])
    print(f"Condensed: {condensed_count}/{len(results)}")
    print(f"Needs condensing: {len(needs_condensing)}/{len(results)}")
    print()
    
    if needs_condensing:
        print("FILES THAT NEED CONDENSING:")
        print("-" * 80)
        for file_path in needs_condensing:
            info = results[file_path]
            print(f"  {file_path}")
            print(f"    Status: {info['status']}")
            print(f"    Headings: {info['heading_count']}")
            print()
    else:
        print("✓ All files appear to be condensed!")
    
    # Print detailed results to file
    output_file = repo_root / 'copilot_structure_report.txt'
    with open(output_file, 'w', encoding='utf-8') as f:
        f.write("COPILOT.md Structure Verification Report\n")
        f.write("=" * 80 + "\n\n")
        
        for file_path in sorted(results.keys()):
            info = results[file_path]
            f.write(f"{file_path}\n")
            f.write(f"  Lines: {info['line_count']}\n")
            f.write(f"  Headings: {info['heading_count']}\n")
            f.write(f"  Type: {'Organizational' if info['is_organizational'] else 'Leaf'}\n")
            f.write(f"  Status: {info['status']}\n")
            f.write(f"  Condensed: {'✓' if info['condensed'] else '✗'}\n")
            f.write(f"  Sections:\n")
            for heading in info['headings'][:10]:  # First 10 headings
                f.write(f"    {heading}\n")
            if len(info['headings']) > 10:
                f.write(f"    ... and {len(info['headings']) - 10} more\n")
            f.write("\n")
    
    print(f"\nDetailed report written to: {output_file}")
    
    return len(needs_condensing)

if __name__ == '__main__':
    exit(main())
