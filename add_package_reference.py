#!/usr/bin/env python3
"""
Add PackageReference to .csproj files efficiently.

Usage:
    python add_package_reference.py <package_name> <version> <project1.csproj> [project2.csproj ...]
    python add_package_reference.py --help

Example:
    python add_package_reference.py icu.net "3.0.0-beta.297" Src/Common/FwUtils/FwUtils.csproj
    python add_package_reference.py Moq "4.17.2" Src/**/Tests/*.csproj
"""

import sys
import argparse
import re
from pathlib import Path
from glob import glob
import xml.etree.ElementTree as ET


def parse_csproj(csproj_path):
    """Parse a .csproj file and return the ElementTree."""
    try:
        # Parse with namespace handling
        tree = ET.parse(csproj_path)
        return tree
    except Exception as e:
        print(f"Error parsing {csproj_path}: {e}", file=sys.stderr)
        return None


def has_package_reference(tree, package_name):
    """Check if a package is already referenced in the project."""
    root = tree.getroot()
    
    # Find all PackageReference elements (namespace-aware)
    for elem in root.iter():
        if elem.tag.endswith('PackageReference'):
            include = elem.get('Include')
            if include and include.lower() == package_name.lower():
                return True
    
    return False


def get_insertion_point(root):
    """
    Find the best place to insert a new PackageReference.
    Returns (parent, insert_index) tuple.
    """
    # Look for existing ItemGroup with PackageReference elements
    for item_group in root.findall('.//{*}ItemGroup'):
        for child in item_group:
            if child.tag.endswith('PackageReference'):
                # Found an ItemGroup with PackageReferences
                # Insert at the end of this group, but keep it sorted
                return item_group, None
    
    # Look for any ItemGroup (fallback)
    for item_group in root.findall('.//{*}ItemGroup'):
        return item_group, None
    
    # No ItemGroup found - need to create one
    # Insert after PropertyGroup elements
    last_prop_group_idx = -1
    for idx, child in enumerate(root):
        if child.tag.endswith('PropertyGroup'):
            last_prop_group_idx = idx
    
    return root, last_prop_group_idx + 1


def add_package_reference(csproj_path, package_name, version, attributes=None):
    """
    Add a PackageReference to a .csproj file if it doesn't already exist.
    
    Args:
        csproj_path: Path to the .csproj file
        package_name: Name of the NuGet package
        version: Version string
        attributes: Optional dict of additional attributes (e.g., {'PrivateAssets': 'All'})
    
    Returns:
        (success: bool, message: str)
    """
    tree = parse_csproj(csproj_path)
    if tree is None:
        return False, f"Failed to parse {csproj_path}"
    
    # Check if package already exists
    if has_package_reference(tree, package_name):
        return False, f"Package '{package_name}' already exists in {csproj_path}"
    
    root = tree.getroot()
    
    # Find or create appropriate ItemGroup
    parent, insert_idx = get_insertion_point(root)
    
    # Determine namespace
    namespace = ''
    if root.tag.startswith('{'):
        namespace = root.tag[root.tag.find('{')+1:root.tag.find('}')]
    
    ns_prefix = f'{{{namespace}}}' if namespace else ''
    
    # Create ItemGroup if we're inserting at root level
    if parent == root and insert_idx is not None:
        item_group = ET.Element(f'{ns_prefix}ItemGroup')
        root.insert(insert_idx, item_group)
        parent = item_group
        
        # Add newline and indentation
        if insert_idx > 0:
            prev = root[insert_idx - 1]
            if prev.tail:
                item_group.tail = prev.tail
            else:
                item_group.tail = '\n  '
    
    # Create PackageReference element
    pkg_ref = ET.Element(f'{ns_prefix}PackageReference')
    pkg_ref.set('Include', package_name)
    pkg_ref.set('Version', version)
    
    # Add any additional attributes
    if attributes:
        for key, value in attributes.items():
            pkg_ref.set(key, value)
    
    # Find the right insertion point within ItemGroup (keep sorted)
    insert_pos = len(parent)
    for idx, child in enumerate(parent):
        if child.tag.endswith('PackageReference'):
            child_name = child.get('Include', '').lower()
            if package_name.lower() < child_name:
                insert_pos = idx
                break
    
    parent.insert(insert_pos, pkg_ref)
    
    # Format nicely with proper indentation
    # Determine indentation level
    indent = '    '  # Default 4 spaces
    if len(parent) > 1:
        # Try to match existing indentation
        prev_sibling = parent[insert_pos - 1] if insert_pos > 0 else None
        if prev_sibling is not None and prev_sibling.tail:
            indent_match = re.search(r'\n(\s+)', prev_sibling.tail)
            if indent_match:
                indent = indent_match.group(1)
    
    # Set indentation
    pkg_ref.tail = '\n  '
    if insert_pos > 0:
        parent[insert_pos - 1].tail = f'\n{indent}'
    
    # Write back to file, preserving XML declaration and formatting
    try:
        # Read original to preserve declaration and formatting style
        with open(csproj_path, 'r', encoding='utf-8') as f:
            original_content = f.read()
        
        # Check if original has XML declaration
        has_declaration = original_content.strip().startswith('<?xml')
        
        # Write tree to string
        ET.register_namespace('', namespace if namespace else 'http://schemas.microsoft.com/developer/msbuild/2003')
        
        tree.write(csproj_path, encoding='utf-8', xml_declaration=has_declaration)
        
        # Clean up: fix spacing issues
        with open(csproj_path, 'r', encoding='utf-8') as f:
            content = f.read()
        
        # Ensure proper line endings and spacing
        if not content.endswith('\n'):
            content += '\n'
        
        with open(csproj_path, 'w', encoding='utf-8', newline='') as f:
            f.write(content)
        
        return True, f"Added '{package_name}' v{version} to {csproj_path}"
        
    except Exception as e:
        return False, f"Error writing {csproj_path}: {e}"


