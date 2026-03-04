"""CLI wrapper for atlassian-readonly-skills Jira operations."""

import argparse

from jira_issues import jira_get_issue
from jira_search import jira_search
from jira_workflow import jira_get_transitions


def main():
    parser = argparse.ArgumentParser(description="Readonly Jira CLI")
    subparsers = parser.add_subparsers(dest="action", required=True)

    get_issue = subparsers.add_parser("get-issue", help="Get a Jira issue")
    get_issue.add_argument("--issue-key", required=True)
    get_issue.add_argument("--fields", default=None)
    get_issue.add_argument("--expand", default=None)

    search = subparsers.add_parser("search", help="Search Jira issues")
    search.add_argument("--jql", required=True)
    search.add_argument("--fields", default=None)
    search.add_argument("--limit", type=int, default=10)
    search.add_argument("--start-at", type=int, default=0)

    transitions = subparsers.add_parser("get-transitions", help="Get issue transitions")
    transitions.add_argument("--issue-key", required=True)

    args = parser.parse_args()

    if args.action == "get-issue":
        print(jira_get_issue(args.issue_key, fields=args.fields, expand=args.expand))
    elif args.action == "search":
        print(jira_search(args.jql, fields=args.fields, limit=args.limit, start_at=args.start_at))
    elif args.action == "get-transitions":
        print(jira_get_transitions(args.issue_key))


if __name__ == "__main__":
    main()
