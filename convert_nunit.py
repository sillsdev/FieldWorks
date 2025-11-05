#!/usr/bin/env python3
from __future__ import annotations

from pathlib import Path
from typing import Callable, List, Optional


def skip_string(text: str, start: int) -> int:
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
    token = token.lstrip()
    if not token:
        return False
    if token[0] in ('"', "'"):
        return True
    return token.startswith('@"')


def is_constant_expression(token: str) -> bool:
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


def replace_assert_invocations(
    content: str, method: str, converter: Callable[[str, str], Optional[str]]
) -> str:
    result: List[str] = []
    idx = 0
    method_len = len(method)
    while idx < len(content):
        pos = content.find(method, idx)
        if pos == -1:
            result.append(content[idx:])
            break

        result.append(content[idx:pos])
        open_paren = pos + method_len
        if open_paren >= len(content) or content[open_paren] != "(":
            # Not a method invocation; leave as-is
            result.append(method)
            idx = open_paren
            continue

        close_paren = find_matching_paren(content, open_paren)
        if close_paren == -1:
            result.append(content[pos : pos + method_len])
            idx = open_paren
            continue

        original = content[pos : close_paren + 1]
        args_str = content[open_paren + 1 : close_paren]
        replacement = converter(args_str, original)
        result.append(replacement if replacement is not None else original)
        idx = close_paren + 1

    return "".join(result)


def extract_named_argument(arg: str, name: str) -> Optional[str]:
    lowered = arg.strip().lower()
    prefix = f"{name.lower()}:"
    if lowered.startswith(prefix):
        value = arg.split(":", 1)[1].strip()
        return value
    return None


def convert_are_equal(args_str: str, original: str) -> Optional[str]:
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
    message_args: List[str] = []

    if remaining:
        first = remaining[0]
        if not is_string_literal(first) and not first.lower().startswith("message:"):
            constraint += f".Within({first})"
            remaining = remaining[1:]

    if remaining:
        message_args = remaining

    suffix = ""
    if message_args:
        suffix = ", " + ", ".join(message_args)

    return f"Assert.That({actual}, {constraint}{suffix})"


def convert_are_not_equal(args_str: str, original: str) -> Optional[str]:
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
        constraint += f".Within({remaining[0]})"
        remaining = remaining[1:]

    suffix = ""
    if remaining:
        suffix = ", " + ", ".join(remaining)

    return f"Assert.That({actual}, {constraint}{suffix})"


def convert_simple_predicate(
    args_str: str, original: str, predicate: str
) -> Optional[str]:
    args = split_args(args_str)
    if not args:
        return None
    expression = args[0]
    extras = args[1:]
    suffix = ""
    if extras:
        suffix = ", " + ", ".join(extras)
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


def convert_less_or_equal(args_str: str, original: str) -> Optional[str]:
    args = split_args(args_str)
    if len(args) < 2:
        return None

    actual = args[0].strip()
    expected = args[1].strip()
    extras = args[2:]

    suffix = ""
    if extras:
        suffix = ", " + ", ".join(extras)

    return f"Assert.That({actual}, Is.LessThanOrEqualTo({expected}){suffix})"


def convert_greater(args_str: str, original: str) -> Optional[str]:
    args = split_args(args_str)
    if len(args) < 2:
        return None

    actual = args[0].strip()
    expected = args[1].strip()
    extras = args[2:]

    suffix = ""
    if extras:
        suffix = ", " + ", ".join(extras)

    return f"Assert.That({actual}, Is.GreaterThan({expected}){suffix})"


def convert_contains(args_str: str, original: str) -> Optional[str]:
    args = split_args(args_str)
    if len(args) < 2:
        return None

    expected = args[0].strip()
    collection = args[1].strip()
    extras = args[2:]

    suffix = ""
    if extras:
        suffix = ", " + ", ".join(extras)

    return f"Assert.That({collection}, Does.Contain({expected}){suffix})"


def convert_does_not_contain(args_str: str, original: str) -> Optional[str]:
    args = split_args(args_str)
    if len(args) < 2:
        return None

    expected = args[0].strip()
    collection = args[1].strip()
    extras = args[2:]

    suffix = ""
    if extras:
        suffix = ", " + ", ".join(extras)

    return f"Assert.That({collection}, Does.Not.Contain({expected}){suffix})"


def convert_are_same(args_str: str, original: str) -> Optional[str]:
    args = split_args(args_str)
    if len(args) < 2:
        return None

    expected = args[0].strip()
    actual = args[1].strip()
    extras = args[2:]

    suffix = ""
    if extras:
        suffix = ", " + ", ".join(extras)

    return f"Assert.That({actual}, Is.SameAs({expected}){suffix})"


def convert_are_not_same(args_str: str, original: str) -> Optional[str]:
    args = split_args(args_str)
    if len(args) < 2:
        return None

    expected = args[0].strip()
    actual = args[1].strip()
    extras = args[2:]

    suffix = ""
    if extras:
        suffix = ", " + ", ".join(extras)

    return f"Assert.That({actual}, Is.Not.SameAs({expected}){suffix})"


