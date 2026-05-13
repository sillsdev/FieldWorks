"""Jira issue management tools.

Tools:
    - jira_get_issue: Retrieve an issue by key
    - jira_get_comments: Retrieve comments for an issue
    - jira_get_attachments: Retrieve attachment metadata
    - jira_download_attachment: Download one attachment by ID
    - jira_download_attachments: Download all issue attachments
"""

import sys
import re
from pathlib import Path
sys.path.insert(0, str(Path(__file__).parent))

from typing import Any, Dict, List, Optional

from _common import (
    AtlassianCredentials,
    get_jira_client,
    simplify_issue,
    format_json_response,
    format_error_response,
    ConfigurationError,
    AuthenticationError,
    ValidationError,
    NotFoundError,
    APIError,
    NetworkError,
)


def _body_to_text(body: Any) -> str:
    """Extract readable text from Jira Server strings or Jira Cloud ADF bodies."""
    if body is None:
        return ""
    if isinstance(body, str):
        return body
    if isinstance(body, list):
        return "\n".join(part for part in (_body_to_text(item) for item in body) if part)
    if isinstance(body, dict):
        text = body.get('text')
        if isinstance(text, str):
            return text
        return _body_to_text(body.get('content', []))
    return str(body)


def _user_label(user_data: Optional[Dict[str, Any]]) -> Optional[str]:
    """Return a stable, human-readable Jira user label."""
    if not user_data:
        return None
    return (
        user_data.get('displayName')
        or user_data.get('emailAddress')
        or user_data.get('name')
        or user_data.get('accountId')
    )


def _simplify_comment(comment_data: Dict[str, Any]) -> Dict[str, Any]:
    """Simplify Jira comment data to the fields agents usually need."""
    return {
        'id': comment_data.get('id', ''),
        'author': _user_label(comment_data.get('author')),
        'update_author': _user_label(comment_data.get('updateAuthor')),
        'body': _body_to_text(comment_data.get('body')),
        'created': comment_data.get('created', ''),
        'updated': comment_data.get('updated', '')
    }


def _simplify_attachment(attachment_data: Dict[str, Any]) -> Dict[str, Any]:
    """Simplify Jira attachment metadata."""
    return {
        'id': attachment_data.get('id', ''),
        'filename': attachment_data.get('filename', ''),
        'size': attachment_data.get('size', 0),
        'mime_type': attachment_data.get('mimeType', ''),
        'author': _user_label(attachment_data.get('author')),
        'created': attachment_data.get('created', ''),
        'content': attachment_data.get('content', ''),
        'thumbnail': attachment_data.get('thumbnail', '')
    }


def _safe_filename(filename: str) -> str:
    """Keep downloaded attachment names inside the requested directory."""
    leaf_name = Path(filename).name or 'attachment'
    return re.sub(r'[<>:"/\\|?*\x00-\x1f]', '_', leaf_name)


def _unique_attachment_path(output_path: Path, target_name: str, attachment_id: str) -> Path:
    """Avoid overwriting earlier downloads when Jira attachments share a filename."""
    candidate = output_path / target_name
    if not candidate.exists():
        return candidate

    target = Path(target_name)
    safe_attachment_id = _safe_filename(attachment_id) or 'attachment'
    stem = target.stem or 'attachment'
    suffix = target.suffix
    candidate = output_path / f"{stem}-{safe_attachment_id}{suffix}"
    counter = 1
    while candidate.exists():
        candidate = output_path / f"{stem}-{safe_attachment_id}-{counter}{suffix}"
        counter += 1
    return candidate


def _get_issue_attachments(client: Any, issue_key: str) -> List[Dict[str, Any]]:
    issue_data = client.get(client.api_path(f'issue/{issue_key}'), params={'fields': 'attachment'})
    return issue_data.get('fields', {}).get('attachment', [])


def _download_attachment_data(
    client: Any,
    attachment_data: Dict[str, Any],
    output_dir: str,
    filename: Optional[str] = None
) -> Dict[str, Any]:
    attachment = _simplify_attachment(attachment_data)
    content_url = attachment.get('content')
    if not content_url:
        raise ValidationError('attachment content URL is missing')

    content, _headers = client.download(content_url)
    output_path = Path(output_dir)
    output_path.mkdir(parents=True, exist_ok=True)
    target_name = _safe_filename(filename or attachment.get('filename') or f"attachment-{attachment.get('id')}")
    target_path = _unique_attachment_path(output_path, target_name, attachment.get('id') or 'attachment')
    target_path.write_bytes(content)

    attachment['path'] = str(target_path)
    attachment['bytes_written'] = len(content)
    return attachment


def jira_get_issue(
    issue_key: str,
    fields: Optional[str] = None,
    expand: Optional[str] = None,
    credentials: Optional[AtlassianCredentials] = None
) -> str:
    """Retrieve a Jira issue by key.

    Args:
        issue_key: Issue key (e.g., 'PROJ-123')
        fields: Comma-separated list of fields to return (optional)
        expand: Comma-separated list of entities to expand (optional)
        credentials: Optional AtlassianCredentials for Agent environments.
                    If not provided, uses environment variables.

    Returns:
        JSON string with issue data or error information
    """
    try:
        client = get_jira_client(credentials)

        params: Dict[str, Any] = {}
        if fields:
            params['fields'] = fields
        if expand:
            params['expand'] = expand

        issue_data = client.get(client.api_path(f'issue/{issue_key}'), params=params)
        simplified = simplify_issue(issue_data)
        return format_json_response(simplified)

    except ConfigurationError as e:
        return format_error_response('ConfigurationError', str(e))
    except AuthenticationError as e:
        return format_error_response('AuthenticationError', str(e))
    except NotFoundError as e:
        return format_error_response('NotFoundError', str(e))
    except (APIError, NetworkError) as e:
        return format_error_response(type(e).__name__, str(e))
    except Exception as e:
        return format_error_response('UnexpectedError', f'Unexpected error: {str(e)}')


