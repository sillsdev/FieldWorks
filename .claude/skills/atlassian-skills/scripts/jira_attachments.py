"""Jira attachment tools.

Tools:
    - jira_add_attachment: Upload one or more files to an issue

Attachments need a multipart POST, which AtlassianClient.post cannot do --
it only sends JSON. This module therefore drives client.session directly,
reusing the client's base URL, auth, SSL setting and error handling.
"""

import mimetypes
import os
import sys
from pathlib import Path
sys.path.insert(0, str(Path(__file__).parent))

from typing import Any, Dict, List, Optional, Union

from _common import (
    AtlassianCredentials,
    get_jira_client,
    format_json_response,
    format_error_response,
    ConfigurationError,
    AuthenticationError,
    ValidationError,
    NotFoundError,
    APIError,
    NetworkError,
)

# Jira Data Center's default ceiling. A larger file fails server-side with a
# message that does not name the limit, so check it here where we can say so.
DEFAULT_MAX_BYTES = 10 * 1024 * 1024


def jira_add_attachment(
    issue_key: str,
    file_paths: Union[str, List[str]],
    credentials: Optional[AtlassianCredentials] = None,
    max_bytes: int = DEFAULT_MAX_BYTES
) -> str:
    """Attach one or more files to a Jira issue.

    Args:
        issue_key: Issue key (e.g., 'LT-22715')
        file_paths: A path, or a list of paths, to upload
        credentials: Optional AtlassianCredentials for Agent environments.
                    If not provided, uses environment variables.
        max_bytes: Reject any file larger than this before uploading

    Returns:
        JSON string with one entry per attachment, each carrying id,
        filename, size and the content URL, or error information

    Note:
        Attachments are visible to everyone who can see the issue. Confirm
        permission to publish before calling this with user data --
        FieldWorks projects, screenshots of live data, and logs frequently
        contain unpublished language material.
    """
    handles = []
    try:
        client = get_jira_client(credentials)

        if not issue_key:
            raise ValidationError('issue_key is required')
        if not file_paths:
            raise ValidationError('at least one file path is required')

        if isinstance(file_paths, str):
            file_paths = [file_paths]

        for path in file_paths:
            if not os.path.isfile(path):
                raise ValidationError(f'file not found: {path}')
            size = os.path.getsize(path)
            if size == 0:
                raise ValidationError(f'file is empty: {path}')
            if size > max_bytes:
                raise ValidationError(
                    f'file is {size} bytes, over the {max_bytes} byte limit: {path}'
                )

        files = []
        for path in file_paths:
            name = os.path.basename(path)
            mime = mimetypes.guess_type(name)[0] or 'application/octet-stream'
            handle = open(path, 'rb')
            handles.append(handle)
            files.append(('file', (name, handle, mime)))

        url = f"{client.config.url}{client.api_path(f'issue/{issue_key}/attachments')}"

        # X-Atlassian-Token defeats Jira's XSRF check, which otherwise rejects
        # the upload. Content-Type must be cleared so requests can set the
        # multipart boundary; the session sets application/json for every
        # other call, and a None value here removes it for this one.
        response = client.session.post(
            url,
            files=files,
            headers={'X-Atlassian-Token': 'no-check', 'Content-Type': None},
            timeout=120,
            verify=client.ssl_verify
        )
        client._handle_error(response)

        uploaded: List[Dict[str, Any]] = []
        for item in (response.json() if response.content else []):
            uploaded.append({
                'id': item.get('id', ''),
                'filename': item.get('filename', ''),
                'size': item.get('size', 0),
                'mimeType': item.get('mimeType', ''),
                'content': item.get('content', ''),
                'thumbnail': item.get('thumbnail', '')
            })

        return format_json_response({
            'issue_key': issue_key,
            'count': len(uploaded),
            'attachments': uploaded
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
    finally:
        for handle in handles:
            handle.close()
