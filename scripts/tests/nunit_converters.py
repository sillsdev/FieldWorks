#!/usr/bin/env python3
"""
NUnit assertion converters.

Converts NUnit 3 classic assertions to NUnit 4 constraint model.
"""
from __future__ import annotations

import re
from typing import Callable, List, Optional, Tuple

from .nunit_parsing import (
    extract_named_argument,
    is_constant_expression,
    is_string_format_call,
    is_string_literal,
    looks_like_tolerance,
    split_args,
)


def convert_format_to_interpolation(format_str: str, args: List[str]) -> str:
    """Convert format string with args to C# interpolated string."""
    # Handle String.Format case
    if is_string_format_call(format_str):
        match = re.match(r'[Ss]tring\.Format\s*\(\s*(.+)\s*\)', format_str, re.DOTALL)
        if match:
            inner_args = split_args(match.group(1))
            if inner_args:
                format_str = inner_args[0]
                args = inner_args[1:] + args

    # Remove quotes from format string
    format_str = format_str.strip()
    if format_str.startswith('@"') and format_str.endswith('"'):
        inner = format_str[2:-1].replace('""', '"')
        is_verbatim = True
    elif format_str.startswith('"') and format_str.endswith('"'):
        inner = format_str[1:-1]
        is_verbatim = False
    else:
        # Can't convert, return as-is with string.Format
        if args:
            return f"string.Format({format_str}, {', '.join(args)})"
        return format_str

    # Replace {0}, {1}, etc. with the actual arguments
    result = inner
    for i, arg in enumerate(args):
        arg_stripped = arg.strip()

        # If the argument is a string literal, inline its value directly (without quotes)
        if is_string_literal(arg_stripped):
            if arg_stripped.startswith('@"') and arg_stripped.endswith('"'):
                # Verbatim string literal
                literal_value = arg_stripped[2:-1].replace('""', '"')
            elif arg_stripped.startswith('"') and arg_stripped.endswith('"'):
                # Regular string literal - unescape
                literal_value = arg_stripped[1:-1].replace('\\"', '"').replace('\\\\', '\\')
            else:
                literal_value = arg_stripped

            result = result.replace('{' + str(i) + '}', literal_value)
            # Handle format specifiers like {0:X4} - for string literals, drop the format
            result = re.sub(r'\{' + str(i) + r':[^}]+\}', literal_value, result)
        else:
            # Non-literal - wrap in interpolation braces
            result = result.replace('{' + str(i) + '}', '{' + arg_stripped + '}')
            # Handle format specifiers like {0:X4}
            result = re.sub(r'\{' + str(i) + r':([^}]+)\}', '{' + arg_stripped + r':\1}', result)

    # Check if result contains any interpolation expressions
    has_interpolation = bool(re.search(r'\{[^}]+\}', result))

    if has_interpolation:
        if is_verbatim:
            return f'$@"{result}"'
        else:
            return f'$"{result}"'
    else:
        # No interpolation needed, return regular string
        if is_verbatim:
            return f'@"{result}"'
        else:
            return f'"{result}"'


def _build_suffix(extras: List[str]) -> str:
    """Build message suffix from extra arguments."""
    if not extras:
        return ""
    if len(extras) > 1 and is_string_literal(extras[0]):
        return ", " + convert_format_to_interpolation(extras[0], extras[1:])
    return ", " + ", ".join(extras)


def convert_are_equal(args_str: str, original: str) -> Optional[str]:
    """Convert Assert.AreEqual to Assert.That with Is.EqualTo."""
    args = split_args(args_str)
    if len(args) < 2:
        return None

    expected = None
    actual = None
    other_args: List[str] = []

    for arg in args:
        named_expected = extract_named_argument(arg, "expected")
        named_actual = extract_named_argument(arg, "actual")
        if named_expected is not None and expected is None:
            expected = named_expected
            continue
        if named_actual is not None and actual is None:
            actual = named_actual
            continue
        other_args.append(arg.strip())

    positional = other_args.copy()

    if expected is None and positional:
        expected = positional.pop(0).strip()
    if actual is None and positional:
        actual = positional.pop(0).strip()

    if expected is None or actual is None:
        return None

    if is_constant_expression(actual) and not is_constant_expression(expected):
        actual, expected = expected, actual

    remaining = positional
    constraint = f"Is.EqualTo({expected})"

    if remaining:
        first = remaining[0]
        if looks_like_tolerance(first):
            constraint += f".Within({first})"
            remaining = remaining[1:]

    suffix = _build_suffix(remaining)
    return f"Assert.That({actual}, {constraint}{suffix})"