def jira_get_comments(
    issue_key: str,
    limit: int = 50,
    start_at: int = 0,
    credentials: Optional[AtlassianCredentials] = None
) -> str:
    """Retrieve comments for a Jira issue."""
    try:
        client = get_jira_client(credentials)

        if not issue_key:
            raise ValidationError('issue_key is required')
        if limit < 0:
            raise ValidationError('limit must be non-negative')
        if start_at < 0:
            raise ValidationError('start_at must be non-negative')

        params: Dict[str, Any] = {'maxResults': limit, 'startAt': start_at}
        response = client.get(client.api_path(f'issue/{issue_key}/comment'), params=params)
        comments = [_simplify_comment(comment) for comment in response.get('comments', [])]
        return format_json_response({
            'issue_key': issue_key,
            'comments': comments,
            'total': response.get('total', len(comments)),
            'start_at': response.get('startAt', start_at),
            'max_results': response.get('maxResults', limit)
        })

    except ConfigurationError as e:
        return format_error_response('ConfigurationError', str(e))
    except AuthenticationError as e:
        return format_error_response('AuthenticationError', str(e))
    except ValidationError as e:
        return format_error_response('ValidationError', str(e))
    except NotFoundError as e:
        return format_error_response('NotFoundError', str(e))
    except (APIError, NetworkError) as e:
        return format_error_response(type(e).__name__, str(e))
    except Exception as e:
        return format_error_response('UnexpectedError', f'Unexpected error: {str(e)}')


def jira_get_attachments(
    issue_key: str,
    credentials: Optional[AtlassianCredentials] = None
) -> str:
    """Retrieve attachment metadata for a Jira issue."""
    try:
        client = get_jira_client(credentials)

        if not issue_key:
            raise ValidationError('issue_key is required')

        attachments = [_simplify_attachment(item) for item in _get_issue_attachments(client, issue_key)]
        return format_json_response({
            'issue_key': issue_key,
            'attachments': attachments,
            'count': len(attachments)
        })

    except ConfigurationError as e:
        return format_error_response('ConfigurationError', str(e))
    except AuthenticationError as e:
        return format_error_response('AuthenticationError', str(e))
    except ValidationError as e:
        return format_error_response('ValidationError', str(e))
    except NotFoundError as e:
        return format_error_response('NotFoundError', str(e))
    except (APIError, NetworkError) as e:
        return format_error_response(type(e).__name__, str(e))
    except Exception as e:
        return format_error_response('UnexpectedError', f'Unexpected error: {str(e)}')


def jira_download_attachment(
    attachment_id: str,
    output_dir: str = '.',
    filename: Optional[str] = None,
    credentials: Optional[AtlassianCredentials] = None
) -> str:
    """Download one Jira attachment by ID."""
    try:
        client = get_jira_client(credentials)

        if not attachment_id:
            raise ValidationError('attachment_id is required')
        if not output_dir:
            raise ValidationError('output_dir is required')

        attachment_data = client.get(client.api_path(f'attachment/{attachment_id}'))
        attachment = _download_attachment_data(client, attachment_data, output_dir, filename)
        return format_json_response({'success': True, 'attachment': attachment})

    except ConfigurationError as e:
        return format_error_response('ConfigurationError', str(e))
    except AuthenticationError as e:
        return format_error_response('AuthenticationError', str(e))
    except ValidationError as e:
        return format_error_response('ValidationError', str(e))
    except NotFoundError as e:
        return format_error_response('NotFoundError', str(e))
    except (APIError, NetworkError) as e:
        return format_error_response(type(e).__name__, str(e))
    except Exception as e:
        return format_error_response('UnexpectedError', f'Unexpected error: {str(e)}')


def jira_download_attachments(
    issue_key: str,
    output_dir: str,
    credentials: Optional[AtlassianCredentials] = None
) -> str:
    """Download all attachments from a Jira issue."""
    try:
        client = get_jira_client(credentials)

        if not issue_key:
            raise ValidationError('issue_key is required')
        if not output_dir:
            raise ValidationError('output_dir is required')

        attachments = [
            _download_attachment_data(client, item, output_dir)
            for item in _get_issue_attachments(client, issue_key)
        ]
        return format_json_response({
            'success': True,
            'issue_key': issue_key,
            'attachments': attachments,
            'count': len(attachments)
        })

    except ConfigurationError as e:
        return format_error_response('ConfigurationError', str(e))
    except AuthenticationError as e:
        return format_error_response('AuthenticationError', str(e))
    except ValidationError as e:
        return format_error_response('ValidationError', str(e))
    except NotFoundError as e:
        return format_error_response('NotFoundError', str(e))
    except (APIError, NetworkError) as e:
        return format_error_response(type(e).__name__, str(e))
    except Exception as e:
        return format_error_response('UnexpectedError', f'Unexpected error: {str(e)}')
