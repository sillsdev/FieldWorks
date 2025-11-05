#!/usr/bin/env python3
"""
NUnit 3 to NUnit 4 Conversion Script
Converts old-style NUnit assertions to the new Assert.That() syntax.
"""

import re
import sys
import os
from pathlib import Path

def convert_assert_are_equal(content):
    """Convert Assert.AreEqual to Assert.That with Is.EqualTo"""
    # Handle multi-line AreEqual with messages
    pattern = r'Assert\.AreEqual\(\s*([^,]+?)\s*,\s*([^,]+?)\s*,\s*([^)]+?)\s*\)'
    replacement = r'Assert.That(\2, Is.EqualTo(\1), \3)'
    content = re.sub(pattern, replacement, content)
    
    # Handle simple AreEqual
    pattern = r'Assert\.AreEqual\(\s*([^,]+?)\s*,\s*([^,]+?)\s*\)'
    replacement = r'Assert.That(\2, Is.EqualTo(\1))'
    content = re.sub(pattern, replacement, content)
    
    return content

def convert_assert_are_not_equal(content):
    """Convert Assert.AreNotEqual to Assert.That with Is.Not.EqualTo"""
    # With message
    pattern = r'Assert\.AreNotEqual\(\s*([^,]+?)\s*,\s*([^,]+?)\s*,\s*([^)]+?)\s*\)'
    replacement = r'Assert.That(\2, Is.Not.EqualTo(\1), \3)'
    content = re.sub(pattern, replacement, content)
    
    # Without message
    pattern = r'Assert\.AreNotEqual\(\s*([^,]+?)\s*,\s*([^,]+?)\s*\)'
    replacement = r'Assert.That(\2, Is.Not.EqualTo(\1))'
    content = re.sub(pattern, replacement, content)
    
    return content

def convert_assert_is_true(content):
    """Convert Assert.IsTrue to Assert.That with Is.True"""
    # With message (including format strings)
    pattern = r'Assert\.IsTrue\(\s*([^,]+?)\s*,\s*([^)]+?)\s*\)'
    replacement = r'Assert.That(\1, Is.True, \2)'
    content = re.sub(pattern, replacement, content)
    
    # Without message
    pattern = r'Assert\.IsTrue\(\s*([^)]+?)\s*\)'
    replacement = r'Assert.That(\1, Is.True)'
    content = re.sub(pattern, replacement, content)
    
    return content

def convert_assert_is_false(content):
    """Convert Assert.IsFalse to Assert.That with Is.False"""
    # With message
    pattern = r'Assert\.IsFalse\(\s*([^,]+?)\s*,\s*([^)]+?)\s*\)'
    replacement = r'Assert.That(\1, Is.False, \2)'
    content = re.sub(pattern, replacement, content)
    
    # Without message
    pattern = r'Assert\.IsFalse\(\s*([^)]+?)\s*\)'
    replacement = r'Assert.That(\1, Is.False)'
    content = re.sub(pattern, replacement, content)
    
    return content

def convert_assert_is_null(content):
    """Convert Assert.IsNull to Assert.That with Is.Null"""
    # With message
    pattern = r'Assert\.IsNull\(\s*([^,]+?)\s*,\s*([^)]+?)\s*\)'
    replacement = r'Assert.That(\1, Is.Null, \2)'
    content = re.sub(pattern, replacement, content)
    
    # Without message
    pattern = r'Assert\.IsNull\(\s*([^)]+?)\s*\)'
    replacement = r'Assert.That(\1, Is.Null)'
    content = re.sub(pattern, replacement, content)
    
    return content

def convert_assert_is_not_null(content):
    """Convert Assert.IsNotNull to Assert.That with Is.Not.Null"""
    # With message
    pattern = r'Assert\.IsNotNull\(\s*([^,]+?)\s*,\s*([^)]+?)\s*\)'
    replacement = r'Assert.That(\1, Is.Not.Null, \2)'
    content = re.sub(pattern, replacement, content)
    
    # Without message
    pattern = r'Assert\.IsNotNull\(\s*([^)]+?)\s*\)'
    replacement = r'Assert.That(\1, Is.Not.Null)'
    content = re.sub(pattern, replacement, content)
    
    return content

