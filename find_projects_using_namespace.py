#!/usr/bin/env python3
"""
Find .csproj files that contain .cs files using a specific namespace.
Can also find projects that DON'T use a namespace (for cleanup).

Usage:
    python find_projects_using_namespace.py <namespace_pattern> [options]
    python find_projects_using_namespace.py --help

Example:
    # Find projects using Newtonsoft.Json
    python find_projects_using_namespace.py "Newtonsoft.Json"
    
    # Find projects that have Moq but don't use it (for cleanup)
    python find_projects_using_namespace.py "Moq" --check-package "Moq" --find-unused
    
    # Generate command to remove unused package references
    python find_projects_using_namespace.py "Moq" --check-package "Moq" --find-unused --generate-remove-command
"""

import sys
import argparse
import re
from pathlib import Path
from collections import defaultdict
import xml.etree.ElementTree as ET


def find_using_statements(cs_file, namespace_patterns, include_patterns=None):
    """
    Find if a .cs file uses any of the specified namespaces.
    
    Args:
        cs_file: Path to .cs file
        namespace_patterns: List of namespace patterns to search for
        include_patterns: Optional list of additional code patterns to search for
    
    Returns:
        List of matching patterns found (empty if none found)
    """
    try:
        with open(cs_file, 'r', encoding='utf-8', errors='ignore') as f:
            content = f.read()
        
        matches = []
        
        # Check for using statements
        for pattern in namespace_patterns:
            # Match: using Namespace; or using Namespace.Subnamespace;
            regex = rf'\busing\s+{re.escape(pattern)}(?:\.[a-zA-Z0-9_]+)*\s*;'
            if re.search(regex, content):
                matches.append(f"using {pattern}")
        
        # Check for additional code patterns if provided
        if include_patterns:
            for pattern in include_patterns:
                if re.search(pattern, content):
                    matches.append(f"code: {pattern}")
        
        return matches
        
    except Exception as e:
        return []


def find_project_for_file(cs_file, src_dir):
    """
    Find the .csproj file that contains a given .cs file.
    
    Args:
        cs_file: Path to .cs file
        src_dir: Root source directory
    
    Returns:
        Path to .csproj file or None if not found
    """
    # Walk up the directory tree looking for a .csproj file
    current_dir = cs_file.parent
    
    while current_dir >= src_dir:
        # Look for .csproj files in current directory
        csproj_files = list(current_dir.glob('*.csproj'))
        if csproj_files:
            # Return the first .csproj found
            return csproj_files[0]
        
        # Move up one directory
        if current_dir == src_dir:
            break
        current_dir = current_dir.parent
    
    return None


def has_package_reference(csproj_path, package_name):
    """Check if a .csproj file has a PackageReference for the given package."""
    try:
        tree = ET.parse(csproj_path)
        root = tree.getroot()
        
        for elem in root.iter():
            if elem.tag.endswith('PackageReference'):
                include = elem.get('Include')
                if include and include.lower() == package_name.lower():
                    return True
        
        return False
    except:
        return False


def get_all_projects_with_package(src_dir, package_name):
    """
    Find all .csproj files that have a PackageReference for the given package.
    
    Args:
        src_dir: Root source directory
        package_name: Name of package to search for
    
    Returns:
        List of project paths that have the package
    """
    src_path = Path(src_dir)
    projects_with_package = []
    
    for csproj in src_path.rglob('*.csproj'):
        if has_package_reference(csproj, package_name):
            projects_with_package.append(csproj)
    
    return projects_with_package


def scan_for_namespace_usage(src_dir, namespace_patterns, include_patterns=None):
    """
    Scan all .cs files in src_dir for namespace usage and group by project.
    
    Args:
        src_dir: Root source directory to scan
        namespace_patterns: List of namespace patterns to search for
        include_patterns: Optional list of additional code patterns
    
    Returns:
        Dict mapping project paths to list of (file, matches) tuples
    """
    src_path = Path(src_dir)
    
    # Find all .cs files
    cs_files = list(src_path.rglob('*.cs'))
    
    # Group by project
    project_usage = defaultdict(list)
    
    for cs_file in cs_files:
        matches = find_using_statements(cs_file, namespace_patterns, include_patterns)
        
        if matches:
            # Find the project this file belongs to
            project = find_project_for_file(cs_file, src_path)
            
            if project:
                rel_cs_path = cs_file.relative_to(project.parent)
                project_usage[project].append((rel_cs_path, matches))
    
    return project_usage


