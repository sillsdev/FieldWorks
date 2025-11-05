#!/usr/bin/env python3
"""
NUnit 3 to NUnit 4 Conversion Script
Converts old-style NUnit assertions to the new Assert.That() syntax.
"""

import re
import sys
import os
from pathlib import Path

# Helper function to find matching parenthesis
def find_matching_paren(text, start_idx):
    """Find the index of the closing paren matching the opening paren at start_idx."""
    count = 1
    i = start_idx + 1
    in_string = False
    in_char = False
    escape = False
    
    while i < len(text):
        ch = text[i]
        
        if escape:
            escape = False
            i += 1
            continue
        
        if ch == '\\':
            escape = True
            i += 1
            continue
        
        if ch == '"' and not in_char:
            in_string = not in_string
        elif ch == "'" and not in_string:
            in_char = not in_char
        elif not in_string and not in_char:
            if ch == '(':
                count += 1
            elif ch == ')':
                count -= 1
                if count == 0:
                    return i
        
        i += 1
    
    return -1

def split_args(args_text):
    """Split comma-separated arguments, respecting nested parentheses and strings."""
    args = []
    current = []
    depth = 0
    in_string = False
    in_char = False
    escape = False
    
    for ch in args_text:
        if escape:
            current.append(ch)
            escape = False
            continue
        
        if ch == '\\':
            current.append(ch)
            escape = True
            continue
        
        if ch == '"' and not in_char:
            in_string = not in_string
            current.append(ch)
        elif ch == "'" and not in_string:
            in_char = not in_char
            current.append(ch)
        elif not in_string and not in_char:
            if ch == '(':
                depth += 1
                current.append(ch)
            elif ch == ')':
                depth -= 1
                current.append(ch)
            elif ch == ',' and depth == 0:
                args.append(''.join(current).strip())
                current = []
            else:
                current.append(ch)
        else:
            current.append(ch)
    
    if current:
        args.append(''.join(current).strip())
    
    return args

def convert_simple_assert(content, old_method, constraint, has_message_format=True):
    """
    Generic converter for simple Assert methods.
    old_method: e.g., 'IsTrue', 'IsFalse', 'IsNull'
    constraint: e.g., 'Is.True', 'Is.False', 'Is.Null'
    has_message_format: if True, looks for format string messages
    """
    result = []
    pattern = f'Assert\\.{old_method}\\s*\\('
    pos = 0
    
    while pos < len(content):
        match = re.search(pattern, content[pos:])
        if not match:
            result.append(content[pos:])
            break
        
        # Add text before match
        result.append(content[pos:pos + match.start()])
        
        # Find closing paren
        open_paren_idx = pos + match.end() - 1
        close_paren_idx = find_matching_paren(content, open_paren_idx)
        
        if close_paren_idx == -1:
            result.append(match.group(0))
            pos = pos + match.end()
            continue
        
        # Extract arguments
        args_text = content[open_paren_idx + 1:close_paren_idx]
        args = split_args(args_text)
        
        # Convert based on arguments
        if len(args) == 1:
            # Simple: Assert.IsTrue(condition)
            result.append(f'Assert.That({args[0]}, {constraint})')
        elif len(args) >= 2:
            # With message: Assert.IsTrue(condition, message, ...)
            message_parts = ', '.join(args[1:])
            result.append(f'Assert.That({args[0]}, {constraint}, {message_parts})')
        else:
            # Malformed, keep original
            result.append(content[pos + match.start():close_paren_idx + 1])
        
        pos = close_paren_idx + 1
    
    return ''.join(result)

def convert_comparison_assert(content, old_method, constraint_template):
    """
    Generic converter for comparison Assert methods (AreEqual, Greater, etc.).
    old_method: e.g., 'AreEqual', 'Greater'
    constraint_template: e.g., 'Is.EqualTo({0})', 'Is.GreaterThan({0})'
    """
    result = []
    pattern = f'Assert\\.{old_method}\\s*\\('
    pos = 0
    
    while pos < len(content):
        match = re.search(pattern, content[pos:])
        if not match:
            result.append(content[pos:])
            break
        
        # Add text before match
        result.append(content[pos:pos + match.start()])
        
        # Find closing paren
        open_paren_idx = pos + match.end() - 1
        close_paren_idx = find_matching_paren(content, open_paren_idx)
        
        if close_paren_idx == -1:
            result.append(match.group(0))
            pos = pos + match.end()
            continue
        
        # Extract arguments
        args_text = content[open_paren_idx + 1:close_paren_idx]
        args = split_args(args_text)
        
        # Convert based on arguments
        if len(args) == 2:
            # Assert.AreEqual(expected, actual) or Assert.Greater(arg1, arg2)
            constraint = constraint_template.format(args[0])
            result.append(f'Assert.That({args[1]}, {constraint})')
        elif len(args) >= 3:
            # With message
            constraint = constraint_template.format(args[0])
            message_parts = ', '.join(args[2:])
            result.append(f'Assert.That({args[1]}, {constraint}, {message_parts})')
        else:
            # Malformed, keep original
            result.append(content[pos + match.start():close_paren_idx + 1])
        
        pos = close_paren_idx + 1
    
    return ''.join(result)

