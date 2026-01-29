import argparse
import json
import os
import subprocess
import sys
from pathlib import Path

REPO_ROOT = Path(__file__).resolve().parents[4]
DEFAULT_JIRA_JSON = REPO_ROOT / ".cache" / "jira_assigned.json"
DEFAULT_SELECTION_FILE = REPO_ROOT / ".cache" / "jira_assigned.selection.txt"
JIRA_BASE_URL = "https://jira.sil.org/browse/"

CHILD_TASKS = [
    (
        "Plan / design",
        "Confirm scope, constraints, and design approach. Capture risks, dependencies, and acceptance notes.",
        "skill-plan-design",
    ),
    (
        "Execute / implement",
        "Implement the fix or change set. Keep scope limited to the issue and follow repo conventions.",
        "skill-execute-implement",
    ),
    (
        "Review",
        "Review the change set for correctness, style, and regressions. Note any follow-up work.",
        "skill-review",
    ),
    (
        "Verify / test",
        "Verify in a local build and capture test results or validation notes.",
        "skill-verify-test",
    ),
]


def run(cmd):
    result = subprocess.run(cmd, capture_output=True, text=True, encoding="utf-8", errors="replace")
    if result.returncode != 0:
        print(result.stdout)
        print(result.stderr, file=sys.stderr)
        raise SystemExit(result.returncode)
    return (result.stdout or "").strip()


def br_get_external_ref_map():
    raw = run(["br", "list", "--format", "csv", "--fields", "id,external_ref", "--all", "--limit", "0"])
    ext_map = {}
    if not raw:
        return ext_map
    lines = raw.splitlines()
    if not lines:
        return ext_map
    # Skip header row
    for line in lines[1:]:
        parts = [p.strip() for p in line.split(",", 1)]
        if len(parts) != 2:
            continue
        issue_id, ext = parts
        if ext:
            ext_map[ext] = issue_id
    return ext_map


def br_create_issue(title, issue_type, description, external_ref=None, labels=None, assignee=None, parent=None):
    cmd = ["br", "create", "--json", "--title", title, "--type", issue_type, "--description", description]
    if external_ref:
        cmd += ["--external-ref", external_ref]
    if labels:
        cmd += ["--labels", ",".join(labels)]
    if assignee:
        cmd += ["--assignee", assignee]
    if parent:
        cmd += ["--parent", str(parent)]
    raw = run(cmd)
    return json.loads(raw)


def br_dep_add(issue_id, depends_on_id):
    run(["br", "dep", "add", str(issue_id), str(depends_on_id)])


def is_open_issue(issue):
    status = (issue.get("status") or issue.get("status_name") or "").strip().lower()
    if not status:
        return True
    closed_statuses = {"done", "closed", "resolved", "complete", "completed"}
    return status not in closed_statuses


def select_issues(issues, selection):
    if not issues:
        return []

    if not selection:
        return []

    raw = selection.strip()
    if not raw:
        return []

    if raw.lower() == "all":
        return issues

    tokens = [t for t in raw.replace(",", " ").split() if t]
    selected = []
    by_key = {issue.get("key"): issue for issue in issues if issue.get("key")}
    for token in tokens:
        if token.isdigit():
            idx = int(token)
            if 1 <= idx <= len(issues):
                selected.append(issues[idx - 1])
            continue
        issue = by_key.get(token)
        if issue:
            selected.append(issue)

    # Preserve input order, de-dup by key
    seen = set()
    deduped = []
    for issue in selected:
        key = issue.get("key")
        if not key or key in seen:
            continue
        seen.add(key)
        deduped.append(issue)

    return deduped


def select_issues_from_keys(issues, keys):
    if not keys:
        return []
    return select_issues(issues, " ".join(keys))


def read_selection_file(path):
    if not path.exists():
        return []

    keys = []
    for raw_line in path.read_text(encoding="utf-8").splitlines():
        line = raw_line.strip()
        if not line or line.startswith("#"):
            continue
        if "|" in line:
            line = line.split("|", 1)[0].strip()
        key = line.split()[0] if line else ""
        if key:
            keys.append(key)
    return keys


def prompt_issue_selection(issues):
    if not issues:
        return []

    print("Select JIRA issues to create as beads (multi-select, empty = none):")
    for idx, issue in enumerate(issues, start=1):
        key = issue.get("key", "(no-key)")
        summary_text = issue.get("summary", "").strip()
        status = issue.get("status") or issue.get("status_name") or "unknown"
        print(f"  {idx}. {key} [{status}] - {summary_text}")

    raw = input("Enter numbers (comma/space-separated), keys, or 'all': ").strip()
    return select_issues(issues, raw)


