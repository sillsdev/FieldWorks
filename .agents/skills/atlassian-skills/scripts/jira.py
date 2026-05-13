"""Small Jira command wrapper for the writable Atlassian skill."""

import argparse
import json
import re
import sys
from pathlib import Path
from typing import List, Optional

from jira_issues import (
    jira_add_attachment,
    jira_add_comment,
    jira_download_attachment,
    jira_download_attachments,
    jira_get_attachments,
    jira_get_comments,
    jira_get_issue,
    jira_update_comment,
    jira_update_issue,
)
from jira_search import jira_search
from jira_workflow import jira_get_transitions, jira_transition_issue


DEFAULT_ISSUE_FIELDS = 'summary,description,status,issuetype,priority,assignee,reporter,created,updated,labels,components'


def normalize_issue_key(value: str) -> str:
    match = re.search(r'([A-Z][A-Z0-9]+-\d+)', value)
    if not match:
        raise argparse.ArgumentTypeError(f"Could not find an issue key in: {value}")
    return match.group(1)


def read_text_argument(body: Optional[str], body_file: Optional[str]) -> str:
    if body_file:
        return Path(body_file).read_text(encoding='utf-8')
    return body or ''


def build_parser() -> argparse.ArgumentParser:
    parser = argparse.ArgumentParser(
        description="Read and update Jira issues using .env or JIRA_* environment variables."
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

    transition = subparsers.add_parser('transition', help='Transition an issue')
    transition.add_argument('issue_key', type=normalize_issue_key)
    transition.add_argument('transition')
    transition.add_argument('--comment')

    update = subparsers.add_parser('update', help='Update simple issue fields')
    update.add_argument('issue_key', type=normalize_issue_key)
    update.add_argument('--summary')
    update.add_argument('--description')
    update.add_argument('--description-file')
    update.add_argument('--priority')
    update.add_argument('--labels', nargs='*')

    add_comment = subparsers.add_parser('add-comment', help='Add a comment')
    add_comment.add_argument('issue_key', type=normalize_issue_key)
    comment_body = add_comment.add_mutually_exclusive_group(required=True)
    comment_body.add_argument('--body')
    comment_body.add_argument('--body-file')

    update_comment = subparsers.add_parser('update-comment', help='Update a comment')
    update_comment.add_argument('issue_key', type=normalize_issue_key)
    update_comment.add_argument('comment_id')
    update_body = update_comment.add_mutually_exclusive_group(required=True)
    update_body.add_argument('--body')
    update_body.add_argument('--body-file')

    attach = subparsers.add_parser('attach', help='Upload one or more file attachments')
    attach.add_argument('issue_key', type=normalize_issue_key)
    attach.add_argument('file_paths', nargs='+')

    return parser


def issue_fields(args: argparse.Namespace) -> Optional[str]:
    if args.all_fields:
        return None
    return args.fields or DEFAULT_ISSUE_FIELDS


def resolve_transition(issue_key: str, transition: str) -> str:
    if transition.isdigit():
        return transition

    transitions = json.loads(jira_get_transitions(issue_key))
    if not transitions.get('success', True):
        return transition

    transition_lower = transition.lower()
    for item in transitions.get('transitions', []):
        names = [item.get('id', ''), item.get('name', ''), item.get('to_status', '')]
        if any(name and name.lower() == transition_lower for name in names):
            return item.get('id') or transition
    return transition


def attach_files(issue_key: str, file_paths: List[str]) -> str:
    results = []
    for file_path in file_paths:
        results.append(json.loads(jira_add_attachment(issue_key, file_path)))
    success = all(item.get('success', True) for item in results)
    return json.dumps({'success': success, 'issue_key': issue_key, 'results': results}, ensure_ascii=False, indent=2)


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
    elif args.command == 'transition':
        result = jira_transition_issue(args.issue_key, resolve_transition(args.issue_key, args.transition), comment=args.comment)
    elif args.command == 'update':
        description = read_text_argument(args.description, args.description_file)
        result = jira_update_issue(
            args.issue_key,
            summary=args.summary,
            description=description or None,
            priority=args.priority,
            labels=args.labels,
        )
    elif args.command == 'add-comment':
        result = jira_add_comment(args.issue_key, read_text_argument(args.body, args.body_file))
    elif args.command == 'update-comment':
        result = jira_update_comment(args.issue_key, args.comment_id, read_text_argument(args.body, args.body_file))
    elif args.command == 'attach':
        result = attach_files(args.issue_key, args.file_paths)
    else:
        parser.error(f"Unknown command: {args.command}")

    print(result)
    return 0


if __name__ == '__main__':
    sys.exit(main())
