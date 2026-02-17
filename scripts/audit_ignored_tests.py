from __future__ import annotations

import argparse
import re
import subprocess
import sys
from dataclasses import dataclass
from pathlib import Path


@dataclass(frozen=True)
class FileCounts:
	if_disabled: int
	legacy_guards: int
	ignore_markers: int


_IGNORE_ATTR_REGEX = re.compile(
	r"^(?!\s*//)\s*\[[^\]]*\b(?:Ignore|Explicit)(?:Attribute)?\b",
	re.IGNORECASE | re.MULTILINE,
)
_ASSERT_IGNORE_REGEX = re.compile(
	r"^(?!\s*//).*?\b(?:NUnit\s*\.\s*Framework\s*\.\s*)?Assert\s*\.\s*Ignore\s*\(",
	re.IGNORECASE | re.MULTILINE,
)

# Match common compile-disabled patterns:
# - #if false
# - #if (false)
# - #if 0
# - #if (0)
_IF_DISABLED_REGEX = re.compile(
	r"^\s*#(?:if|elif)\s*(?:\(\s*)?(?:false|0)\s*(?:\))?\b",
	re.IGNORECASE | re.MULTILINE,
)


def parse_args(argv: list[str] | None = None) -> argparse.Namespace:
	parser = argparse.ArgumentParser(
		description=(
			"Audit ignored/skipped tests by scanning source for NUnit ignore markers "
			"and compile-disabled (#if false) blocks, comparing current working tree "
			"to a baseline git ref (default: release/9.3)."
		),
	)
	parser.add_argument(
		"--repo-root",
		type=Path,
		default=Path(__file__).resolve().parents[1],
		help=argparse.SUPPRESS,
	)
	parser.add_argument(
		"--baseline-ref",
		type=str,
		default="release/9.3",
		help="Git ref to compare against (default: release/9.3)",
	)
	parser.add_argument(
		"--out-dir",
		type=Path,
		default=Path(".cache/test-audit"),
		help="Output directory for reports (default: .cache/test-audit)",
	)
	parser.add_argument(
		"--paths",
		type=Path,
		nargs="*",
		default=[Path("Src"), Path("Lib/src"), Path("Build/Src")],
		help="Paths (relative to repo root) to scan (default: Src Lib/src Build/Src)",
	)
	parser.add_argument(
		"--extensions",
		type=str,
		nargs="*",
		default=[".cs"],
		help="File extensions to scan (default: .cs)",
	)
	parser.add_argument(
		"--legacy-guard-symbols",
		type=str,
		nargs="*",
		default=["RUN_LW_LEGACY_TESTS", "FW_LEGACY_TESTS_DISABLED"],
		help=(
			"Preprocessor symbols that guard archived/legacy tests. Lines like '#if <symbol>' "
			"(and '#if !<symbol>') are counted separately from '#if false/0'."
		),
	)
	parser.add_argument(
		"--include-non-test-files",
		action="store_true",
		help="Include files not under a *Tests* folder (default: false)",
	)
	return parser.parse_args(argv)


def main(argv: list[str] | None = None) -> int:
	args = parse_args(argv)
	repo_root: Path = args.repo_root.resolve()
	out_dir: Path = (repo_root / args.out_dir).resolve() if not args.out_dir.is_absolute() else args.out_dir.resolve()
	baseline_ref: str = args.baseline_ref
	legacy_guard_symbols: list[str] = list(args.legacy_guard_symbols or [])

	baseline_sha = _git(repo_root, ["rev-parse", "--short=8", baseline_ref]).strip()

	scan_roots = [(_resolve(repo_root, p)).resolve() for p in args.paths]
	extensions = set(args.extensions)

	files = _discover_files(repo_root, scan_roots, extensions, include_non_test=args.include_non_test_files)
	rows: list[tuple[Path, FileCounts, FileCounts]] = []

	for rel_path in files:
		current_text = (repo_root / rel_path).read_text(encoding="utf-8", errors="replace")
		current_counts = _count_markers(current_text, legacy_guard_symbols)

		baseline_text = _git_show_text(repo_root, baseline_ref, rel_path)
		baseline_counts = (
			_count_markers(baseline_text, legacy_guard_symbols)
			if baseline_text is not None
			else FileCounts(0, 0, 0)
		)

		if current_counts == FileCounts(0, 0, 0) and baseline_counts == FileCounts(0, 0, 0):
			continue

		rows.append((rel_path, current_counts, baseline_counts))

	rows.sort(
		key=lambda r: (
			-(r[1].if_disabled + r[1].legacy_guards + r[1].ignore_markers),
			str(r[0]).lower(),
		)
	)

	out_dir.mkdir(parents=True, exist_ok=True)

	# Preserve the historical/default filename for the tests-only scan.
	# When including non-test files, write a distinct report so we don't clobber the tests-only artifact.
	report_stem = f"ignored-history-{_sanitize_ref(baseline_ref)}-{baseline_sha}"
	if args.include_non_test_files:
		report_stem += "-all"
	report_path = out_dir / f"{report_stem}.md"
	_write_report(report_path, baseline_ref, baseline_sha, rows)

	print(f"Wrote {report_path}")
	print(f"Files with markers: {len(rows)}")
	return 0