def find_unused_package_references(src_dir, package_name, namespace_patterns, include_patterns=None):
    """
    Find projects that have a PackageReference but don't use the namespace.
    
    Args:
        src_dir: Root source directory
        package_name: Name of package to check
        namespace_patterns: Namespace patterns to search for
        include_patterns: Optional additional code patterns
    
    Returns:
        List of project paths that have the package but don't use it
    """
    # Get all projects with the package
    projects_with_package = get_all_projects_with_package(src_dir, package_name)
    
    # Get all projects that use the namespace
    projects_using_namespace = set(scan_for_namespace_usage(src_dir, namespace_patterns, include_patterns).keys())
    
    # Find projects that have package but don't use it
    unused = []
    for project in projects_with_package:
        if project not in projects_using_namespace:
            unused.append(project)
    
    return unused


def format_output(project_usage, check_package=None, show_files=False, output_format='text'):
    """
    Format the output of project usage.
    
    Args:
        project_usage: Dict of project -> list of (file, matches)
        check_package: Optional package name to check if already referenced
        show_files: Whether to show individual files
        output_format: 'text', 'list', or 'csv'
    """
    if output_format == 'csv':
        # CSV format: project,file_count,has_package
        print("Project,FileCount,HasPackage")
        for project in sorted(project_usage.keys()):
            files = project_usage[project]
            has_pkg = 'Yes' if (check_package and has_package_reference(project, check_package)) else 'No'
            print(f"{project},{len(files)},{has_pkg}")
    
    elif output_format == 'list':
        # Simple list of project paths
        for project in sorted(project_usage.keys()):
            print(project)
    
    else:  # text format
        for project in sorted(project_usage.keys()):
            files = project_usage[project]
            
            # Check if project already has the package
            status = ''
            if check_package:
                if has_package_reference(project, check_package):
                    status = ' ✓ (already has PackageReference)'
                else:
                    status = ' ✗ (missing PackageReference)'
            
            print(f"\n{project}{status}")
            print(f"  {len(files)} file(s) with matches")
            
            if show_files:
                for cs_file, matches in files:
                    print(f"    {cs_file}")
                    for match in matches:
                        print(f"      - {match}")


def format_unused_output(unused_projects, output_format='text'):
    """Format output for unused package references."""
    if output_format == 'csv':
        print("Project")
        for project in sorted(unused_projects):
            print(project)
    elif output_format == 'list':
        for project in sorted(unused_projects):
            print(project)
    else:  # text
        if not unused_projects:
            print("No unused package references found.")
        else:
            print("Projects with unused PackageReference:")
            for project in sorted(unused_projects):
                print(f"  ✗ {project}")


def generate_add_commands(project_usage, package_name, version, check_package=None):
    """
    Generate add_package_reference.py commands for projects that need the package.
    
    Args:
        project_usage: Dict of project -> list of files
        package_name: Name of package to add
        version: Version of package
        check_package: Optional package name to check (if different from package_name)
    """
    if not check_package:
        check_package = package_name
    
    # Filter to projects that don't have the package
    projects_needing_package = []
    
    for project in sorted(project_usage.keys()):
        if not has_package_reference(project, check_package):
            projects_needing_package.append(str(project))
    
    if not projects_needing_package:
        print("# All projects already have the package")
        return
    
    # Generate command
    print(f"# Add {package_name} v{version} to {len(projects_needing_package)} project(s)")
    print(f"python add_package_reference.py {package_name} \"{version}\" \\")
    
    for i, project in enumerate(projects_needing_package):
        if i < len(projects_needing_package) - 1:
            print(f"  {project} \\")
        else:
            print(f"  {project}")


def generate_remove_commands(unused_projects, package_name):
    """
    Generate remove_package_reference.py commands for unused packages.
    
    Args:
        unused_projects: List of project paths with unused package
        package_name: Name of package to remove
    """
    if not unused_projects:
        print("# No unused package references to remove")
        return
    
    print(f"# Remove unused {package_name} from {len(unused_projects)} project(s)")
    print(f"python remove_package_reference.py {package_name} \\")
    
    for i, project in enumerate(sorted(unused_projects)):
        if i < len(unused_projects) - 1:
            print(f"  {project} \\")
        else:
            print(f"  {project}")


