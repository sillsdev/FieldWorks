#!/usr/bin/env python3
"""
Remove PackageReference from .csproj files efficiently.

Usage:
    python remove_package_reference.py <package_name> <project1.csproj> [project2.csproj ...]
    python remove_package_reference.py --help

Example:
    python remove_package_reference.py Moq Src/Common/FwUtils/FwUtils.csproj
    python remove_package_reference.py System.Memory "Src/**/*.csproj"
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
        tree = ET.parse(csproj_path)
        return tree
    except Exception as e:
        print(f"Error parsing {csproj_path}: {e}", file=sys.stderr)
        return None


def remove_package_reference(csproj_path, package_name):
    """
    Remove a PackageReference from a .csproj file if it exists.
    
    Args:
        csproj_path: Path to the .csproj file
        package_name: Name of the NuGet package to remove
    
    Returns:
        (success: bool, message: str)
    """
    tree = parse_csproj(csproj_path)
    if tree is None:
        return False, f"Failed to parse {csproj_path}"
    
    root = tree.getroot()
    
    # Find and remove the PackageReference
    removed = False
    for item_group in root.findall('.//{*}ItemGroup'):
        for pkg_ref in list(item_group.findall('.//{*}PackageReference')):
            include = pkg_ref.get('Include')
            if include and include.lower() == package_name.lower():
                item_group.remove(pkg_ref)
                removed = True
                
                # If ItemGroup is now empty, remove it
                if len(list(item_group)) == 0:
                    root.remove(item_group)
                
                break
        
        if removed:
            break
    
    if not removed:
        return False, f"Package '{package_name}' not found in {csproj_path}"
    
    # Write back to file
    try:
        # Read original to check for XML declaration
        with open(csproj_path, 'r', encoding='utf-8') as f:
            original_content = f.read()
        
        has_declaration = original_content.strip().startswith('<?xml')
        
        # Determine namespace
        namespace = ''
        if root.tag.startswith('{'):
            namespace = root.tag[root.tag.find('{')+1:root.tag.find('}')]
        
        # Register namespace to avoid ns0: prefix
        ET.register_namespace('', namespace if namespace else 'http://schemas.microsoft.com/developer/msbuild/2003')
        
        # Write tree
        tree.write(csproj_path, encoding='utf-8', xml_declaration=has_declaration)
        
        # Ensure proper line ending
        with open(csproj_path, 'r', encoding='utf-8') as f:
            content = f.read()
        
        if not content.endswith('\n'):
            content += '\n'
        
        with open(csproj_path, 'w', encoding='utf-8', newline='') as f:
            f.write(content)
        
        return True, f"Removed '{package_name}' from {csproj_path}"
        
    except Exception as e:
        return False, f"Error writing {csproj_path}: {e}"


def expand_glob_patterns(patterns):
    """Expand glob patterns to actual file paths."""
    files = []
    for pattern in patterns:
        if '*' in pattern or '?' in pattern:
            expanded = glob(pattern, recursive=True)
            files.extend(expanded)
        else:
            if Path(pattern).exists():
                files.append(pattern)
            else:
                print(f"Warning: Path not found: {pattern}", file=sys.stderr)
    
    return files


def main():
    parser = argparse.ArgumentParser(
        description='Remove PackageReference from .csproj files efficiently.',
        formatter_class=argparse.RawDescriptionHelpFormatter,
        epilog="""
Examples:
  # Remove Moq from a single project
  python remove_package_reference.py Moq Src/Common/FwUtils/FwUtils.csproj
  
  # Remove System.Memory from all projects (it's usually transitive)
  python remove_package_reference.py System.Memory "Src/**/*.csproj"
  
  # Dry run to see what would be removed
  python remove_package_reference.py --dry-run Moq Src/Common/FwUtils/FwUtils.csproj
  
  # Remove from multiple specific projects
  python remove_package_reference.py Castle.Core \\
    Src/Project1/Project1.csproj \\
    Src/Project2/Project2.csproj
        """
    )
    
    parser.add_argument('package', help='Package name to remove (e.g., Moq)')
    parser.add_argument('projects', nargs='+', help='Project file(s) or glob patterns')
    parser.add_argument('--dry-run', action='store_true',
                        help='Show what would be done without making changes')
    parser.add_argument('-v', '--verbose', action='store_true',
                        help='Verbose output')
    
    args = parser.parse_args()
    
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
        print(f"Package to remove: {args.package}")
        print()
    
    # Process each project
    removed_count = 0
    not_found_count = 0
    error_count = 0
    
    for project in sorted(project_files):
        if args.dry_run:
            tree = parse_csproj(project)
            if tree:
                root = tree.getroot()
                found = False
                for elem in root.iter():
                    if elem.tag.endswith('PackageReference'):
                        include = elem.get('Include')
                        if include and include.lower() == args.package.lower():
                            found = True
                            break
                
                if found:
                    print(f"[DRY RUN] Would remove '{args.package}' from {project}")
                    removed_count += 1
                else:
                    if args.verbose:
                        print(f"[DRY RUN] Package not found in {project}")
                    not_found_count += 1
            else:
                print(f"[DRY RUN] Error parsing {project}")
                error_count += 1
        else:
            success, message = remove_package_reference(project, args.package)
            if success:
                print(f"✓ {message}")
                removed_count += 1
            else:
                if 'not found' in message:
                    if args.verbose:
                        print(f"⊝ {message}")
                    not_found_count += 1
                else:
                    print(f"✗ {message}", file=sys.stderr)
                    error_count += 1
    
    # Summary
    print()
    print(f"Summary: {removed_count} removed, {not_found_count} not found, {error_count} errors")
    
    if error_count > 0:
        sys.exit(1)


if __name__ == '__main__':
    main()
