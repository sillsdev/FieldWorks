"""Small Jira command wrapper for the read-only Atlassian skill."""

import argparse
import re
import sys
from typing import List, Optional

from jira_issues import (
    jira_download_attachment,
    jira_download_attachments,
    jira_get_attachments,
    jira_get_comments,
    jira_get_issue,
)
from jira_search import jira_search
from jira_workflow import jira_get_transitions


DEFAULT_ISSUE_FIELDS = 'summary,description,status,issuetype,priority,assignee,reporter,created,updated,labels,components'


def normalize_issue_key(value: str) -> str:
    match = re.search(r'([A-Z][A-Z0-9]+-\d+)', value)
    if not match:
        raise argparse.ArgumentTypeError(f"Could not find an issue key in: {value}")
    return match.group(1)


def build_parser() -> argparse.ArgumentParser:
    parser = argparse.ArgumentParser(
        description="Read Jira issue data using .env or JIRA_* environment variables."
    )
    subparsers = parser.add_subparsers(dest='command', required=True)

    issue = subparsers.add_parser('issue', help='Get one issue')
    issue.add_argument('issue_key', type=normalize_issue_key)
    issue_fields_group = issue.add_mutually_exclusive_group()
    issue_fields_group.add_argument('--fields', help='Comma-separated Jira fields to return')
    issue_fields_group.add_argument('--all-fields', action='store_true', help='Return every Jira field instead of the readable default set')
    issue.add_argument('--expand')

    comments = subparsers.add_parser('comments', help='List issue comments')
    comments.add_argument('issue_key', type=normalize_issue_key)
    comments.add_argument('--limit', type=int, default=50)
    comments.add_argument('--start-at', type=int, default=0)

    attachments = subparsers.add_parser('attachments', help='List issue attachments')
    attachments.add_argument('issue_key', type=normalize_issue_key)

    download_attachment = subparsers.add_parser('download-attachment', help='Download one attachment by ID')
    download_attachment.add_argument('attachment_id')
    download_attachment.add_argument('--out', default='.')
    download_attachment.add_argument('--filename')

    download_attachments = subparsers.add_parser('download-attachments', help='Download all issue attachments')
    download_attachments.add_argument('issue_key', type=normalize_issue_key)
    download_attachments.add_argument('--out', required=True)

    search = subparsers.add_parser('search', help='Run a JQL search')
    search.add_argument('jql')
    search.add_argument('--fields')
    search.add_argument('--limit', type=int, default=10)
    search.add_argument('--start-at', type=int, default=0)

    transitions = subparsers.add_parser('transitions', help='List available issue transitions')
    transitions.add_argument('issue_key', type=normalize_issue_key)

    return parser


def issue_fields(args: argparse.Namespace) -> Optional[str]:
    if args.all_fields:
        return None
    return args.fields or DEFAULT_ISSUE_FIELDS


def main(argv: Optional[List[str]] = None) -> int:
    parser = build_parser()
    args = parser.parse_args(argv)

    if args.command == 'issue':
        result = jira_get_issue(args.issue_key, fields=issue_fields(args), expand=args.expand)
    elif args.command == 'comments':
        result = jira_get_comments(args.issue_key, limit=args.limit, start_at=args.start_at)
    elif args.command == 'attachments':
        result = jira_get_attachments(args.issue_key)
    elif args.command == 'download-attachment':
        result = jira_download_attachment(args.attachment_id, output_dir=args.out, filename=args.filename)
    elif args.command == 'download-attachments':
        result = jira_download_attachments(args.issue_key, output_dir=args.out)
    elif args.command == 'search':
        result = jira_search(args.jql, fields=args.fields, limit=args.limit, start_at=args.start_at)
    elif args.command == 'transitions':
        result = jira_get_transitions(args.issue_key)
    else:
        parser.error(f"Unknown command: {args.command}")

    print(result)
    return 0


if __name__ == '__main__':
    sys.exit(main())
