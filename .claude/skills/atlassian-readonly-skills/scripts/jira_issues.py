"""Jira issue management tools.

Tools:
    - jira_get_issue: Retrieve an issue by key
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