def convert_less(args_str: str, original: str) -> Optional[str]:
    args = split_args(args_str)
    if len(args) < 2:
        return None

    actual = args[0].strip()
    expected = args[1].strip()
    extras = args[2:]

    suffix = ""
    if extras:
        suffix = ", " + ", ".join(extras)

    return f"Assert.That({actual}, Is.LessThan({expected}){suffix})"


def convert_greater_or_equal(args_str: str, original: str) -> Optional[str]:
    args = split_args(args_str)
    if len(args) < 2:
        return None

    actual = args[0].strip()
    expected = args[1].strip()
    extras = args[2:]

    suffix = ""
    if extras:
        suffix = ", " + ", ".join(extras)

    return f"Assert.That({actual}, Is.GreaterThanOrEqualTo({expected}){suffix})"


def convert_is_instance_of(args_str: str, original: str) -> Optional[str]:
    args = split_args(args_str)
    if len(args) < 2:
        return None

    expected_type = args[0].strip()
    actual = args[1].strip()
    extras = args[2:]

    suffix = ""
    if extras:
        suffix = ", " + ", ".join(extras)

    return f"Assert.That({actual}, Is.InstanceOf({expected_type}){suffix})"


# Converters for Assert methods that don't need changes (just keep them as-is)
def no_conversion(args_str: str, original: str) -> Optional[str]:
    # These methods are already compatible with NUnit 4
    return original


# StringAssert converters
def convert_string_assert_contains(args_str: str, original: str) -> Optional[str]:
    args = split_args(args_str)
    if len(args) < 2:
        return None

    expected = args[0].strip()
    actual = args[1].strip()
    extras = args[2:]

    suffix = ""
    if extras:
        suffix = ", " + ", ".join(extras)

    return f"Assert.That({actual}, Does.Contain({expected}){suffix})"


def convert_string_assert_does_not_contain(args_str: str, original: str) -> Optional[str]:
    args = split_args(args_str)
    if len(args) < 2:
        return None

    expected = args[0].strip()
    actual = args[1].strip()
    extras = args[2:]

    suffix = ""
    if extras:
        suffix = ", " + ", ".join(extras)

    return f"Assert.That({actual}, Does.Not.Contain({expected}){suffix})"


def convert_string_assert_starts_with(args_str: str, original: str) -> Optional[str]:
    args = split_args(args_str)
    if len(args) < 2:
        return None

    expected = args[0].strip()
    actual = args[1].strip()
    extras = args[2:]

    suffix = ""
    if extras:
        suffix = ", " + ", ".join(extras)

    return f"Assert.That({actual}, Does.StartWith({expected}){suffix})"


def convert_string_assert_ends_with(args_str: str, original: str) -> Optional[str]:
    args = split_args(args_str)
    if len(args) < 2:
        return None

    expected = args[0].strip()
    actual = args[1].strip()
    extras = args[2:]

    suffix = ""
    if extras:
        suffix = ", " + ", ".join(extras)

    return f"Assert.That({actual}, Does.EndWith({expected}){suffix})"


# CollectionAssert converters
def convert_collection_assert_are_equal(args_str: str, original: str) -> Optional[str]:
    args = split_args(args_str)
    if len(args) < 2:
        return None

    expected = args[0].strip()
    actual = args[1].strip()
    extras = args[2:]

    suffix = ""
    if extras:
        suffix = ", " + ", ".join(extras)

    return f"Assert.That({actual}, Is.EqualTo({expected}){suffix})"


def convert_collection_assert_are_equivalent(args_str: str, original: str) -> Optional[str]:
    args = split_args(args_str)
    if len(args) < 2:
        return None

    expected = args[0].strip()
    actual = args[1].strip()
    extras = args[2:]

    suffix = ""
    if extras:
        suffix = ", " + ", ".join(extras)

    return f"Assert.That({actual}, Is.EquivalentTo({expected}){suffix})"


def convert_collection_assert_contains(args_str: str, original: str) -> Optional[str]:
    args = split_args(args_str)
    if len(args) < 2:
        return None

    expected = args[0].strip()
    actual = args[1].strip()
    extras = args[2:]

    suffix = ""
    if extras:
        suffix = ", " + ", ".join(extras)

    return f"Assert.That({actual}, Does.Contain({expected}){suffix})"


def convert_collection_assert_does_not_contain(args_str: str, original: str) -> Optional[str]:
    args = split_args(args_str)
    if len(args) < 2:
        return None

    expected = args[0].strip()
    actual = args[1].strip()
    extras = args[2:]

    suffix = ""
    if extras:
        suffix = ", " + ", ".join(extras)

    return f"Assert.That({actual}, Does.Not.Contain({expected}){suffix})"


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
    extras = args[2:]

    suffix = ""
    if extras:
        suffix = ", " + ", ".join(extras)

    return f"Assert.That({subset}, Is.SubsetOf({superset}){suffix})"


# FileAssert converters
def convert_file_assert_are_equal(args_str: str, original: str) -> Optional[str]:
    args = split_args(args_str)
    if len(args) < 2:
        return None

    expected = args[0].strip()
    actual = args[1].strip()
    extras = args[2:]

    suffix = ""
    if extras:
        suffix = ", " + ", ".join(extras)

    return f"Assert.That({actual}, Is.EqualTo({expected}){suffix})"