def expand_glob_patterns(patterns):
    """Expand glob patterns to actual file paths."""
    files = []
    for pattern in patterns:
        if '*' in pattern or '?' in pattern:
            # It's a glob pattern
            expanded = glob(pattern, recursive=True)
            files.extend(expanded)
        else:
            # It's a direct path
            if Path(pattern).exists():
                files.append(pattern)
            else:
                print(f"Warning: Path not found: {pattern}", file=sys.stderr)
    
    return files


def main():
    parser = argparse.ArgumentParser(
        description='Add PackageReference to .csproj files efficiently.',
        formatter_class=argparse.RawDescriptionHelpFormatter,
        epilog="""
Examples:
  # Add icu.net to a single project
  python add_package_reference.py icu.net "3.0.0-beta.297" Src/Common/FwUtils/FwUtils.csproj
  
  # Add Moq to all test projects (with glob pattern)
  python add_package_reference.py Moq "4.17.2" "Src/**/*Tests/*.csproj"
  
  # Add a test package with PrivateAssets
  python add_package_reference.py --attr PrivateAssets=All SIL.TestUtilities "17.0.0-*" "Src/**/Tests/*.csproj"
  
  # Dry run to see what would be added
  python add_package_reference.py --dry-run Newtonsoft.Json "13.0.3" Src/Common/FwUtils/FwUtils.csproj
        """
    )
    
    parser.add_argument('package', help='Package name (e.g., icu.net)')
    parser.add_argument('version', help='Package version (e.g., 3.0.0-beta.297)')
    parser.add_argument('projects', nargs='+', help='Project file(s) or glob patterns')
    parser.add_argument('--attr', action='append', dest='attributes',
                        help='Additional attribute (format: key=value). Can be used multiple times.')
    parser.add_argument('--dry-run', action='store_true',
                        help='Show what would be done without making changes')
    parser.add_argument('-v', '--verbose', action='store_true',
                        help='Verbose output')
    
    args = parser.parse_args()
    
    # Parse attributes
    attributes = {}
    if args.attributes:
        for attr in args.attributes:
            if '=' not in attr:
                print(f"Error: Invalid attribute format '{attr}'. Use key=value", file=sys.stderr)
                sys.exit(1)
            key, value = attr.split('=', 1)
            attributes[key.strip()] = value.strip()
    
    # Expand glob patterns
    project_files = expand_glob_patterns(args.projects)
    
    if not project_files:
        print("Error: No project files found", file=sys.stderr)
        sys.exit(1)
    
    # Filter to only .csproj files
    project_files = [f for f in project_files if f.endswith('.csproj')]
    
    if not project_files:
        print("Error: No .csproj files found in the specified paths", file=sys.stderr)
        sys.exit(1)
    
    if args.verbose:
        print(f"Found {len(project_files)} project file(s)")
        print(f"Package: {args.package} v{args.version}")
        if attributes:
            print(f"Attributes: {attributes}")
        print()
    
    # Process each project
    added_count = 0
    skipped_count = 0
    error_count = 0
    
    for project in sorted(project_files):
        if args.dry_run:
            tree = parse_csproj(project)
            if tree and not has_package_reference(tree, args.package):
                print(f"[DRY RUN] Would add '{args.package}' to {project}")
                added_count += 1
            elif tree:
                print(f"[DRY RUN] Would skip {project} (already has {args.package})")
                skipped_count += 1
            else:
                print(f"[DRY RUN] Error parsing {project}")
                error_count += 1
        else:
            success, message = add_package_reference(project, args.package, args.version, attributes)
            if success:
                print(f"✓ {message}")
                added_count += 1
            else:
                if 'already exists' in message:
                    if args.verbose:
                        print(f"⊝ {message}")
                    skipped_count += 1
                else:
                    print(f"✗ {message}", file=sys.stderr)
                    error_count += 1
    
    # Summary
    print()
    print(f"Summary: {added_count} added, {skipped_count} skipped, {error_count} errors")
    
    if error_count > 0:
        sys.exit(1)


if __name__ == '__main__':
    main()
