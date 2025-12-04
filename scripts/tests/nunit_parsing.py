#!/usr/bin/env python3
"""
Parsing utilities for C# code analysis.

Provides functions for parsing C# strings, arguments, and method invocations.
"""
from __future__ import annotations

from typing import Callable, List, Optional


def skip_string(text: str, start: int) -> int:
    """Skip over a quoted string, handling escape sequences."""
    quote = text[start]
    i = start + 1
    while i < len(text):
        if text[i] == "\\":
            i += 2
            continue
        if text[i] == quote:
            return i
        i += 1
    return len(text) - 1


def skip_verbatim_string(text: str, start: int) -> int:
    """Skip over a verbatim string (@"..."), handling doubled quotes."""
    # start points to '@'
    i = start + 2  # skip @"
    while i < len(text):
        if text[i] == '"':
            if i + 1 < len(text) and text[i + 1] == '"':
                i += 2
                continue
            return i
        i += 1
    return len(text) - 1


def find_matching_paren(text: str, open_index: int) -> int:
    """Find the closing parenthesis matching the one at open_index."""
    depth = 0
    i = open_index
    while i < len(text):
        c = text[i]
        if c == "@" and i + 1 < len(text) and text[i + 1] == '"':
            i = skip_verbatim_string(text, i)
        elif c in ('"', "'"):
            i = skip_string(text, i)
        elif c == "(":
            depth += 1
        elif c == ")":
            depth -= 1
            if depth == 0:
                return i
        i += 1
    return -1


def split_args(arg_string: str) -> List[str]:
    """Split a comma-separated argument list, respecting nesting and strings."""
    parts: List[str] = []
    depth = 0
    start = 0
    i = 0
    length = len(arg_string)
    while i < length:
        c = arg_string[i]
        if c == "@" and i + 1 < length and arg_string[i + 1] == '"':
            i = skip_verbatim_string(arg_string, i)
        elif c in ('"', "'"):
            i = skip_string(arg_string, i)
        elif c in "([{":
            depth += 1
        elif c in ")]}":
            depth -= 1
        elif c == "," and depth == 0:
            parts.append(arg_string[start:i].strip())
            start = i + 1
        i += 1

    tail = arg_string[start:].strip()
    if tail:
        parts.append(tail)
    return parts


def is_string_literal(token: str) -> bool:
    """Check if token starts with a string literal."""
    token = token.lstrip()
    if not token:
        return False
    first = token[0]
    if first in ('"', "'"):
        return True
    if first == "$":
        if len(token) > 1 and token[1] in ('"', "@"):
            return True
    if first == "@" and len(token) > 1 and token[1] == '"':
        return True
    if token.startswith('@$"') or token.startswith('$@"'):
        return True
    return False


def is_string_format_call(token: str) -> bool:
    """Check if token is a String.Format(...) call."""
    token = token.strip()
    return token.startswith("String.Format(") or token.startswith("string.Format(")


def looks_like_message(token: str) -> bool:
    """Check if token looks like a message string rather than a numeric tolerance."""
    token = token.strip()
    if is_string_literal(token):
        return True
    if is_string_format_call(token):
        return True
    if token.lower().startswith("message:"):
        return True
    return False


def looks_like_tolerance(token: str) -> bool:
    """Check if token looks like a numeric tolerance value."""
    token = token.strip()
    if not token:
        return False

    lowered = token.lower()
    if lowered.startswith("message:"):
        return False

    # String literals, interpolated strings, and concatenations contain quotes/dollar signs.
    if (
        '"' in token
        or "'" in token
        or token.startswith("$")
        or token.startswith("@$")
        or token.startswith("$@")
    ):
        return False

    # String.Format is a message, not a tolerance
    if is_string_format_call(token):
        return False

    keywords = (
        "timespan",
        "tolerance",
        "milliseconds",
        "seconds",
        "minutes",
        "days",
        "ticks",
    )
    if any(keyword in lowered for keyword in keywords):
        return True

    # Numeric literals or numeric expressions
    numeric_chars = set("0123456789")
    if any(ch in numeric_chars for ch in token):
        return True

    # Expressions like SomeValue or Constants typically used for tolerance
    if token.endswith("Tolerance"):
        return True

    return False


def is_constant_expression(token: str) -> bool:
    """Check if token is a constant expression (literal value)."""
    stripped = token.strip()
    if not stripped:
        return False
    lowered = stripped.lower()
    if lowered in {"true", "false", "null"}:
        return True
    if stripped[0] in ('"', "'", "@"):
        return True
    if stripped[0] in "+-" and len(stripped) > 1 and stripped[1].isdigit():
        return True
    if stripped[0].isdigit():
        return True
    if lowered.startswith("0x"):
        return True
    return False


def extract_named_argument(arg: str, name: str) -> Optional[str]:
    """Extract value from a named argument like 'expected: value'."""
    lowered = arg.strip().lower()
    prefix = f"{name.lower()}:"
    if lowered.startswith(prefix):
        value = arg.split(":", 1)[1].strip()
        return value
    return None


def replace_assert_invocations(
    content: str, method: str, converter: Callable[[str, str], Optional[str]]
) -> str:
    """Replace all invocations of a method using a converter function.

    Handles both regular and generic method calls:
    - Assert.IsNull(x)
    - Assert.IsInstanceOf<Type>(x)
    """
    result: List[str] = []
    idx = 0
    method_len = len(method)
    while idx < len(content):
        pos = content.find(method, idx)
        if pos == -1:
            result.append(content[idx:])
            break

        result.append(content[idx:pos])

        # Check for generic type parameter <T>
        generic_param = ""
        open_paren = pos + method_len
        if open_paren < len(content) and content[open_paren] == '<':
            # Find the closing >
            angle_depth = 1
            i = open_paren + 1
            while i < len(content) and angle_depth > 0:
                if content[i] == '<':
                    angle_depth += 1
                elif content[i] == '>':
                    angle_depth -= 1
                i += 1
            if angle_depth == 0:
                generic_param = content[open_paren:i]
                open_paren = i

        if open_paren >= len(content) or content[open_paren] != "(":
            # Not a method invocation; leave as-is
            result.append(method + generic_param)
            idx = open_paren
            continue

        close_paren = find_matching_paren(content, open_paren)
        if close_paren == -1:
            result.append(content[pos : pos + method_len] + generic_param)
            idx = open_paren
            continue

        original = content[pos : close_paren + 1]
        args_str = content[open_paren + 1 : close_paren]

        # For generic methods, prepend the type parameter to the args
        if generic_param:
            args_str = generic_param + ", " + args_str if args_str.strip() else generic_param

        replacement = converter(args_str, original)
        result.append(replacement if replacement is not None else original)
        idx = close_paren + 1

    return "".join(result)