def convert_assert_not_null(content):
    """Convert Assert.NotNull to Assert.That with Is.Not.Null"""
    # With message
    pattern = r'Assert\.NotNull\(\s*([^,]+?)\s*,\s*([^)]+?)\s*\)'
    replacement = r'Assert.That(\1, Is.Not.Null, \2)'
    content = re.sub(pattern, replacement, content)
    
    # Without message
    pattern = r'Assert\.NotNull\(\s*([^)]+?)\s*\)'
    replacement = r'Assert.That(\1, Is.Not.Null)'
    content = re.sub(pattern, replacement, content)
    
    return content

def convert_assert_true(content):
    """Convert Assert.True to Assert.That with Is.True"""
    # With message
    pattern = r'Assert\.True\(\s*([^,]+?)\s*,\s*([^)]+?)\s*\)'
    replacement = r'Assert.That(\1, Is.True, \2)'
    content = re.sub(pattern, replacement, content)
    
    # Without message
    pattern = r'Assert\.True\(\s*([^)]+?)\s*\)'
    replacement = r'Assert.That(\1, Is.True)'
    content = re.sub(pattern, replacement, content)
    
    return content

def convert_assert_false(content):
    """Convert Assert.False to Assert.That with Is.False"""
    # With message
    pattern = r'Assert\.False\(\s*([^,]+?)\s*,\s*([^)]+?)\s*\)'
    replacement = r'Assert.That(\1, Is.False, \2)'
    content = re.sub(pattern, replacement, content)
    
    # Without message
    pattern = r'Assert\.False\(\s*([^)]+?)\s*\)'
    replacement = r'Assert.That(\1, Is.False)'
    content = re.sub(pattern, replacement, content)
    
    return content

def convert_assert_are_same(content):
    """Convert Assert.AreSame to Assert.That with Is.SameAs"""
    # With message
    pattern = r'Assert\.AreSame\(\s*([^,]+?)\s*,\s*([^,]+?)\s*,\s*([^)]+?)\s*\)'
    replacement = r'Assert.That(\2, Is.SameAs(\1), \3)'
    content = re.sub(pattern, replacement, content)
    
    # Without message
    pattern = r'Assert\.AreSame\(\s*([^,]+?)\s*,\s*([^,]+?)\s*\)'
    replacement = r'Assert.That(\2, Is.SameAs(\1))'
    content = re.sub(pattern, replacement, content)
    
    return content

def convert_assert_are_not_same(content):
    """Convert Assert.AreNotSame to Assert.That with Is.Not.SameAs"""
    # With message
    pattern = r'Assert\.AreNotSame\(\s*([^,]+?)\s*,\s*([^,]+?)\s*,\s*([^)]+?)\s*\)'
    replacement = r'Assert.That(\2, Is.Not.SameAs(\1), \3)'
    content = re.sub(pattern, replacement, content)
    
    # Without message
    pattern = r'Assert\.AreNotSame\(\s*([^,]+?)\s*,\s*([^,]+?)\s*\)'
    replacement = r'Assert.That(\2, Is.Not.SameAs(\1))'
    content = re.sub(pattern, replacement, content)
    
    return content

def convert_assert_greater(content):
    """Convert Assert.Greater to Assert.That with Is.GreaterThan"""
    # With message
    pattern = r'Assert\.Greater\(\s*([^,]+?)\s*,\s*([^,]+?)\s*,\s*([^)]+?)\s*\)'
    replacement = r'Assert.That(\1, Is.GreaterThan(\2), \3)'
    content = re.sub(pattern, replacement, content)
    
    # Without message
    pattern = r'Assert\.Greater\(\s*([^,]+?)\s*,\s*([^,]+?)\s*\)'
    replacement = r'Assert.That(\1, Is.GreaterThan(\2))'
    content = re.sub(pattern, replacement, content)
    
    return content

def convert_assert_greater_or_equal(content):
    """Convert Assert.GreaterOrEqual to Assert.That with Is.GreaterThanOrEqualTo"""
    # With message
    pattern = r'Assert\.GreaterOrEqual\(\s*([^,]+?)\s*,\s*([^,]+?)\s*,\s*([^)]+?)\s*\)'
    replacement = r'Assert.That(\1, Is.GreaterThanOrEqualTo(\2), \3)'
    content = re.sub(pattern, replacement, content)
    
    # Without message
    pattern = r'Assert\.GreaterOrEqual\(\s*([^,]+?)\s*,\s*([^,]+?)\s*\)'
    replacement = r'Assert.That(\1, Is.GreaterThanOrEqualTo(\2))'
    content = re.sub(pattern, replacement, content)
    
    return content