def convert_are_not_equal(args_str: str, original: str) -> Optional[str]:
    """Convert Assert.AreNotEqual to Assert.That with Is.Not.EqualTo."""
    args = split_args(args_str)
    if len(args) < 2:
        return None

    expected = None
    actual = None
    other_args: List[str] = []

    for arg in args:
        named_expected = extract_named_argument(arg, "expected")
        named_actual = extract_named_argument(arg, "actual")
        if named_expected is not None and expected is None:
            expected = named_expected
            continue
        if named_actual is not None and actual is None:
            actual = named_actual
            continue
        other_args.append(arg.strip())

    positional = other_args.copy()

    if expected is None and positional:
        expected = positional.pop(0).strip()
    if actual is None and positional:
        actual = positional.pop(0).strip()

    if expected is None or actual is None:
        return None

    if is_constant_expression(actual) and not is_constant_expression(expected):
        actual, expected = expected, actual

    remaining = positional
    constraint = f"Is.Not.EqualTo({expected})"

    if remaining:
        first = remaining[0]
        if looks_like_tolerance(first):
            constraint += f".Within({first})"
            remaining = remaining[1:]

    suffix = _build_suffix(remaining)
    return f"Assert.That({actual}, {constraint}{suffix})"


def convert_simple_predicate(
    args_str: str, original: str, predicate: str
) -> Optional[str]:
    """Convert simple assertion to Assert.That with predicate."""
    args = split_args(args_str)
    if not args:
        return None
    expression = args[0]
    suffix = _build_suffix(args[1:])
    return f"Assert.That({expression}, {predicate}{suffix})"


def convert_is_null(args_str: str, original: str) -> Optional[str]:
    return convert_simple_predicate(args_str, original, "Is.Null")


def convert_is_not_null(args_str: str, original: str) -> Optional[str]:
    return convert_simple_predicate(args_str, original, "Is.Not.Null")


def convert_is_true(args_str: str, original: str) -> Optional[str]:
    return convert_simple_predicate(args_str, original, "Is.True")


def convert_is_false(args_str: str, original: str) -> Optional[str]:
    return convert_simple_predicate(args_str, original, "Is.False")


def convert_is_empty(args_str: str, original: str) -> Optional[str]:
    return convert_simple_predicate(args_str, original, "Is.Empty")


def convert_is_not_empty(args_str: str, original: str) -> Optional[str]:
    return convert_simple_predicate(args_str, original, "Is.Not.Empty")


def convert_true(args_str: str, original: str) -> Optional[str]:
    return convert_simple_predicate(args_str, original, "Is.True")


def convert_false(args_str: str, original: str) -> Optional[str]:
    return convert_simple_predicate(args_str, original, "Is.False")


def _convert_comparison(args_str: str, constraint_template: str) -> Optional[str]:
    """Helper for comparison assertions (Less, Greater, etc.)."""
    args = split_args(args_str)
    if len(args) < 2:
        return None
    actual = args[0].strip()
    expected = args[1].strip()
    suffix = _build_suffix(args[2:])
    constraint = constraint_template.format(expected=expected)
    return f"Assert.That({actual}, {constraint}{suffix})"


def convert_less(args_str: str, original: str) -> Optional[str]:
    return _convert_comparison(args_str, "Is.LessThan({expected})")


def convert_less_or_equal(args_str: str, original: str) -> Optional[str]:
    return _convert_comparison(args_str, "Is.LessThanOrEqualTo({expected})")


def convert_greater(args_str: str, original: str) -> Optional[str]:
    return _convert_comparison(args_str, "Is.GreaterThan({expected})")


def convert_greater_or_equal(args_str: str, original: str) -> Optional[str]:
    return _convert_comparison(args_str, "Is.GreaterThanOrEqualTo({expected})")