CONVERTERS: List[tuple[str, Callable[[str, str], Optional[str]]]] = [
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
    # Assert.NotNull and Assert.Null are aliases that also need conversion
    ("Assert.NotNull", convert_is_not_null),
    ("Assert.Null", convert_is_null),
    ("Assert.True", convert_true),
    ("Assert.False", convert_false),
    # Assert methods that don't need conversion (already NUnit 4 compatible)
    # We list them to ensure they are not accidentally matched by other patterns
    # but they return the original string unchanged
    ("Assert.Throws", no_conversion),
    ("Assert.Catch", no_conversion),
    ("Assert.DoesNotThrow", no_conversion),
    ("Assert.Fail", no_conversion),
    ("Assert.Ignore", no_conversion),
    ("Assert.Pass", no_conversion),
    ("Assert.Inconclusive", no_conversion),
]


FILES_TO_CONVERT = [
    "Lib/src/ScrChecks/ScrChecksTests/CapitalizationCheckSilUnitTest.cs",
    "Lib/src/ScrChecks/ScrChecksTests/CapitalizationCheckUnitTest.cs",
    "Lib/src/ScrChecks/ScrChecksTests/ChapterVerseTests.cs",
    "Lib/src/ScrChecks/ScrChecksTests/CharactersCheckUnitTest.cs",
    "Lib/src/ScrChecks/ScrChecksTests/MatchedPairsCheckUnitTest.cs",
    "Lib/src/ScrChecks/ScrChecksTests/MixedCapitalizationCheckUnitTest.cs",
    "Lib/src/ScrChecks/ScrChecksTests/PunctuationCheckUnitTest.cs",
    "Lib/src/ScrChecks/ScrChecksTests/QuotationCheckSilUnitTest.cs",
    "Lib/src/ScrChecks/ScrChecksTests/QuotationCheckUnitTest.cs",
    "Lib/src/ScrChecks/ScrChecksTests/RepeatedWordsCheckTests.cs",
    "Lib/src/ScrChecks/ScrChecksTests/RepeatedWordsCheckUnitTest.cs",
    "Lib/src/ScrChecks/ScrChecksTests/ScrChecksTestBase.cs",
]


def get_files_from_folder(folder: str) -> List[str]:
    """Recursively find all .cs files in a folder."""
    folder_path = Path(folder)
    if not folder_path.is_dir():
        print(f"Error: {folder} is not a valid directory")
        return []
    return [str(p) for p in folder_path.rglob("*.cs")]


def convert_content(content: str) -> str:
    updated = content
    for method, converter in CONVERTERS:
        updated = replace_assert_invocations(updated, method, converter)
    return updated


def main() -> None:
    import sys

    if len(sys.argv) > 1 and sys.argv[1] in ("-h", "--help"):
        print("NUnit 3 to NUnit 4 Converter")
        print("=" * 50)
        print()
        print("Usage: python3 convert_nunit.py [folder_path]")
        print()
        print("Description:")
        print("  Converts NUnit 3 style assertions to NUnit 4 constraint model.")
        print("  Recursively processes all .cs files in the specified folder.")
        print()
        print("Arguments:")
        print("  folder_path  Path to folder containing C# test files to convert.")
        print("               If not specified, uses the default file list.")
        print()
        print("Examples:")
        print("  python3 convert_nunit.py Src")
        print("  python3 convert_nunit.py Src/Common/FwUtils/FwUtilsTests")
        print()
        print("Converts:")
        print("  - Assert.AreEqual, IsTrue, IsNull, Contains, Greater, etc.")
        print("  - StringAssert.Contains, StartsWith, EndsWith, etc.")
        print("  - CollectionAssert.IsEmpty, Contains, AreEquivalent, etc.")
        print("  - FileAssert.AreEqual")
        print()
        print("Does NOT convert (already NUnit 4 compatible):")
        print("  - Assert.That(...)")
        print("  - Assert.Throws, DoesNotThrow, Catch")
        print("  - Assert.Fail, Ignore, Pass")
        return

    if len(sys.argv) > 1:
        # Use provided folder argument
        folder = sys.argv[1]
        files_to_process = get_files_from_folder(folder)
        if not files_to_process:
            print("No .cs files found in the specified folder")
            return
    else:
        # Use default file list
        files_to_process = FILES_TO_CONVERT

    for relative_path in files_to_process:
        path = Path(relative_path)
        if not path.exists():
            print(f"File not found: {relative_path}")
            continue

        print(f"Converting {relative_path}...")
        try:
            original = path.read_text(encoding="utf-8")
        except UnicodeDecodeError:
            # Fallback to latin-1 which can handle any byte sequence
            original = path.read_text(encoding="latin-1")

        converted = convert_content(original)
        if converted != original:
            path.write_text(converted, encoding="utf-8")
            print(f"  ✓ Converted {relative_path}")
        else:
            print(f"  · No changes for {relative_path}")


if __name__ == "__main__":
    main()