def convert_assert_less(content):
    """Convert Assert.Less to Assert.That with Is.LessThan"""
    # With message
    pattern = r'Assert\.Less\(\s*([^,]+?)\s*,\s*([^,]+?)\s*,\s*([^)]+?)\s*\)'
    replacement = r'Assert.That(\1, Is.LessThan(\2), \3)'
    content = re.sub(pattern, replacement, content)
    
    # Without message
    pattern = r'Assert\.Less\(\s*([^,]+?)\s*,\s*([^,]+?)\s*\)'
    replacement = r'Assert.That(\1, Is.LessThan(\2))'
    content = re.sub(pattern, replacement, content)
    
    return content

def convert_assert_less_or_equal(content):
    """Convert Assert.LessOrEqual to Assert.That with Is.LessThanOrEqualTo"""
    # With message
    pattern = r'Assert\.LessOrEqual\(\s*([^,]+?)\s*,\s*([^,]+?)\s*,\s*([^)]+?)\s*\)'
    replacement = r'Assert.That(\1, Is.LessThanOrEqualTo(\2), \3)'
    content = re.sub(pattern, replacement, content)
    
    # Without message
    pattern = r'Assert\.LessOrEqual\(\s*([^,]+?)\s*,\s*([^,]+?)\s*\)'
    replacement = r'Assert.That(\1, Is.LessThanOrEqualTo(\2))'
    content = re.sub(pattern, replacement, content)
    
    return content

def convert_assert_contains(content):
    """Convert Assert.Contains to Assert.That with Does.Contain"""
    # With message
    pattern = r'Assert\.Contains\(\s*([^,]+?)\s*,\s*([^,]+?)\s*,\s*([^)]+?)\s*\)'
    replacement = r'Assert.That(\2, Does.Contain(\1), \3)'
    content = re.sub(pattern, replacement, content)
    
    # Without message
    pattern = r'Assert\.Contains\(\s*([^,]+?)\s*,\s*([^,]+?)\s*\)'
    replacement = r'Assert.That(\2, Does.Contain(\1))'
    content = re.sub(pattern, replacement, content)
    
    return content

def convert_assert_is_empty(content):
    """Convert Assert.IsEmpty to Assert.That with Is.Empty"""
    # With message
    pattern = r'Assert\.IsEmpty\(\s*([^,]+?)\s*,\s*([^)]+?)\s*\)'
    replacement = r'Assert.That(\1, Is.Empty, \2)'
    content = re.sub(pattern, replacement, content)
    
    # Without message
    pattern = r'Assert\.IsEmpty\(\s*([^)]+?)\s*\)'
    replacement = r'Assert.That(\1, Is.Empty)'
    content = re.sub(pattern, replacement, content)
    
    return content

def convert_assert_is_not_empty(content):
    """Convert Assert.IsNotEmpty to Assert.That with Is.Not.Empty"""
    # With message
    pattern = r'Assert\.IsNotEmpty\(\s*([^,]+?)\s*,\s*([^)]+?)\s*\)'
    replacement = r'Assert.That(\1, Is.Not.Empty, \2)'
    content = re.sub(pattern, replacement, content)
    
    # Without message
    pattern = r'Assert\.IsNotEmpty\(\s*([^)]+?)\s*\)'
    replacement = r'Assert.That(\1, Is.Not.Empty)'
    content = re.sub(pattern, replacement, content)
    
    return content