def _discover_files(
	repo_root: Path,
	scan_roots: list[Path],
	extensions: set[str],
	*,
	include_non_test: bool,
) -> list[Path]:
	results: list[Path] = []
	for root in scan_roots:
		if not root.exists():
			continue
		for path in root.rglob("*"):
			if not path.is_file():
				continue
			if path.suffix.lower() not in extensions:
				continue
			rel_path = path.relative_to(repo_root)
			if not include_non_test and not _is_test_path(rel_path):
				continue
			results.append(rel_path)
	return results


def _is_test_path(rel_path: Path) -> bool:
	# Heuristic: most FieldWorks test sources live under folders ending with *Tests.
	for part in rel_path.parts:
		if part.endswith("Tests"):
			return True
	# Some older projects use *Test as a folder name, but keep it conservative.
	if rel_path.name.endswith("Tests.cs") or rel_path.name.endswith("Test.cs"):
		return True
	return False


def _count_markers(text: str, legacy_guard_symbols: list[str]) -> FileCounts:
	legacy_regex = _build_legacy_guard_regex(legacy_guard_symbols)
	return FileCounts(
		if_disabled=len(_IF_DISABLED_REGEX.findall(text)),
		legacy_guards=len(legacy_regex.findall(text)) if legacy_regex is not None else 0,
		ignore_markers=(
			len(_IGNORE_ATTR_REGEX.findall(text)) + len(_ASSERT_IGNORE_REGEX.findall(text))
		),
	)


def _build_legacy_guard_regex(legacy_guard_symbols: list[str]) -> re.Pattern[str] | None:
	# Match lines like:
	#   #if RUN_LW_LEGACY_TESTS
	#   #if !RUN_LW_LEGACY_TESTS
	# Keep it strict to avoid counting arbitrary #if expressions.
	if not legacy_guard_symbols:
		return None
	safe_symbols = [re.escape(s) for s in legacy_guard_symbols if s and s.strip()]
	if not safe_symbols:
		return None
	symbol_alt = "|".join(safe_symbols)
	return re.compile(
		rf"^\s*#if\s+!?\s*(?:{symbol_alt})\b",
		re.IGNORECASE | re.MULTILINE,
	)


def _git_show_text(repo_root: Path, ref: str, rel_path: Path) -> str | None:
	try:
		return _git(repo_root, ["show", f"{ref}:{rel_path.as_posix()}"])
	except subprocess.CalledProcessError:
		return None


def _write_report(
	report_path: Path,
	baseline_ref: str,
	baseline_sha: str,
	rows: list[tuple[Path, FileCounts, FileCounts]],
) -> None:
	lines: list[str] = []
	lines.append("# Ignored / Skipped Test Audit")
	lines.append("")
	lines.append(f"Baseline ref: `{baseline_ref}` (`{baseline_sha}`)")
	lines.append("")
	lines.append("Markers scanned:")
	lines.append("- Compile-disabled blocks: `#if false`, `#if 0` (with optional parentheses)")
	lines.append("- Legacy/archived guards: `#if <symbol>` for configured legacy symbols")
	lines.append("- NUnit/runtime skip markers: `[Ignore]`, `[Explicit]`, `Assert.Ignore(...)`")
	lines.append("")
	lines.append("## Results")
	lines.append("")
	lines.append(
		"| File | Current `#if false/0` | Baseline `#if false/0` | Current legacy guards | Baseline legacy guards | Current ignore markers | Baseline ignore markers |"
	)
	lines.append("| --- | ---: | ---: | ---: | ---: | ---: | ---: |")

	for rel_path, cur, base in rows:
		lines.append(
			f"| {rel_path.as_posix()} | {cur.if_disabled} | {base.if_disabled} | {cur.legacy_guards} | {base.legacy_guards} | {cur.ignore_markers} | {base.ignore_markers} |"
		)

	lines.append("")
	lines.append("## Notes")
	lines.append("")
	lines.append(
		"- This is a text scan, not a semantic parse; counts are intended as a quick signal. "
		"Use `scripts/Agent/Git-Search.ps1` or `git show` for authoritative context."
	)
	lines.append("")
	report_path.write_text("\n".join(lines) + "\n", encoding="utf-8")


def _resolve(repo_root: Path, path: Path) -> Path:
	return path if path.is_absolute() else repo_root / path


def _git(repo_root: Path, args: list[str]) -> str:
	result = subprocess.run(
		["git", "-C", str(repo_root), *args],
		check=True,
		stdout=subprocess.PIPE,
		stderr=subprocess.PIPE,
		text=True,
		encoding="utf-8",
		errors="replace",
	)
	return result.stdout


def _sanitize_ref(ref: str) -> str:
	# Make a filename-safe token.
	return re.sub(r"[^A-Za-z0-9._-]+", "-", ref).strip("-")


if __name__ == "__main__":
	raise SystemExit(main(sys.argv[1:]))
