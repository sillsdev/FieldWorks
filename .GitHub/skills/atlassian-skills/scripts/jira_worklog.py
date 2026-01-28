"""Jira worklog management tools.

Tools:
    - jira_get_worklog: Get worklog entries for an issue
    - jira_add_worklog: Add a worklog entry
"""

import sys
from pathlib import Path
sys.path.insert(0, str(Path(__file__).parent))

from typing import Any, Dict, Optional

from _common import (
    AtlassianCredentials,
    get_jira_client,
    parse_time_spent,
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


def jira_add_worklog(
    issue_key: str,
    time_spent: str,
    comment: Optional[str] = None,
    started: Optional[str] = None,
    original_estimate: Optional[str] = None,
    remaining_estimate: Optional[str] = None
,
    credentials: Optional[AtlassianCredentials] = None) -> str:
    """Add a worklog entry to a Jira issue.
    
    Args:
        issue_key: Issue key (e.g., 'PROJ-123')
        time_spent: Time spent (e.g., '1h 30m', '3h', '1d', '2w')
        comment: Optional comment describing the work done
        started: Optional ISO8601 datetime for when work began
        original_estimate: Optional new value for the original estimate
        remaining_estimate: Optional new value for the remaining estimate
    
    Returns:
        JSON string with created worklog entry or error information
    """
    try:
        client = get_jira_client(credentials)
        
        if not issue_key:
            raise ValidationError('issue_key is required')
        if not time_spent:
            raise ValidationError('time_spent is required')
        
        time_spent_seconds = parse_time_spent(time_spent)
        
        original_estimate_updated = False
        remaining_estimate_updated = False
        
        # Update original estimate if provided
        if original_estimate:
            try:
                parse_time_spent(original_estimate)
                payload = {
                    'fields': {
                        'timetracking': {'originalEstimate': original_estimate}
                    }
                }
                client.put(client.api_path(f'issue/{issue_key}'), json=payload)
                original_estimate_updated = True
            except ValidationError:
                raise ValidationError(
                    f'Invalid original_estimate format: {original_estimate}'
                )
            except (APIError, AuthenticationError):
                pass
        
        # Build worklog payload
        worklog_payload: Dict[str, Any] = {'timeSpentSeconds': time_spent_seconds}
        
        if comment:
            worklog_payload['comment'] = comment
        if started:
            worklog_payload['started'] = started
        
        # Build URL with query parameters for remaining estimate
        url = client.api_path(f'issue/{issue_key}/worklog')
        
        if remaining_estimate:
            try:
                parse_time_spent(remaining_estimate)
                url += f'?adjustEstimate=new&newEstimate={remaining_estimate}'
                remaining_estimate_updated = True
            except ValidationError:
                raise ValidationError(
                    f'Invalid remaining_estimate format: {remaining_estimate}'
                )
        
        response = client.post(url, json=worklog_payload)
        
        result = _simplify_worklog(response)
        result['original_estimate_updated'] = original_estimate_updated
        result['remaining_estimate_updated'] = remaining_estimate_updated
        result['issue_key'] = issue_key
        
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