def convert_string_assert(content):
    """Convert StringAssert methods to Assert.That with Does.Contain, Does.StartWith, etc."""
    # StringAssert.Contains
    pattern = r'StringAssert\.Contains\(\s*([^,]+?)\s*,\s*([^,]+?)\s*,\s*([^)]+?)\s*\)'
    replacement = r'Assert.That(\2, Does.Contain(\1), \3)'
    content = re.sub(pattern, replacement, content)
    
    pattern = r'StringAssert\.Contains\(\s*([^,]+?)\s*,\s*([^,]+?)\s*\)'
    replacement = r'Assert.That(\2, Does.Contain(\1))'
    content = re.sub(pattern, replacement, content)
    
    # StringAssert.StartsWith
    pattern = r'StringAssert\.StartsWith\(\s*([^,]+?)\s*,\s*([^,]+?)\s*,\s*([^)]+?)\s*\)'
    replacement = r'Assert.That(\2, Does.StartWith(\1), \3)'
    content = re.sub(pattern, replacement, content)
    
    pattern = r'StringAssert\.StartsWith\(\s*([^,]+?)\s*,\s*([^,]+?)\s*\)'
    replacement = r'Assert.That(\2, Does.StartWith(\1))'
    content = re.sub(pattern, replacement, content)
    
    # StringAssert.EndsWith
    pattern = r'StringAssert\.EndsWith\(\s*([^,]+?)\s*,\s*([^,]+?)\s*,\s*([^)]+?)\s*\)'
    replacement = r'Assert.That(\2, Does.EndWith(\1), \3)'
    content = re.sub(pattern, replacement, content)
    
    pattern = r'StringAssert\.EndsWith\(\s*([^,]+?)\s*,\s*([^,]+?)\s*\)'
    replacement = r'Assert.That(\2, Does.EndWith(\1))'
    content = re.sub(pattern, replacement, content)
    
    # StringAssert.AreEqualIgnoringCase
    pattern = r'StringAssert\.AreEqualIgnoringCase\(\s*([^,]+?)\s*,\s*([^,]+?)\s*,\s*([^)]+?)\s*\)'
    replacement = r'Assert.That(\2, Is.EqualTo(\1).IgnoreCase, \3)'
    content = re.sub(pattern, replacement, content)
    
    pattern = r'StringAssert\.AreEqualIgnoringCase\(\s*([^,]+?)\s*,\s*([^,]+?)\s*\)'
    replacement = r'Assert.That(\2, Is.EqualTo(\1).IgnoreCase)'
    content = re.sub(pattern, replacement, content)
    
    return content

def convert_collection_assert(content):
    """Convert CollectionAssert methods to Assert.That"""
    # CollectionAssert.AreEqual
    pattern = r'CollectionAssert\.AreEqual\(\s*([^,]+?)\s*,\s*([^,]+?)\s*,\s*([^)]+?)\s*\)'
    replacement = r'Assert.That(\2, Is.EqualTo(\1), \3)'
    content = re.sub(pattern, replacement, content)
    
    pattern = r'CollectionAssert\.AreEqual\(\s*([^,]+?)\s*,\s*([^,]+?)\s*\)'
    replacement = r'Assert.That(\2, Is.EqualTo(\1))'
    content = re.sub(pattern, replacement, content)
    
    # CollectionAssert.AreNotEqual
    pattern = r'CollectionAssert\.AreNotEqual\(\s*([^,]+?)\s*,\s*([^,]+?)\s*,\s*([^)]+?)\s*\)'
    replacement = r'Assert.That(\2, Is.Not.EqualTo(\1), \3)'
    content = re.sub(pattern, replacement, content)
    
    pattern = r'CollectionAssert\.AreNotEqual\(\s*([^,]+?)\s*,\s*([^,]+?)\s*\)'
    replacement = r'Assert.That(\2, Is.Not.EqualTo(\1))'
    content = re.sub(pattern, replacement, content)
    
    # CollectionAssert.Contains
    pattern = r'CollectionAssert\.Contains\(\s*([^,]+?)\s*,\s*([^,]+?)\s*,\s*([^)]+?)\s*\)'
    replacement = r'Assert.That(\1, Does.Contain(\2), \3)'
    content = re.sub(pattern, replacement, content)
    
    pattern = r'CollectionAssert\.Contains\(\s*([^,]+?)\s*,\s*([^,]+?)\s*\)'
    replacement = r'Assert.That(\1, Does.Contain(\2))'
    content = re.sub(pattern, replacement, content)
    
    # CollectionAssert.DoesNotContain
    pattern = r'CollectionAssert\.DoesNotContain\(\s*([^,]+?)\s*,\s*([^,]+?)\s*,\s*([^)]+?)\s*\)'
    replacement = r'Assert.That(\1, Does.Not.Contain(\2), \3)'
    content = re.sub(pattern, replacement, content)
    
    pattern = r'CollectionAssert\.DoesNotContain\(\s*([^,]+?)\s*,\s*([^,]+?)\s*\)'
    replacement = r'Assert.That(\1, Does.Not.Contain(\2))'
    content = re.sub(pattern, replacement, content)
    
    # CollectionAssert.IsEmpty
    pattern = r'CollectionAssert\.IsEmpty\(\s*([^,]+?)\s*,\s*([^)]+?)\s*\)'
    replacement = r'Assert.That(\1, Is.Empty, \2)'
    content = re.sub(pattern, replacement, content)
    
    pattern = r'CollectionAssert\.IsEmpty\(\s*([^)]+?)\s*\)'
    replacement = r'Assert.That(\1, Is.Empty)'
    content = re.sub(pattern, replacement, content)
    
    # CollectionAssert.IsNotEmpty
    pattern = r'CollectionAssert\.IsNotEmpty\(\s*([^,]+?)\s*,\s*([^)]+?)\s*\)'
    replacement = r'Assert.That(\1, Is.Not.Empty, \2)'
    content = re.sub(pattern, replacement, content)
    
    pattern = r'CollectionAssert\.IsNotEmpty\(\s*([^)]+?)\s*\)'
    replacement = r'Assert.That(\1, Is.Not.Empty)'
    content = re.sub(pattern, replacement, content)
    
    return content

