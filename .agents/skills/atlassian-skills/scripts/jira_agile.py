"""Jira agile boards and sprints tools.

Tools:
    - jira_get_agile_boards: Search for agile boards
    - jira_get_board_issues: Get issues from a board
    - jira_get_sprints_from_board: Get sprints from a board
    - jira_get_sprint_issues: Get issues in a sprint
    - jira_create_sprint: Create a new sprint
    - jira_update_sprint: Update a sprint
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


def _simplify_board(board_data: Dict[str, Any]) -> Dict[str, Any]:
    """Simplify board data to essential fields."""
    return {
        'id': str(board_data.get('id', '')),
        'name': board_data.get('name', ''),
        'type': board_data.get('type', '')
    }


def _simplify_sprint(sprint_data: Dict[str, Any]) -> Dict[str, Any]:
    """Simplify sprint data to essential fields."""
    return {
        'id': str(sprint_data.get('id', '')),
        'name': sprint_data.get('name', ''),
        'state': sprint_data.get('state', ''),
        'start_date': sprint_data.get('startDate', ''),
        'end_date': sprint_data.get('endDate', ''),
        'goal': sprint_data.get('goal', '')
    }


def jira_get_agile_boards(
    board_name: Optional[str] = None,
    project_key: Optional[str] = None,
    board_type: Optional[str] = None,
    start_at: int = 0,
    limit: int = 50
,
    credentials: Optional[AtlassianCredentials] = None) -> str:
    """Search for Jira agile boards.
    
    Args:
        board_name: Board name to search for (optional)
        project_key: Project key to filter boards (optional)
        board_type: Board type filter ('scrum' or 'kanban') (optional)
        start_at: Index of first result for pagination (default: 0)
        limit: Maximum number of results (default: 50)
    
    Returns:
        JSON string with list of boards or error information
    """
    try:
        client = get_jira_client(credentials)
        
        params: Dict[str, Any] = {
            'startAt': start_at,
            'maxResults': limit
        }
        if board_name:
            params['name'] = board_name
        if project_key:
            params['projectKeyOrId'] = project_key
        if board_type:
            params['type'] = board_type
        
        response = client.get('/rest/agile/1.0/board', params=params)
        
        boards = response.get('values', [])
        simplified_boards = [_simplify_board(b) for b in boards]
        
        result = {
            'boards': simplified_boards,
            'total': response.get('total', len(boards)),
            'start_at': start_at,
            'is_last': response.get('isLast', True)
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


def jira_get_board_issues(
    board_id: str,
    jql: Optional[str] = None,
    fields: Optional[str] = None,
    start_at: int = 0,
    limit: int = 50
,
    credentials: Optional[AtlassianCredentials] = None) -> str:
    """Get issues from a Jira agile board.
    
    Args:
        board_id: Board ID to get issues from
        jql: JQL query to filter issues (optional)
        fields: Comma-separated list of fields to return (optional)
        start_at: Index of first result for pagination (default: 0)
        limit: Maximum number of results (default: 50)
    
    Returns:
        JSON string with list of issues or error information
    """
    try:
        client = get_jira_client(credentials)
        
        if not board_id:
            raise ValidationError('board_id is required')
        
        params: Dict[str, Any] = {
            'startAt': start_at,
            'maxResults': limit
        }
        if jql:
            params['jql'] = jql
        if fields:
            params['fields'] = fields
        
        response = client.get(
            f'/rest/agile/1.0/board/{board_id}/issue', params=params
        )
        
        issues = [simplify_issue(i) for i in response.get('issues', [])]
        
        result = {
            'issues': issues,
            'total': response.get('total', len(issues)),
            'start_at': start_at,
            'board_id': board_id
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


def jira_get_sprints_from_board(
    board_id: str,
    state: Optional[str] = None,
    start_at: int = 0,
    limit: int = 50
,
    credentials: Optional[AtlassianCredentials] = None) -> str:
    """Get sprints from a Jira agile board.
    
    Args:
        board_id: Board ID to get sprints from
        state: Sprint state filter ('active', 'future', 'closed') (optional)
        start_at: Index of first result for pagination (default: 0)
        limit: Maximum number of results (default: 50)
    
    Returns:
        JSON string with list of sprints or error information
    """
    try:
        client = get_jira_client(credentials)
        
        if not board_id:
            raise ValidationError('board_id is required')
        
        params: Dict[str, Any] = {
            'startAt': start_at,
            'maxResults': limit
        }
        if state:
            params['state'] = state
        
        response = client.get(
            f'/rest/agile/1.0/board/{board_id}/sprint', params=params
        )
        
        sprints = response.get('values', [])
        simplified_sprints = [_simplify_sprint(s) for s in sprints]
        
        result = {
            'sprints': simplified_sprints,
            'total': len(simplified_sprints),
            'start_at': start_at,
            'board_id': board_id,
            'is_last': response.get('isLast', True)
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


def jira_get_sprint_issues(
    sprint_id: str,
    fields: Optional[str] = None,
    start_at: int = 0,
    limit: int = 50
,
    credentials: Optional[AtlassianCredentials] = None) -> str:
    """Get all issues in a specific sprint.
    
    Args:
        sprint_id: Sprint ID to get issues from
        fields: Comma-separated list of fields to return (optional)
        start_at: Index of first result for pagination (default: 0)
        limit: Maximum number of results (default: 50)
    
    Returns:
        JSON string with list of issues or error information
    """
    try:
        client = get_jira_client(credentials)
        
        if not sprint_id:
            raise ValidationError('sprint_id is required')
        
        params: Dict[str, Any] = {
            'startAt': start_at,
            'maxResults': limit
        }
        if fields:
            params['fields'] = fields
        
        response = client.get(
            f'/rest/agile/1.0/sprint/{sprint_id}/issue', params=params
        )
        
        issues = [simplify_issue(i) for i in response.get('issues', [])]
        
        result = {
            'issues': issues,
            'total': response.get('total', len(issues)),
            'start_at': start_at,
            'sprint_id': sprint_id
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


def jira_create_sprint(
    board_id: str,
    sprint_name: str,
    start_date: str,
    end_date: str,
    goal: Optional[str] = None
,
    credentials: Optional[AtlassianCredentials] = None) -> str:
    """Create a new sprint on a Jira agile board.
    
    Args:
        board_id: Board ID to create the sprint on
        sprint_name: Name for the new sprint
        start_date: Start date in ISO format
        end_date: End date in ISO format
        goal: Sprint goal description (optional)
    
    Returns:
        JSON string with created sprint data or error information
    """
    try:
        client = get_jira_client(credentials)
        
        if not board_id:
            raise ValidationError('board_id is required')
        if not sprint_name:
            raise ValidationError('sprint_name is required')
        if not start_date:
            raise ValidationError('start_date is required')
        if not end_date:
            raise ValidationError('end_date is required')
        
        payload: Dict[str, Any] = {
            'name': sprint_name,
            'originBoardId': int(board_id),
            'startDate': start_date,
            'endDate': end_date
        }
        if goal:
            payload['goal'] = goal
        
        response = client.post('/rest/agile/1.0/sprint', json=payload)
        simplified = _simplify_sprint(response)
        
        return format_json_response(simplified)
        
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


def jira_update_sprint(
    sprint_id: str,
    sprint_name: Optional[str] = None,
    state: Optional[str] = None,
    start_date: Optional[str] = None,
    end_date: Optional[str] = None,
    goal: Optional[str] = None
,
    credentials: Optional[AtlassianCredentials] = None) -> str:
    """Update an existing sprint's configuration.
    
    Args:
        sprint_id: Sprint ID to update
        sprint_name: New name for the sprint (optional)
        state: New state ('future', 'active', 'closed') (optional)
        start_date: New start date in ISO format (optional)
        end_date: New end date in ISO format (optional)
        goal: New sprint goal description (optional)
    
    Returns:
        JSON string with updated sprint data or error information
    """
    try:
        client = get_jira_client(credentials)
        
        if not sprint_id:
            raise ValidationError('sprint_id is required')
        
        payload: Dict[str, Any] = {}
        if sprint_name is not None:
            payload['name'] = sprint_name
        if state is not None:
            payload['state'] = state
        if start_date is not None:
            payload['startDate'] = start_date
        if end_date is not None:
            payload['endDate'] = end_date
        if goal is not None:
            payload['goal'] = goal
        
        if not payload:
            raise ValidationError('At least one field to update is required')
        
        response = client.put(f'/rest/agile/1.0/sprint/{sprint_id}', json=payload)
        simplified = _simplify_sprint(response)
        
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
