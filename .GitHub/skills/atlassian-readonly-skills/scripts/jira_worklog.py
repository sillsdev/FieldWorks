"""Jira worklog management tools.

Tools:
    - jira_get_worklog: Get worklog entries for an issue
"""

import sys
from pathlib import Path
sys.path.insert(0, str(Path(__file__).parent))

from typing import Any, Dict

from _common import (
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


def _simplify_worklog(worklog_data: Dict[str, Any]) -> Dict[str, Any]:
    """Simplify worklog data to essential fields."""
    author = worklog_data.get('author', {})
    update_author = worklog_data.get('updateAuthor', {})
    
    return {
        'id': worklog_data.get('id', ''),
        'comment': worklog_data.get('comment', ''),
        'created': worklog_data.get('created', ''),
        'updated': worklog_data.get('updated', ''),
        'started': worklog_data.get('started', ''),
        'time_spent': worklog_data.get('timeSpent', ''),
        'time_spent_seconds': worklog_data.get('timeSpentSeconds', 0),
        'author': author.get('displayName', '') or author.get('emailAddress', ''),
        'update_author': (
            update_author.get('displayName', '') or
            update_author.get('emailAddress', '')
        ),
    }


def jira_get_worklog(issue_key: str,
    credentials: Optional[AtlassianCredentials] = None) -> str:
    """Retrieve all worklog entries for a Jira issue.
    
    Args:
        issue_key: Issue key (e.g., 'PROJ-123')
    
    Returns:
        JSON string with worklog entries or error information
    """
    try:
        client = get_jira_client(credentials)
        
        if not issue_key:
            raise ValidationError('issue_key is required')
        
        response = client.get(client.api_path(f'issue/{issue_key}/worklog'))
        
        worklogs = response.get('worklogs', [])
        simplified_worklogs = [_simplify_worklog(wl) for wl in worklogs]
        
        result = {
            'worklogs': simplified_worklogs,
            'count': len(simplified_worklogs),
            'issue_key': issue_key
        }
        
        return format_json_response(result)
        
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
