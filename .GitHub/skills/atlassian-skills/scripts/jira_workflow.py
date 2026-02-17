"""Jira workflow and transition tools.

Tools:
    - jira_get_transitions: Get available status transitions
    - jira_transition_issue: Transition an issue to a new status
"""

import sys
from pathlib import Path
sys.path.insert(0, str(Path(__file__).parent))

from typing import Any, Dict, Optional

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


def _simplify_transition(transition_data: Dict[str, Any]) -> Dict[str, Any]:
    """Simplify transition data to essential fields."""
    simplified = {
        'id': transition_data.get('id', ''),
        'name': transition_data.get('name', ''),
    }
    
    to_status = transition_data.get('to', {})
    if to_status and isinstance(to_status, dict):
        simplified['to_status'] = to_status.get('name', '')
        simplified['to_status_id'] = to_status.get('id', '')
    
    has_screen = transition_data.get('hasScreen', False)
    simplified['has_screen'] = has_screen
    
    fields = transition_data.get('fields', {})
    if fields:
        required_fields = []
        optional_fields = []
        for field_id, field_info in fields.items():
            field_data = {
                'id': field_id,
                'name': field_info.get('name', field_id),
                'required': field_info.get('required', False),
                'schema': field_info.get('schema', {})
            }
            if field_info.get('required', False):
                required_fields.append(field_data)
            else:
                optional_fields.append(field_data)
        
        if required_fields:
            simplified['required_fields'] = required_fields
        if optional_fields:
            simplified['optional_fields'] = optional_fields
    
    return simplified


def jira_get_transitions(issue_key: str,
    credentials: Optional[AtlassianCredentials] = None) -> str:
    """Get available status transitions for a Jira issue.
    
    Args:
        issue_key: Issue key (e.g., 'PROJ-123')
    
    Returns:
        JSON string with list of available transitions or error information
    """
    try:
        client = get_jira_client(credentials)
        
        if not issue_key:
            raise ValidationError('issue_key is required')
        
        params = {'expand': 'transitions.fields'}
        response = client.get(
            client.api_path(f'issue/{issue_key}/transitions'), params=params
        )
        
        transitions = response.get('transitions', [])
        simplified_transitions = [_simplify_transition(t) for t in transitions]
        
        result = {
            'issue_key': issue_key,
            'transitions': simplified_transitions,
            'count': len(simplified_transitions)
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


def jira_transition_issue(
    issue_key: str,
    transition_id: str,
    fields: Optional[Dict[str, Any]] = None,
    comment: Optional[str] = None
,
    credentials: Optional[AtlassianCredentials] = None) -> str:
    """Transition a Jira issue to a new status.
    
    Args:
        issue_key: Issue key (e.g., 'PROJ-123')
        transition_id: ID of the transition to perform
        fields: Dictionary of fields to set during transition (optional)
        comment: Comment text to add during the transition (optional)
    
    Returns:
        JSON string with the updated issue data or error information
    """
    try:
        client = get_jira_client(credentials)
        
        if not issue_key:
            raise ValidationError('issue_key is required')
        if not transition_id:
            raise ValidationError('transition_id is required')
        
        payload: Dict[str, Any] = {
            'transition': {'id': str(transition_id)}
        }
        
        if fields:
            payload['fields'] = fields
        
        if comment:
            payload['update'] = {
                'comment': [{'add': {'body': comment}}]
            }
        
        client.post(client.api_path(f'issue/{issue_key}/transitions'), json=payload)
        
        issue_data = client.get(client.api_path(f'issue/{issue_key}'))
        simplified = simplify_issue(issue_data)
        
        return format_json_response(simplified)
        
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
