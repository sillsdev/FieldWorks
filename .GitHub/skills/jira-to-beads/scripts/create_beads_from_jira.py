import json
import subprocess
import sys
from pathlib import Path

JIRA_JSON_PATH = Path(__file__).parent / "jira_assigned.json"
JIRA_BASE_URL = "https://jira.sil.org/browse/"

CHILD_TASKS = [
    ("Triage and reproduce", "Reproduce the issue, capture exact steps, logs, and environment details. Summarize expected vs actual behavior."),
    ("Root cause analysis", "Identify root cause and impacted areas. Note any relevant regressions or dependencies."),
    ("Implement fix", "Apply code changes to address root cause. Keep scope limited to the issue."),
    ("Add/update tests", "Add or adjust tests to cover the fix where feasible."),
    ("Verify fix", "Verify in a local build and document verification steps/results."),
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


def main():
    if not JIRA_JSON_PATH.exists():
        raise SystemExit(f"Missing {JIRA_JSON_PATH}. Run the JIRA export step first.")

    data = json.loads(JIRA_JSON_PATH.read_text(encoding="utf-8"))
    issues = data.get("issues", [])
    if not issues:
        print("No JIRA issues found.")
        return

    external_refs = br_get_external_ref_map()

    summary = []

    for issue in issues:
        key = issue.get("key")
        summary_text = issue.get("summary", "").strip()
        if not key or not summary_text:
            continue

        if key in external_refs:
            summary.append((key, "skipped", external_refs[key]))
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
        for child_title, child_desc in CHILD_TASKS:
            child = br_create_issue(
                title=f"{key}: {child_title}",
                issue_type="task",
                description=child_desc,
                labels=["jira", "subtask"],
                assignee=issue.get("assignee"),
                parent=parent_id,
            )
            child_ids.append(child.get("id"))

        # Wire dependencies: 2->1, 3->2, 4->3, 5->4
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
