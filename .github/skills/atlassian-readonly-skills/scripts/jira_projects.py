"""Jira projects and versions tools.

Tools:
    - jira_get_all_projects: Get all accessible projects
    - jira_get_project_issues: Get issues for a project
    - jira_get_project_versions: Get versions for a project
"""

import sys
from pathlib import Path
sys.path.insert(0, str(Path(__file__).parent))

from typing import Any, Dict

from _common import (
    get_jira_client,
    simplify_issue,
    format_json_response,
    format_error_response,
    ConfigurationError,
    AuthenticationError,
    ValidationError,
    APIError,
    NetworkError,
)


def _simplify_project(project_data: Dict[str, Any]) -> Dict[str, Any]:
    """Simplify project data to essential fields."""
    return {
        'id': project_data.get('id', ''),
        'key': project_data.get('key', ''),
        'name': project_data.get('name', ''),
        'project_type': project_data.get('projectTypeKey', ''),
        'style': project_data.get('style', ''),
        'is_private': project_data.get('isPrivate', False)
    }


def _simplify_version(version_data: Dict[str, Any]) -> Dict[str, Any]:
    """Simplify version data to essential fields."""
    return {
        'id': version_data.get('id', ''),
        'name': version_data.get('name', ''),
        'description': version_data.get('description', ''),
        'released': version_data.get('released', False),
        'archived': version_data.get('archived', False),
        'start_date': version_data.get('startDate', ''),
        'release_date': version_data.get('releaseDate', '')
    }


def jira_get_all_projects(include_archived: bool = False,
    credentials: Optional[AtlassianCredentials] = None) -> str:
    """Get all accessible Jira projects.
    
    Args:
        include_archived: Whether to include archived projects (default: False)
    
    Returns:
        JSON string with list of projects or error information
    """
    try:
        client = get_jira_client(credentials)
        
        params: Dict[str, Any] = {'expand': 'description'}
        if include_archived:
            params['includeArchived'] = 'true'
        
        response = client.get(client.api_path('project'), params=params)
        
        projects = [_simplify_project(p) for p in response]
        
        result = {
            'projects': projects,
            'count': len(projects)
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


def jira_get_project_issues(
    project_key: str,
    limit: int = 10,
    start_at: int = 0
,
    credentials: Optional[AtlassianCredentials] = None) -> str:
    """Get all issues for a specific Jira project.
    
    Args:
        project_key: Project key (e.g., 'PROJ')
        limit: Maximum number of results (default: 10)
        start_at: Index of first result for pagination (default: 0)
    
    Returns:
        JSON string with list of issues or error information
    """
    try:
        client = get_jira_client(credentials)
        
        if not project_key:
            raise ValidationError('project_key is required')
        
        params: Dict[str, Any] = {
            'jql': f'project = {project_key}',
            'maxResults': limit,
            'startAt': start_at
        }
        
        response = client.get(client.api_path('search'), params=params)
        issues = [simplify_issue(i) for i in response.get('issues', [])]
        
        result = {
            'issues': issues,
            'total': response.get('total', 0),
            'start_at': start_at,
            'project_key': project_key,
            'is_last': start_at + len(issues) >= response.get('total', 0)
        }
        
        return format_json_response(result)
        
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


def jira_get_project_versions(project_key: str,
    credentials: Optional[AtlassianCredentials] = None) -> str:
    """Get all versions for a specific Jira project.
    
    Args:
        project_key: Project key (e.g., 'PROJ')
    
    Returns:
        JSON string with list of versions or error information
    """
    try:
        client = get_jira_client(credentials)
        
        if not project_key:
            raise ValidationError('project_key is required')
        
        response = client.get(client.api_path(f'project/{project_key}/versions'))
        versions = [_simplify_version(v) for v in response]
        
        result = {
            'versions': versions,
            'count': len(versions),
            'project_key': project_key
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
