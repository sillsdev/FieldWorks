"""CLI wrapper for atlassian-skills Jira operations."""

import argparse

from jira_issues import jira_add_comment, jira_create_issue, jira_update_issue
from jira_workflow import jira_get_transitions, jira_transition_issue


def _parse_labels(labels_value):
    if labels_value is None or labels_value == "":
        return None
    return [label.strip() for label in labels_value.split(",") if label.strip()]


def main():
    parser = argparse.ArgumentParser(description="Jira CLI")
    subparsers = parser.add_subparsers(dest="action", required=True)

    create_issue = subparsers.add_parser("create-issue", help="Create a Jira issue")
    create_issue.add_argument("--project-key", required=True)
    create_issue.add_argument("--summary", required=True)
    create_issue.add_argument("--issue-type", required=True)
    create_issue.add_argument("--description", default=None)
    create_issue.add_argument("--assignee", default=None)
    create_issue.add_argument("--priority", default=None)
    create_issue.add_argument("--labels", default=None)

    update_issue = subparsers.add_parser("update-issue", help="Update a Jira issue")
    update_issue.add_argument("--issue-key", required=True)
    update_issue.add_argument("--summary", default=None)
    update_issue.add_argument("--description", default=None)
    update_issue.add_argument("--assignee", default=None)
    update_issue.add_argument("--priority", default=None)
    update_issue.add_argument("--labels", default=None)

    add_comment = subparsers.add_parser("add-comment", help="Add issue comment")
    add_comment.add_argument("--issue-key", required=True)
    add_comment.add_argument("--comment", required=True)

    transitions = subparsers.add_parser("get-transitions", help="Get issue transitions")
    transitions.add_argument("--issue-key", required=True)

    transition_issue = subparsers.add_parser("transition", help="Transition issue status")
    transition_issue.add_argument("--issue-key", required=True)
    transition_issue.add_argument("--transition-id", required=True)
    transition_issue.add_argument("--comment", default=None)

    args = parser.parse_args()

    if args.action == "create-issue":
        print(
            jira_create_issue(
                project_key=args.project_key,
                summary=args.summary,
                issue_type=args.issue_type,
                description=args.description,
                assignee=args.assignee,
                priority=args.priority,
                labels=_parse_labels(args.labels),
            )
        )
    elif args.action == "update-issue":
        print(
            jira_update_issue(
                issue_key=args.issue_key,
                summary=args.summary,
                description=args.description,
                assignee=args.assignee,
                priority=args.priority,
                labels=_parse_labels(args.labels),
            )
        )
    elif args.action == "add-comment":
        print(jira_add_comment(args.issue_key, args.comment))
    elif args.action == "get-transitions":
        print(jira_get_transitions(args.issue_key))
    elif args.action == "transition":
        print(
            jira_transition_issue(
                issue_key=args.issue_key,
                transition_id=args.transition_id,
                comment=args.comment,
            )
        )


if __name__ == "__main__":
    main()