def main():
    parser = argparse.ArgumentParser(
        description='Find .csproj files that use (or don\'t use) a specific namespace.',
        formatter_class=argparse.RawDescriptionHelpFormatter,
        epilog="""
Examples:
  # Find projects using Newtonsoft.Json
  python find_projects_using_namespace.py "Newtonsoft.Json"
  
  # Find projects using ICU with additional pattern matching
  python find_projects_using_namespace.py "Icu" --include-patterns "Collator" "Normalizer"
  
  # Check which projects already have the package
  python find_projects_using_namespace.py "Moq" --check-package "Moq"
  
  # Find projects that have Moq but DON'T use it (cleanup)
  python find_projects_using_namespace.py "Moq" --check-package "Moq" --find-unused
  
  # Show individual files
  python find_projects_using_namespace.py "DocumentFormat.OpenXml" --show-files
  
  # Generate command to add package to projects that need it
  python find_projects_using_namespace.py "Newtonsoft.Json" \\
    --generate-command "Newtonsoft.Json" "13.0.3"
  
  # Generate command to remove unused package references
  python find_projects_using_namespace.py "Moq" --check-package "Moq" \\
    --find-unused --generate-remove-command
  
  # Multiple namespaces (OR logic)
  python find_projects_using_namespace.py "Newtonsoft.Json" "Newtonsoft.Json.Linq"
  
  # Output formats
  python find_projects_using_namespace.py "Moq" --format list
  python find_projects_using_namespace.py "Moq" --format csv --check-package "Moq"
        """
    )
    
    parser.add_argument('namespaces', nargs='+',
                        help='Namespace(s) to search for (e.g., "Newtonsoft.Json")')
    parser.add_argument('--include-patterns', nargs='+',
                        help='Additional regex patterns to search for in code (e.g., "JObject" "JsonConvert")')
    parser.add_argument('--check-package',
                        help='Check if projects already have this package as PackageReference')
    parser.add_argument('--find-unused', action='store_true',
                        help='Find projects that have the package but DON\'T use the namespace (for cleanup)')
    parser.add_argument('--show-files', action='store_true',
                        help='Show individual .cs files that match')
    parser.add_argument('--format', choices=['text', 'list', 'csv'], default='text',
                        help='Output format')
    parser.add_argument('--generate-command', nargs=2, metavar=('PACKAGE', 'VERSION'),
                        help='Generate add_package_reference.py command (package name and version)')
    parser.add_argument('--generate-remove-command', action='store_true',
                        help='Generate remove_package_reference.py command (requires --find-unused and --check-package)')
    parser.add_argument('--src-dir', default='Src',
                        help='Source directory to scan (default: Src)')
    
    args = parser.parse_args()
    
    # Validate src directory
    src_path = Path(args.src_dir)
    if not src_path.exists():
        print(f"Error: Source directory '{args.src_dir}' not found", file=sys.stderr)
        sys.exit(1)
    
    # Validate combinations
    if args.generate_remove_command and (not args.find_unused or not args.check_package):
        print("Error: --generate-remove-command requires both --find-unused and --check-package", file=sys.stderr)
        sys.exit(1)
    
    # Find unused mode
    if args.find_unused:
        if not args.check_package:
            print("Error: --find-unused requires --check-package", file=sys.stderr)
            sys.exit(1)
        
        unused_projects = find_unused_package_references(
            src_path,
            args.check_package,
            args.namespaces,
            args.include_patterns
        )
        
        if args.generate_remove_command:
            generate_remove_commands(unused_projects, args.check_package)
        else:
            format_unused_output(unused_projects, args.format)
            
            if args.format == 'text':
                print(f"\n{'='*60}")
                print(f"Summary: {len(unused_projects)} project(s) have {args.check_package} but don't use it")
        
        sys.exit(0)
    
    # Normal mode - find projects that DO use the namespace
    project_usage = scan_for_namespace_usage(
        src_path,
        args.namespaces,
        args.include_patterns
    )
    
    if not project_usage:
        print(f"No projects found using namespace(s): {', '.join(args.namespaces)}")
        sys.exit(0)
    
    # Generate command if requested
    if args.generate_command:
        package_name, version = args.generate_command
        generate_add_commands(project_usage, package_name, version, args.check_package)
    else:
        # Regular output
        format_output(project_usage, args.check_package, args.show_files, args.format)
        
        # Summary
        if args.format == 'text':
            print(f"\n{'='*60}")
            print(f"Summary: {len(project_usage)} project(s) use namespace(s): {', '.join(args.namespaces)}")
            
            if args.check_package:
                with_pkg = sum(1 for p in project_usage.keys() if has_package_reference(p, args.check_package))
                without_pkg = len(project_usage) - with_pkg
                print(f"  {with_pkg} already have PackageReference for '{args.check_package}'")
                print(f"  {without_pkg} missing PackageReference")


if __name__ == '__main__':
    main()
