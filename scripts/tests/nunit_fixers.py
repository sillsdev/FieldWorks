#!/usr/bin/env python3
"""
NUnit 4 breaking change fixers.

Fixes patterns that work in NUnit 3 but break in NUnit 4:
- .Within(message) patterns
- Assert.That with format string params
"""
from __future__ import annotations

import re
from typing import List, Tuple, Optional

from .nunit_parsing import is_string_literal, split_args, find_matching_paren
from .nunit_converters import convert_format_to_interpolation


def _find_within_call(content: str, start_pos: int) -> Optional[Tuple[int, int, str]]:
    """
    Find a .Within(...) call starting from start_pos.

    Returns (start_of_within, end_of_within, argument) or None.
    """
    # Look for .Within(
    within_match = re.search(r'\.Within\s*\(', content[start_pos:])
    if not within_match:
        return None

    within_start = start_pos + within_match.start()
    paren_start = start_pos + within_match.end() - 1  # Position of (

    # Find the matching )
    paren_end = find_matching_paren(content, paren_start)
    if paren_end < 0:
        return None

    argument = content[paren_start + 1:paren_end].strip()
    return (within_start, paren_end + 1, argument)


def fix_within_with_message(content: str) -> str:
    """
    Fix .Within(String.Format(...)) and .Within("message") patterns.

    In NUnit 4, .Within() only accepts numeric tolerance values.
    Messages should be passed as the third argument to Assert.That().

    Converts:
        Assert.That(x, Is.EqualTo(y).Within(String.Format("msg {0}", z)))
    To:
        Assert.That(x, Is.EqualTo(y), $"msg {z}")

    And:
        Assert.That(x, Is.EqualTo(y).Within("message"))
    To:
        Assert.That(x, Is.EqualTo(y), "message")
    """
    result = []
    pos = 0

    while pos < len(content):
        # Find next Assert.That
        assert_match = re.search(r'Assert\.That\s*\(', content[pos:])
        if not assert_match:
            result.append(content[pos:])
            break

        # Append content before Assert.That
        result.append(content[pos:pos + assert_match.start()])

        assert_start = pos + assert_match.start()
        paren_start = pos + assert_match.end() - 1  # Position of (

        # Find the end of Assert.That(...)
        paren_end = find_matching_paren(content, paren_start)
        if paren_end < 0:
            # Can't find matching paren, skip this match
            result.append(content[assert_start:assert_start + assert_match.end()])
            pos = assert_start + assert_match.end()
            continue

        # Get the full Assert.That(...) call
        full_call = content[assert_start:paren_end + 1]

        # Look for .Within( inside the call
        within_info = _find_within_call(full_call, 0)
        if within_info:
            within_start, within_end, argument = within_info

            # Check if the argument is a string literal, String.Format, or string expression
            is_string_message = False
            message = None

            # Check for String.Format(...)
            format_match = re.match(r'\s*[Ss]tring\.Format\s*\((.+)\)\s*$', argument, re.DOTALL)
            if format_match:
                format_args_str = format_match.group(1)
                format_args = split_args(format_args_str)
                if format_args:
                    message = convert_format_to_interpolation(format_args[0], format_args[1:])
                    is_string_message = True
            # Check for string literal
            elif is_string_literal(argument):
                message = argument
                is_string_message = True
            # Check for string concatenation expression (e.g., message + " count")
            elif '"' in argument and '+' in argument:
                # Contains a string literal and concatenation - it's a message
                message = argument.strip()
                is_string_message = True
            # Check for expression ending with .ToString() - likely a message
            elif argument.strip().endswith('.ToString()'):
                message = argument.strip()
                is_string_message = True
            # Check for $ interpolated string expression variable
            elif re.match(r'^\$?[a-zA-Z_][a-zA-Z0-9_]*$', argument.strip()):
                # Simple variable name that could be a message string
                # Only treat as message if it doesn't look like a number
                var_name = argument.strip().lower()
                if not any(x in var_name for x in ['num', 'count', 'index', 'size', 'len', 'value', 'tolerance']):
                    message = argument.strip()
                    is_string_message = True

            if is_string_message and message:
                # Remove .Within(...) and add message as third argument
                before_within = full_call[:within_start]
                after_within = full_call[within_end:]

                # The before_within should end with the constraint, after_within should be )
                # We need to insert the message before the final )
                if after_within.strip() == ')':
                    modified = f"{before_within}, {message})"
                    result.append(modified)
                    pos = paren_end + 1
                    continue

        # No modification needed
        result.append(full_call)
        pos = paren_end + 1

    return ''.join(result)


def _contains_format_placeholder(message: str) -> bool:
    """Check if a string contains format placeholders like {0}, {1}, etc."""
    # Look for {0}, {1}, {2}, etc. in the message
    return bool(re.search(r'\{[0-9]+\}', message))