def _convert_collection_check(
    args_str: str, constraint_template: str, swap_args: bool = False
) -> Optional[str]:
    """Helper for collection assertions."""
    args = split_args(args_str)
    if len(args) < 2:
        return None
    first = args[0].strip()
    second = args[1].strip()
    if swap_args:
        actual, expected = second, first
    else:
        actual, expected = first, second
    suffix = _build_suffix(args[2:])
    constraint = constraint_template.format(expected=expected)
    return f"Assert.That({actual}, {constraint}{suffix})"


def convert_contains(args_str: str, original: str) -> Optional[str]:
    return _convert_collection_check(args_str, "Does.Contain({expected})", swap_args=True)


def convert_does_not_contain(args_str: str, original: str) -> Optional[str]:
    return _convert_collection_check(args_str, "Does.Not.Contain({expected})", swap_args=True)


def convert_are_same(args_str: str, original: str) -> Optional[str]:
    return _convert_collection_check(args_str, "Is.SameAs({expected})", swap_args=True)


def convert_are_not_same(args_str: str, original: str) -> Optional[str]:
    return _convert_collection_check(args_str, "Is.Not.SameAs({expected})", swap_args=True)


def convert_is_instance_of(args_str: str, original: str) -> Optional[str]:
    """Convert Assert.IsInstanceOf to Assert.That with Is.InstanceOf.

    Handles both forms:
    - Assert.IsInstanceOf(typeof(T), actual)
    - Assert.IsInstanceOf<T>(actual)  -> args_str will be "<T>, actual"
    """
    args = split_args(args_str)
    if len(args) < 1:
        return None

    # Check if first arg is a generic type parameter like "<ToolStrip>"
    first_arg = args[0].strip()
    if first_arg.startswith('<') and first_arg.endswith('>'):
        # Generic form: Assert.IsInstanceOf<T>(actual)
        type_param = first_arg  # Keep as <T>
        if len(args) < 2:
            return None
        actual = args[1].strip()
        suffix = _build_suffix(args[2:])
        return f"Assert.That({actual}, Is.InstanceOf{type_param}(){suffix})"
    elif len(args) >= 2:
        # Non-generic form: Assert.IsInstanceOf(typeof(T), actual)
        expected_type = first_arg
        actual = args[1].strip()
        suffix = _build_suffix(args[2:])
        return f"Assert.That({actual}, Is.InstanceOf({expected_type}){suffix})"

    return None


def no_conversion(args_str: str, original: str) -> Optional[str]:
    """Return original unchanged (for already-compatible methods)."""
    return original


def convert_assert_fail(args_str: str, original: str) -> Optional[str]:
    """Convert Assert.Fail with format string to use interpolation."""
    args = split_args(args_str)
    if not args or len(args) == 1:
        return original
    if is_string_literal(args[0]) and len(args) > 1:
        interpolated = convert_format_to_interpolation(args[0], args[1:])
        return f"Assert.Fail({interpolated})"
    return original


# StringAssert converters
def convert_string_assert_contains(args_str: str, original: str) -> Optional[str]:
    return _convert_collection_check(args_str, "Does.Contain({expected})", swap_args=True)


def convert_string_assert_does_not_contain(args_str: str, original: str) -> Optional[str]:
    return _convert_collection_check(args_str, "Does.Not.Contain({expected})", swap_args=True)


def convert_string_assert_starts_with(args_str: str, original: str) -> Optional[str]:
    return _convert_collection_check(args_str, "Does.StartWith({expected})", swap_args=True)


def convert_string_assert_ends_with(args_str: str, original: str) -> Optional[str]:
    return _convert_collection_check(args_str, "Does.EndWith({expected})", swap_args=True)


# CollectionAssert converters
def convert_collection_assert_are_equal(args_str: str, original: str) -> Optional[str]:
    return _convert_collection_check(args_str, "Is.EqualTo({expected})", swap_args=True)


def convert_collection_assert_are_equivalent(args_str: str, original: str) -> Optional[str]:
    return _convert_collection_check(args_str, "Is.EquivalentTo({expected})", swap_args=True)


def convert_collection_assert_contains(args_str: str, original: str) -> Optional[str]:
    return _convert_collection_check(args_str, "Does.Contain({expected})", swap_args=True)