def convert_does_not_throw(content):
    """Convert Assert.DoesNotThrow to Assert.That with Throws.Nothing"""
    result = []
    pattern = r'Assert\.DoesNotThrow\s*\('
    pos = 0
    
    while pos < len(content):
        match = re.search(pattern, content[pos:])
        if not match:
            result.append(content[pos:])
            break
        
        # Add text before match
        result.append(content[pos:pos + match.start()])
        
        # Find closing paren
        open_paren_idx = pos + match.end() - 1
        close_paren_idx = find_matching_paren(content, open_paren_idx)
        
        if close_paren_idx == -1:
            result.append(match.group(0))
            pos = pos + match.end()
            continue
        
        # Extract the delegate/lambda
        delegate = content[open_paren_idx + 1:close_paren_idx].strip()
        
        # Convert to Assert.That
        result.append(f'Assert.That({delegate}, Throws.Nothing)')
        
        pos = close_paren_idx + 1
    
    return ''.join(result)

def convert_string_assert(content):
    """Convert StringAssert methods to Assert.That"""
    # StringAssert.Contains
    content = convert_comparison_assert(content, 'StringAssert\\.Contains', 'Does.Contain({0})')
    # StringAssert.StartsWith  
    content = convert_comparison_assert(content, 'StringAssert\\.StartsWith', 'Does.StartWith({0})')
    # StringAssert.EndsWith
    content = convert_comparison_assert(content, 'StringAssert\\.EndsWith', 'Does.EndWith({0})')
    # StringAssert.AreEqualIgnoringCase
    content = convert_comparison_assert(content, 'StringAssert\\.AreEqualIgnoringCase', 'Is.EqualTo({0}).IgnoreCase')
    return content

def convert_collection_assert(content):
    """Convert CollectionAssert methods to Assert.That"""
    # CollectionAssert.AreEqual
    content = convert_comparison_assert(content, 'CollectionAssert\\.AreEqual', 'Is.EqualTo({0})')
    # CollectionAssert.AreNotEqual
    content = convert_comparison_assert(content, 'CollectionAssert\\.AreNotEqual', 'Is.Not.EqualTo({0})')
    # CollectionAssert.Contains
    content = convert_comparison_assert(content, 'CollectionAssert\\.Contains', 'Does.Contain({0})')
    # CollectionAssert.DoesNotContain
    content = convert_comparison_assert(content, 'CollectionAssert\\.DoesNotContain', 'Does.Not.Contain({0})')
    # CollectionAssert.AllItemsAreUnique
    content = convert_simple_assert(content, 'CollectionAssert\\.AllItemsAreUnique', 'Is.Unique')
    # CollectionAssert.IsEmpty
    content = convert_simple_assert(content, 'CollectionAssert\\.IsEmpty', 'Is.Empty')
    # CollectionAssert.IsNotEmpty
    content = convert_simple_assert(content, 'CollectionAssert\\.IsNotEmpty', 'Is.Not.Empty')
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
        
        # Apply conversions in order - use helper functions
        # Comparison asserts (with two arguments + optional message)
        content = convert_comparison_assert(content, 'AreNotEqual', 'Is.Not.EqualTo({0})')
        content = convert_comparison_assert(content, 'AreEqual', 'Is.EqualTo({0})')
        content = convert_comparison_assert(content, 'AreNotSame', 'Is.Not.SameAs({0})')
        content = convert_comparison_assert(content, 'AreSame', 'Is.SameAs({0})')
        content = convert_comparison_assert(content, 'GreaterOrEqual', 'Is.GreaterThanOrEqualTo({0})')
        content = convert_comparison_assert(content, 'Greater', 'Is.GreaterThan({0})')
        content = convert_comparison_assert(content, 'LessOrEqual', 'Is.LessThanOrEqualTo({0})')
        content = convert_comparison_assert(content, 'Less', 'Is.LessThan({0})')
        content = convert_comparison_assert(content, 'Contains', 'Does.Contain({0})')
        
        # IsInstanceOf - special handling
        content = convert_comparison_assert(content, 'IsInstanceOf', 'Is.InstanceOf({0})')
        
        # Simple asserts (with one argument + optional message)
        content = convert_simple_assert(content, 'IsNotNull', 'Is.Not.Null')
        content = convert_simple_assert(content, 'IsNull', 'Is.Null')
        content = convert_simple_assert(content, 'NotNull', 'Is.Not.Null')
        content = convert_simple_assert(content, 'Null', 'Is.Null')
        content = convert_simple_assert(content, 'IsFalse', 'Is.False')
        content = convert_simple_assert(content, 'IsTrue', 'Is.True')
        content = convert_simple_assert(content, 'False', 'Is.False')
        content = convert_simple_assert(content, 'True', 'Is.True')
        content = convert_simple_assert(content, 'IsNotEmpty', 'Is.Not.Empty')
        content = convert_simple_assert(content, 'IsEmpty', 'Is.Empty')
        
        # DoesNotThrow - convert to Assert.That with Throws.Nothing
        content = convert_does_not_throw(content)
        
        # String and Collection asserts
        content = convert_string_assert(content)
        content = convert_collection_assert(content)
        
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