def resolve_input_path(cli_path=None):
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

    return DEFAULT_JIRA_JSON


def resolve_selection_path(cli_path=None):
    if cli_path:
        candidate = Path(cli_path)
        if not candidate.is_absolute():
            candidate = REPO_ROOT / candidate
        return candidate

    return DEFAULT_SELECTION_FILE


def main():
    parser = argparse.ArgumentParser(description="Create Beads issues from a JIRA JSON export.")
    parser.add_argument(
        "-i",
        "--input",
        help="Path to JIRA JSON export (default: .cache/jira_assigned.json)",
    )
    parser.add_argument(
        "--select",
        help="Comma/space-separated issue numbers or keys (or 'all')",
    )
    parser.add_argument(
        "--select-file",
        help="Path to selection file (default: .cache/jira_assigned.selection.txt)",
    )
    args = parser.parse_args()

    jira_json_path = resolve_input_path(args.input)
    if not jira_json_path.exists():
        legacy_path = Path(__file__).parent / "jira_assigned.json"
        if legacy_path.exists():
            jira_json_path = legacy_path
        else:
            raise SystemExit(
                f"Missing {jira_json_path}. Run export_jira_assigned.py or pass --input to specify the file."
            )

    data = json.loads(jira_json_path.read_text(encoding="utf-8"))
    issues = data.get("issues", [])
    if not issues:
        print("No JIRA issues found.")
        return

    external_refs = br_get_external_ref_map()

    summary = []

    by_key_all = {}
    closed_keys = set()
    existing_keys = set()
    candidates = []
    for issue in issues:
        key = issue.get("key")
        summary_text = issue.get("summary", "").strip()
        if not key or not summary_text:
            continue

        by_key_all[key] = issue

        if not is_open_issue(issue):
            closed_keys.add(key)
            continue

        if key in external_refs:
            existing_keys.add(key)
            summary.append((key, "skipped", external_refs[key]))
            continue

        candidates.append(issue)

    if args.select:
        selected_keys = [k for k in args.select.replace(",", " ").split() if k]
        selected_issues = select_issues(candidates, args.select)
    else:
        selection_path = resolve_selection_path(args.select_file)
        selected_keys = read_selection_file(selection_path)
        if selected_keys:
            selected_issues = select_issues_from_keys(candidates, selected_keys)
        else:
            selected_issues = prompt_issue_selection(candidates)

    if not selected_issues:
        if selected_keys:
            missing_keys = [k for k in selected_keys if k not in by_key_all]
            closed_selected = [k for k in selected_keys if k in closed_keys]
            existing_selected = [k for k in selected_keys if k in existing_keys]

            print("No issues selected after filtering.")
            if existing_selected:
                print("Already exists in beads:")
                for key in existing_selected:
                    print(f"  {key} (existing {external_refs.get(key)})")
            if closed_selected:
                print("Not open in JIRA export:")
                for key in closed_selected:
                    print(f"  {key}")
            if missing_keys:
                print("Not found in JIRA export:")
                for key in missing_keys:
                    print(f"  {key}")
        else:
            print("No issues selected.")
        return

    for issue in selected_issues:
        key = issue.get("key")
        summary_text = issue.get("summary", "").strip()
        if not key or not summary_text:
            continue

        title = f"{key}: {summary_text}"
        jira_url = f"{JIRA_BASE_URL}{key}"
        parent_desc = f"JIRA: {jira_url}\n\nSummary: {summary_text}\n\nSee JIRA for full details and attachments."

        parent = br_create_issue(
            title=title,
            issue_type="bug",
            description=parent_desc,
            external_ref=key,
            labels=["jira"],
            assignee=issue.get("assignee"),
        )
        parent_id = parent.get("id")

        child_ids = []
        for child_title, child_desc, child_skill in CHILD_TASKS:
            child = br_create_issue(
                title=f"{key}: {child_title}",
                issue_type="task",
                description=child_desc,
                labels=["jira", "subtask", child_skill],
                assignee=issue.get("assignee"),
                parent=parent_id,
            )
            child_ids.append(child.get("id"))

        # Wire dependencies: 2->1, 3->2, 4->3
        for idx in range(1, len(child_ids)):
            br_dep_add(child_ids[idx], child_ids[idx - 1])

        summary.append((key, "created", parent_id, child_ids))

    for item in summary:
        if item[1] == "skipped":
            print(f"{item[0]}: skipped (existing {item[2]})")
        else:
            print(f"{item[0]}: created parent {item[2]} children {item[3]}")


if __name__ == "__main__":
    main()