def _parse_assert_that_args(content: str, start_paren: int) -> Optional[Tuple[List[str], int]]:
    """
    Parse Assert.That(...) arguments starting from the opening paren.

    Returns (list of arguments, end position) or None if parsing fails.
    """
    args = []
    current_arg_start = start_paren + 1
    paren_depth = 1
    bracket_depth = 0
    in_string = False
    string_char = None
    i = start_paren + 1

    while i < len(content) and paren_depth > 0:
        char = content[i]

        # Handle string literals
        if in_string:
            if char == '\\' and i + 1 < len(content):
                i += 2  # Skip escaped character
                continue
            if char == string_char:
                in_string = False
        elif char == '"':
            in_string = True
            string_char = '"'
        elif char == '@' and i + 1 < len(content) and content[i + 1] == '"':
            in_string = True
            string_char = '"'
            i += 1  # Skip the @
        elif char == '(':
            paren_depth += 1
        elif char == ')':
            paren_depth -= 1
            if paren_depth == 0:
                # End of Assert.That
                arg = content[current_arg_start:i].strip()
                if arg:
                    args.append(arg)
                return (args, i)
        elif char == '[':
            bracket_depth += 1
        elif char == ']':
            bracket_depth -= 1
        elif char == ',' and paren_depth == 1 and bracket_depth == 0:
            # Top-level argument separator
            arg = content[current_arg_start:i].strip()
            if arg:
                args.append(arg)
            current_arg_start = i + 1

        i += 1

    return None


def fix_assert_that_format_strings(content: str) -> str:
    """
    Fix Assert.That with format string params.

    In NUnit 4, Assert.That no longer accepts params object[] for format args.

    Converts:
        Assert.That(x, Is.EqualTo(y), "msg {0} {1}", arg1, arg2)
    To:
        Assert.That(x, Is.EqualTo(y), $"msg {arg1} {arg2}")

    Only converts when the message string actually contains format placeholders.
    """
    result = []
    pos = 0

    while pos < len(content):
        # Find next Assert.That
        assert_match = re.search(r'Assert\.That\s*\(', content[pos:])
        if not assert_match:
            result.append(content[pos:])
            break

        # Append content before Assert.That
        result.append(content[pos:pos + assert_match.start()])

        assert_start = pos + assert_match.start()
        paren_start = pos + assert_match.end() - 1

        # Parse the arguments
        parsed = _parse_assert_that_args(content, paren_start)
        if parsed is None:
            # Can't parse, skip this
            result.append(content[assert_start:assert_start + assert_match.end()])
            pos = assert_start + assert_match.end()
            continue

        args, end_pos = parsed

        # We need at least 4 args: actual, constraint, message with {0}, format_arg
        if len(args) >= 4:
            # Check if third arg is a message with format placeholders
            message_arg = args[2]
            if is_string_literal(message_arg) and _contains_format_placeholder(message_arg):
                # Check if remaining args are format args (not all string literals)
                format_args = args[3:]
                if format_args and not all(is_string_literal(a.strip()) for a in format_args):
                    # Convert to interpolated string
                    interpolated = convert_format_to_interpolation(message_arg, format_args)
                    new_call = f"Assert.That({args[0]}, {args[1]}, {interpolated})"
                    result.append(new_call)
                    pos = end_pos + 1
                    continue
                elif format_args:
                    # All args are string literals - inline them
                    interpolated = convert_format_to_interpolation(message_arg, format_args)
                    new_call = f"Assert.That({args[0]}, {args[1]}, {interpolated})"
                    result.append(new_call)
                    pos = end_pos + 1
                    continue

        # No modification needed
        full_call = content[assert_start:end_pos + 1]
        result.append(full_call)
        pos = end_pos + 1

    return ''.join(result)


def fix_assert_that_wrong_argument_order(content: str) -> str:
    """
    Fix Assert.That with wrong argument order.

    In NUnit 4, the signature is Assert.That(actual, constraint, message).
    Some legacy code has Assert.That(condition, "message", Is.True/Is.False).

    Converts:
        Assert.That(condition, "message", Is.True)
    To:
        Assert.That(condition, Is.True, "message")

    Also handles string expressions:
        Assert.That(condition, "msg " + var, Is.True)
    To:
        Assert.That(condition, Is.True, "msg " + var)
    """
    result = []
    pos = 0

    while pos < len(content):
        # Find next Assert.That
        assert_match = re.search(r'Assert\.That\s*\(', content[pos:])
        if not assert_match:
            result.append(content[pos:])
            break

        # Append content before Assert.That
        result.append(content[pos:pos + assert_match.start()])

        assert_start = pos + assert_match.start()
        paren_start = pos + assert_match.end() - 1

        # Parse the arguments
        parsed = _parse_assert_that_args(content, paren_start)
        if parsed is None:
            # Can't parse, skip this
            result.append(content[assert_start:assert_start + assert_match.end()])
            pos = assert_start + assert_match.end()
            continue

        args, end_pos = parsed

        # Check for pattern: Assert.That(condition, message_expr, Is.True/Is.False)
        if len(args) == 3:
            constraint = args[2].strip()
            if constraint in ('Is.True', 'Is.False'):
                # Check if second arg looks like a message (string literal or string expression)
                second_arg = args[1].strip()
                if is_string_literal(second_arg) or '"' in second_arg:
                    # Swap args 1 and 2
                    new_call = f"Assert.That({args[0]}, {constraint}, {second_arg})"
                    result.append(new_call)
                    pos = end_pos + 1
                    continue

        # No modification needed
        full_call = content[assert_start:end_pos + 1]
        result.append(full_call)
        pos = end_pos + 1

    return ''.join(result)
