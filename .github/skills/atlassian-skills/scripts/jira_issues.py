"""Jira issue management tools.

Tools:
    - jira_get_issue: Retrieve an issue by key
    - jira_create_issue: Create a new issue
    - jira_update_issue: Update an existing issue
    - jira_delete_issue: Delete an issue
    - jira_add_comment: Add a comment to an issue
"""

import sys
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


def jira_create_issue(
    project_key: str,
    summary: str,
    issue_type: str,
    description: Optional[str] = None,
    assignee: Optional[str] = None,
    priority: Optional[str] = None,
    labels: Optional[List[str]] = None,
    custom_fields: Optional[Dict[str, Any]] = None,
    credentials: Optional[AtlassianCredentials] = None
) -> str:
    """Create a new Jira issue.
    
    Args:
        project_key: Project key (e.g., 'PROJ')
        summary: Issue summary/title
        issue_type: Issue type (e.g., 'Task', 'Bug', 'Story')
        description: Issue description (optional)
        assignee: Assignee account ID (optional)
        priority: Priority name (e.g., 'High', 'Medium', 'Low') (optional)
        labels: List of label strings (optional)
        custom_fields: Dictionary of custom field IDs to values (optional)
                      e.g., {'customfield_10001': 'value', 'customfield_10002': 123}
        credentials: Optional AtlassianCredentials for Agent environments.
                    If not provided, uses environment variables.
    
    Returns:
        JSON string with created issue data or error information
    """
    try:
        client = get_jira_client(credentials)
        
        if not project_key:
            raise ValidationError('project_key is required')
        if not summary:
            raise ValidationError('summary is required')
        if not issue_type:
            raise ValidationError('issue_type is required')
        
        fields: Dict[str, Any] = {
            'project': {'key': project_key},
            'summary': summary,
            'issuetype': {'name': issue_type}
        }
        
        if description:
            fields['description'] = description
        if assignee:
            fields['assignee'] = {'accountId': assignee}
        if priority:
            fields['priority'] = {'name': priority}
        if labels:
            fields['labels'] = labels
        if custom_fields:
            fields.update(custom_fields)
        
        payload = {'fields': fields}
        response = client.post(client.api_path('issue'), json=payload)
        
        issue_key = response.get('key')
        if issue_key:
            return jira_get_issue(issue_key, credentials=credentials)
        return format_json_response(response)
        
    except ConfigurationError as e:
        return format_error_response('ConfigurationError', str(e))
    except AuthenticationError as e:
        return format_error_response('AuthenticationError', str(e))
    except ValidationError as e:
        return format_error_response('ValidationError', str(e))
    except (APIError, NetworkError) as e:
        return format_error_response(type(e).__name__, str(e))
    except Exception as e:
        return format_error_response('UnexpectedError', f'Unexpected error: {str(e)}')


def jira_update_issue(
    issue_key: str,
    summary: Optional[str] = None,
    description: Optional[str] = None,
    assignee: Optional[str] = None,
    priority: Optional[str] = None,
    labels: Optional[List[str]] = None,
    custom_fields: Optional[Dict[str, Any]] = None,
    credentials: Optional[AtlassianCredentials] = None
) -> str:
    """Update an existing Jira issue.
    
    Args:
        issue_key: Issue key (e.g., 'PROJ-123')
        summary: New summary/title (optional)
        description: New description (optional)
        assignee: New assignee account ID (optional)
        priority: New priority name (optional)
        labels: New list of labels (optional)
        custom_fields: Dictionary of custom field IDs to values (optional)
                      e.g., {'customfield_10001': 'value', 'customfield_10002': 123}
        credentials: Optional AtlassianCredentials for Agent environments.
                    If not provided, uses environment variables.
    
    Returns:
        JSON string with updated issue data or error information
    """
    try:
        client = get_jira_client(credentials)
        
        if not issue_key:
            raise ValidationError('issue_key is required')
        
        fields: Dict[str, Any] = {}
        if summary is not None:
            fields['summary'] = summary
        if description is not None:
            fields['description'] = description
        if assignee is not None:
            fields['assignee'] = {'accountId': assignee}
        if priority is not None:
            fields['priority'] = {'name': priority}
        if labels is not None:
            fields['labels'] = labels
        if custom_fields is not None:
            fields.update(custom_fields)
        
        if not fields:
            raise ValidationError('At least one field to update is required')
        
        payload: Dict[str, Any] = {'fields': fields}
        client.put(client.api_path(f'issue/{issue_key}'), json=payload)
        
        return jira_get_issue(issue_key, credentials=credentials)
        
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


def jira_delete_issue(
    issue_key: str,
    delete_subtasks: bool = False,
    credentials: Optional[AtlassianCredentials] = None
) -> str:
    """Delete a Jira issue.
    
    Args:
        issue_key: Issue key (e.g., 'PROJ-123')
        delete_subtasks: Whether to delete subtasks (default: False)
        credentials: Optional AtlassianCredentials for Agent environments.
                    If not provided, uses environment variables.
    
    Returns:
        JSON string with success message or error information
    """
    try:
        client = get_jira_client(credentials)
        
        if not issue_key:
            raise ValidationError('issue_key is required')
        
        path = client.api_path(f'issue/{issue_key}')
        if delete_subtasks:
            path += '?deleteSubtasks=true'
        
        client.delete(path)
        
        return format_json_response({
            'success': True,
            'message': f'Issue {issue_key} deleted successfully'
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


def jira_add_comment(
    issue_key: str,
    comment: str,
    credentials: Optional[AtlassianCredentials] = None
) -> str:
    """Add a comment to a Jira issue.
    
    Args:
        issue_key: Issue key (e.g., 'PROJ-123')
        comment: Comment text
        credentials: Optional AtlassianCredentials for Agent environments.
                    If not provided, uses environment variables.
    
    Returns:
        JSON string with comment data or error information
    """
    try:
        client = get_jira_client(credentials)
        
        if not issue_key:
            raise ValidationError('issue_key is required')
        if not comment:
            raise ValidationError('comment text is required')
        
        # Jira Cloud API v3 requires ADF (Atlassian Document Format)
        if client.api_version == "3":
            payload: Dict[str, Any] = {
                'body': {
                    'type': 'doc',
                    'version': 1,
                    'content': [
                        {
                            'type': 'paragraph',
                            'content': [
                                {'type': 'text', 'text': comment}
                            ]
                        }
                    ]
                }
            }
        else:
            payload: Dict[str, Any] = {'body': comment}
        response = client.post(client.api_path(f'issue/{issue_key}/comment'), json=payload)
        
        simplified = {
            'id': response.get('id', ''),
            'author': response.get('author', {}).get('emailAddress', ''),
            'body': response.get('body', ''),
            'created': response.get('created', ''),
            'updated': response.get('updated', '')
        }
        
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
