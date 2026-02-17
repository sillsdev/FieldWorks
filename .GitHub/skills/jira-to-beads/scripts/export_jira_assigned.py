import argparse
import json
import os
import sys
from pathlib import Path

REPO_ROOT = Path(__file__).resolve().parents[4]
DEFAULT_OUTPUT = REPO_ROOT / ".cache" / "jira_assigned.json"
DEFAULT_SELECTION = REPO_ROOT / ".cache" / "jira_assigned.selection.txt"
DEFAULT_JQL = "assignee = currentUser() AND statusCategory != Done ORDER BY updated DESC"

ATLASSIAN_READONLY_DIR = REPO_ROOT / ".github" / "skills" / "atlassian-readonly-skills" / "scripts"
sys.path.insert(0, str(ATLASSIAN_READONLY_DIR))

from jira_search import jira_search


def resolve_output_path(cli_path=None):
    if cli_path:
        candidate = Path(cli_path)
        if not candidate.is_absolute():
            candidate = REPO_ROOT / candidate
        return candidate

    env_path = os.getenv("JIRA_ASSIGNED_PATH")
    if env_path:
        candidate = Path(env_path)
        if not candidate.is_absolute():
            candidate = REPO_ROOT / candidate
        return candidate

    return DEFAULT_OUTPUT


def resolve_selection_path(cli_path=None):
    if cli_path:
        candidate = Path(cli_path)
        if not candidate.is_absolute():
            candidate = REPO_ROOT / candidate
        return candidate

    return DEFAULT_SELECTION


def fetch_issues(jql, limit):
    issues = []
    start_at = 0

    while True:
        raw = jira_search(jql=jql, limit=limit, start_at=start_at)
        data = json.loads(raw)

        if data.get("success") is False:
            message = data.get("error", "Unknown error")
            raise SystemExit(f"Jira search failed: {message}")

        page_issues = data.get("issues", [])
        issues.extend(page_issues)

        if data.get("is_last") or not page_issues:
            break

        start_at = data.get("start_at", start_at) + len(page_issues)

    return issues


def main():
    parser = argparse.ArgumentParser(description="Export assigned JIRA issues to jira_assigned.json.")
    parser.add_argument(
        "--jql",
        default=DEFAULT_JQL,
        help="JQL query to export (default: assigned open issues)",
    )
    parser.add_argument(
        "--output",
        "-o",
        help="Output JSON path (default: .cache/jira_assigned.json)",
    )
    parser.add_argument(
        "--selection-file",
        help="Write selection file for create_beads_from_jira.py (default: .cache/jira_assigned.selection.txt)",
    )
    parser.add_argument(
        "--limit",
        type=int,
        default=100,
        help="Page size for Jira search results (default: 100)",
    )
    args = parser.parse_args()

    output_path = resolve_output_path(args.output)
    output_path.parent.mkdir(parents=True, exist_ok=True)

    issues = fetch_issues(args.jql, args.limit)

    payload = {
        "jql": args.jql,
        "total": len(issues),
        "issues": issues,
    }

    output_path.write_text(json.dumps(payload, ensure_ascii=False, indent=2), encoding="utf-8")

    selection_path = resolve_selection_path(args.selection_file)
    selection_path.parent.mkdir(parents=True, exist_ok=True)
    selection_lines = [
        "# Uncomment issues you want to create as beads by deleting the leading '#'.",
        "# Format: KEY | Summary",
        "# Example: FLEx-123 | Fix crash on startup",
        "",
    ]
    for issue in issues:
        key = issue.get("key", "").strip()
        summary = (issue.get("summary") or "").strip()
        if not key:
            continue
        selection_lines.append(f"# {key} | {summary}")

    selection_path.write_text("\n".join(selection_lines) + "\n", encoding="utf-8")

    print(f"Wrote {len(issues)} issue(s) to {output_path}")
    print(f"Wrote selection file to {selection_path}")


if __name__ == "__main__":
    main()