def convert_assert_throws(content):
    """Convert Assert.Throws to Assert.That with Throws"""
    # Note: Assert.Throws already uses the correct syntax in NUnit 4
    # But we need to handle cases like Assert.Throws<Exception>(() => { ... })
    # These are already compatible, so no changes needed
    return content

def convert_file(file_path, dry_run=False):
    """Convert a single file from NUnit 3 to NUnit 4 syntax."""
    try:
        with open(file_path, 'r', encoding='utf-8') as f:
            original_content = f.read()
        
        # Skip if file doesn't use Assert
        if 'Assert.' not in original_content and 'StringAssert.' not in original_content and 'CollectionAssert.' not in original_content:
            return False
        
        content = original_content
        
        # Apply conversions in order (more specific patterns first)
        content = convert_assert_are_not_equal(content)
        content = convert_assert_are_equal(content)
        content = convert_assert_are_not_same(content)
        content = convert_assert_are_same(content)
        content = convert_assert_is_not_null(content)
        content = convert_assert_is_null(content)
        content = convert_assert_not_null(content)
        content = convert_assert_is_false(content)
        content = convert_assert_is_true(content)
        content = convert_assert_false(content)
        content = convert_assert_true(content)
        content = convert_assert_greater_or_equal(content)
        content = convert_assert_greater(content)
        content = convert_assert_less_or_equal(content)
        content = convert_assert_less(content)
        content = convert_assert_contains(content)
        content = convert_assert_is_not_empty(content)
        content = convert_assert_is_empty(content)
        content = convert_string_assert(content)
        content = convert_collection_assert(content)
        content = convert_assert_throws(content)
        
        # Only write if content changed
        if content != original_content:
            if dry_run:
                print(f"Would convert: {file_path}")
                return True
            else:
                with open(file_path, 'w', encoding='utf-8') as f:
                    f.write(content)
                print(f"Converted: {file_path}")
                return True
        
        return False
    
    except Exception as e:
        print(f"Error processing {file_path}: {e}", file=sys.stderr)
        return False

def main():
    """Main function to convert files."""
    import argparse
    
    parser = argparse.ArgumentParser(description='Convert NUnit 3 assertions to NUnit 4 syntax')
    parser.add_argument('paths', nargs='*', help='Files or directories to convert')
    parser.add_argument('--dry-run', action='store_true', help='Show what would be converted without making changes')
    parser.add_argument('--recursive', action='store_true', help='Recursively process directories')
    
    args = parser.parse_args()
    
    if not args.paths:
        print("Usage: convert_nunit.py [--dry-run] [--recursive] <file_or_directory> [...]")
        sys.exit(1)
    
    files_to_process = []
    
    for path_str in args.paths:
        path = Path(path_str)
        if path.is_file():
            if path.suffix == '.cs':
                files_to_process.append(path)
        elif path.is_dir():
            if args.recursive:
                files_to_process.extend(path.rglob('*.cs'))
            else:
                files_to_process.extend(path.glob('*.cs'))
    
    # Filter out obj and bin directories
    files_to_process = [f for f in files_to_process if '/obj/' not in str(f) and '/bin/' not in str(f) and '/.git/' not in str(f)]
    
    converted_count = 0
    for file_path in files_to_process:
        if convert_file(file_path, args.dry_run):
            converted_count += 1
    
    print(f"\nProcessed {len(files_to_process)} files, converted {converted_count} files.")

if __name__ == '__main__':
    main()