def convert_collection_assert_does_not_contain(args_str: str, original: str) -> Optional[str]:
    return _convert_collection_check(args_str, "Does.Not.Contain({expected})", swap_args=True)


def convert_collection_assert_is_empty(args_str: str, original: str) -> Optional[str]:
    return convert_simple_predicate(args_str, original, "Is.Empty")


def convert_collection_assert_is_not_empty(args_str: str, original: str) -> Optional[str]:
    return convert_simple_predicate(args_str, original, "Is.Not.Empty")


def convert_collection_assert_all_items_are_unique(args_str: str, original: str) -> Optional[str]:
    return convert_simple_predicate(args_str, original, "Is.Unique")


def convert_collection_assert_is_subset_of(args_str: str, original: str) -> Optional[str]:
    args = split_args(args_str)
    if len(args) < 2:
        return None
    subset = args[0].strip()
    superset = args[1].strip()
    suffix = _build_suffix(args[2:])
    return f"Assert.That({subset}, Is.SubsetOf({superset}){suffix})"


# FileAssert converters
def convert_file_assert_are_equal(args_str: str, original: str) -> Optional[str]:
    return _convert_collection_check(args_str, "Is.EqualTo({expected})", swap_args=True)


# Converter registry
CONVERTERS: List[Tuple[str, Callable[[str, str], Optional[str]]]] = [
    # StringAssert - must come before Assert to avoid partial matches
    ("StringAssert.Contains", convert_string_assert_contains),
    ("StringAssert.DoesNotContain", convert_string_assert_does_not_contain),
    ("StringAssert.StartsWith", convert_string_assert_starts_with),
    ("StringAssert.EndsWith", convert_string_assert_ends_with),
    # CollectionAssert - must come before Assert to avoid partial matches
    ("CollectionAssert.AreEqual", convert_collection_assert_are_equal),
    ("CollectionAssert.AreEquivalent", convert_collection_assert_are_equivalent),
    ("CollectionAssert.Contains", convert_collection_assert_contains),
    ("CollectionAssert.DoesNotContain", convert_collection_assert_does_not_contain),
    ("CollectionAssert.IsEmpty", convert_collection_assert_is_empty),
    ("CollectionAssert.IsNotEmpty", convert_collection_assert_is_not_empty),
    ("CollectionAssert.AllItemsAreUnique", convert_collection_assert_all_items_are_unique),
    ("CollectionAssert.IsSubsetOf", convert_collection_assert_is_subset_of),
    # FileAssert - must come before Assert to avoid partial matches
    ("FileAssert.AreEqual", convert_file_assert_are_equal),
    # Assert methods that need conversion
    ("Assert.AreEqual", convert_are_equal),
    ("Assert.AreNotEqual", convert_are_not_equal),
    ("Assert.AreSame", convert_are_same),
    ("Assert.AreNotSame", convert_are_not_same),
    ("Assert.Contains", convert_contains),
    ("Assert.DoesNotContain", convert_does_not_contain),
    ("Assert.Greater", convert_greater),
    ("Assert.GreaterOrEqual", convert_greater_or_equal),
    ("Assert.IsEmpty", convert_is_empty),
    ("Assert.IsFalse", convert_is_false),
    ("Assert.IsInstanceOf", convert_is_instance_of),
    ("Assert.IsNotEmpty", convert_is_not_empty),
    ("Assert.IsNotNull", convert_is_not_null),
    ("Assert.IsNull", convert_is_null),
    ("Assert.IsTrue", convert_is_true),
    ("Assert.Less", convert_less),
    ("Assert.LessOrEqual", convert_less_or_equal),
    # Assert.NotNull and Assert.Null are aliases
    ("Assert.NotNull", convert_is_not_null),
    ("Assert.Null", convert_is_null),
    ("Assert.True", convert_true),
    ("Assert.False", convert_false),
    # Assert.Fail needs special handling for format strings
    ("Assert.Fail", convert_assert_fail),
    # Assert methods that don't need conversion (already NUnit 4 compatible)
    ("Assert.Throws", no_conversion),
    ("Assert.Catch", no_conversion),
    ("Assert.DoesNotThrow", no_conversion),
    ("Assert.Ignore", no_conversion),
    ("Assert.Pass", no_conversion),
    ("Assert.Inconclusive", no_conversion),
]
