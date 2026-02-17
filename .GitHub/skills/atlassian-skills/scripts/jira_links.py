"""Jira issue linking tools.

Tools:
    - jira_get_link_types: Get available link types
    - jira_create_issue_link: Create a link between issues
    - jira_link_to_epic: Link an issue to an epic
    - jira_remove_issue_link: Remove a link
"""

import sys
from pathlib import Path
sys.path.insert(0, str(Path(__file__).parent))

from typing import Any, Dict, Optional

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


def _simplify_link_type(link_type_data: Dict[str, Any]) -> Dict[str, Any]:
    """Simplify link type data to essential fields."""
    return {
        'id': link_type_data.get('id', ''),
        'name': link_type_data.get('name', ''),
        'inward': link_type_data.get('inward', ''),
        'outward': link_type_data.get('outward', ''),
    }


def jira_get_link_types(
    credentials: Optional[AtlassianCredentials] = None) -> str:
    """Get all available issue link types.
    
    Returns:
        JSON string with list of link types or error information
    """
    try:
        client = get_jira_client(credentials)
        
        response = client.get(client.api_path('issueLinkType'))
        
        link_types = response.get('issueLinkTypes', [])
        simplified_link_types = [_simplify_link_type(lt) for lt in link_types]
        
        result = {
            'link_types': simplified_link_types,
            'count': len(simplified_link_types)
        }
        
        return format_json_response(result)
        
    except ConfigurationError as e:
        return format_error_response('ConfigurationError', str(e))
    except AuthenticationError as e:
        return format_error_response('AuthenticationError', str(e))
    except (APIError, NetworkError) as e:
        return format_error_response(type(e).__name__, str(e))
    except Exception as e:
        return format_error_response('UnexpectedError', f'Unexpected error: {str(e)}')


def jira_create_issue_link(
    link_type: str,
    inward_issue_key: str,
    outward_issue_key: str,
    comment: Optional[str] = None
,
    credentials: Optional[AtlassianCredentials] = None) -> str:
    """Create a link between two Jira issues.
    
    Args:
        link_type: Name of the link type (e.g., 'Duplicate', 'Blocks', 'Relates')
        inward_issue_key: Issue key for the inward/source issue
        outward_issue_key: Issue key for the outward/target issue
        comment: Optional comment to add with the link
    
    Returns:
        JSON string with success message or error information
    """
    try:
        client = get_jira_client(credentials)
        
        if not link_type:
            raise ValidationError('link_type is required')
        if not inward_issue_key:
            raise ValidationError('inward_issue_key is required')
        if not outward_issue_key:
            raise ValidationError('outward_issue_key is required')
        
        payload: Dict[str, Any] = {
            'type': {'name': link_type},
            'inwardIssue': {'key': inward_issue_key},
            'outwardIssue': {'key': outward_issue_key}
        }
        
        if comment:
            payload['comment'] = {'body': comment}
        
        client.post(client.api_path('issueLink'), json=payload)
        
        result = {
            'success': True,
            'message': f'Link created between {inward_issue_key} and {outward_issue_key}',
            'link_type': link_type,
            'inward_issue': inward_issue_key,
            'outward_issue': outward_issue_key
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


def jira_link_to_epic(issue_key: str, epic_key: str,
    credentials: Optional[AtlassianCredentials] = None) -> str:
    """Link an issue to an epic.
    
    Args:
        issue_key: Issue key to link (e.g., 'PROJ-123')
        epic_key: Epic key to link to (e.g., 'PROJ-456')
    
    Returns:
        JSON string with success message or error information
    """
    try:
        client = get_jira_client(credentials)
        
        if not issue_key:
            raise ValidationError('issue_key is required')
        if not epic_key:
            raise ValidationError('epic_key is required')
        
        # Verify the epic exists
        epic_data = client.get(client.api_path(f'issue/{epic_key}'))
        epic_fields = epic_data.get('fields', {})
        issue_type = epic_fields.get('issuetype', {})
        issue_type_name = issue_type.get('name', '').lower() if issue_type else ''
        
        if 'epic' not in issue_type_name:
            raise ValidationError(
                f'{epic_key} is not an Epic (type: {issue_type.get("name", "unknown")})'
            )
        
        # Verify the issue exists
        client.get(client.api_path(f'issue/{issue_key}'))
        
        # Try Method 1: Use parent field
        try:
            payload = {'fields': {'parent': {'key': epic_key}}}
            client.put(client.api_path(f'issue/{issue_key}'), json=payload)
            
            return format_json_response({
                'success': True,
                'message': f'Issue {issue_key} linked to epic {epic_key}',
                'issue_key': issue_key,
                'epic_key': epic_key,
                'method': 'parent'
            })
        except (APIError, ValidationError):
            pass
        
        # Try Method 2: Common Epic Link custom fields
        common_epic_fields = [
            'customfield_10014',
            'customfield_10008',
            'customfield_10000',
            'customfield_10001',
        ]
        
        for field_id in common_epic_fields:
            try:
                payload = {'fields': {field_id: epic_key}}
                client.put(client.api_path(f'issue/{issue_key}'), json=payload)
                
                return format_json_response({
                    'success': True,
                    'message': f'Issue {issue_key} linked to epic {epic_key}',
                    'issue_key': issue_key,
                    'epic_key': epic_key,
                    'method': field_id
                })
            except (APIError, ValidationError):
                continue
        
        # Try Method 3: Create a "Relates to" link as fallback
        try:
            link_payload = {
                'type': {'name': 'Relates'},
                'inwardIssue': {'key': issue_key},
                'outwardIssue': {'key': epic_key}
            }
            client.post(client.api_path('issueLink'), json=link_payload)
            
            return format_json_response({
                'success': True,
                'message': f'Issue {issue_key} linked to epic {epic_key} (via Relates)',
                'issue_key': issue_key,
                'epic_key': epic_key,
                'method': 'relates_link'
            })
        except (APIError, ValidationError):
            pass
        
        raise APIError(
            f'Could not link issue {issue_key} to epic {epic_key}. '
            'Your Jira instance may use a different field for epic links.'
        )
        
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


def jira_remove_issue_link(link_id: str,
    credentials: Optional[AtlassianCredentials] = None) -> str:
    """Remove a link between two issues.
    
    Args:
        link_id: ID of the link to remove
    
    Returns:
        JSON string with success message or error information
    """
    try:
        client = get_jira_client(credentials)
        
        if not link_id:
            raise ValidationError('link_id is required')
        
        client.delete(client.api_path(f'issueLink/{link_id}'))
        
        result = {
            'success': True,
            'message': f'Link with ID {link_id} has been removed',
            'link_id': link_id
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
