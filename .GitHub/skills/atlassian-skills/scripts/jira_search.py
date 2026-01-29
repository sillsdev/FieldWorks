"""Jira search tools.

Tools:
    - jira_search: Search issues using JQL
    - jira_search_fields: Search field definitions
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
    APIError,
    NetworkError,
)


def jira_search(
    jql: str,
    fields: Optional[str] = None,
    limit: int = 10,
    start_at: int = 0,
    credentials: Optional[AtlassianCredentials] = None
) -> str:
    """Search for Jira issues using JQL.
    
    Args:
        jql: JQL query string (e.g., 'project = PROJ AND status = Open')
        fields: Comma-separated list of fields to return (optional)
        limit: Maximum number of results to return (default: 10)
        start_at: Index of the first result for pagination (default: 0)
        credentials: Optional AtlassianCredentials for Agent environments.
                    If not provided, uses environment variables.
    
    Returns:
        JSON string with search results and pagination metadata or error
    """
    try:
        client = get_jira_client(credentials)
        
        if not jql:
            raise ValidationError('jql query is required')
        if limit < 0:
            raise ValidationError('limit must be non-negative')
        if start_at < 0:
            raise ValidationError('start_at must be non-negative')
        
        # Use new search/jql endpoint for Jira Cloud API v3
        if client.api_version == "3":
            # New API uses GET with query params
            params: Dict[str, Any] = {
                'jql': jql,
                'maxResults': limit,
            }
            if fields:
                params['fields'] = fields
            response = client.get(client.api_path('search/jql'), params=params)
        else:
            params: Dict[str, Any] = {
                'jql': jql,
                'maxResults': limit,
                'startAt': start_at
            }
            if fields:
                params['fields'] = fields
            response = client.get(client.api_path('search'), params=params)
        issues = [simplify_issue(issue) for issue in response.get('issues', [])]
        
        result = {
            'issues': issues,
            'total': response.get('total', 0),
            'start_at': response.get('startAt', start_at),
            'max_results': response.get('maxResults', limit),
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


def jira_search_fields(
    keyword: str = "",
    limit: int = 10,
    credentials: Optional[AtlassianCredentials] = None
) -> str:
    """Search for Jira field definitions by keyword.
    
    Args:
        keyword: Keyword to search for in field names (default: "")
        limit: Maximum number of results to return (default: 10)
        credentials: Optional AtlassianCredentials for Agent environments.
                    If not provided, uses environment variables.
    
    Returns:
        JSON string with field definitions or error information
    """
    try:
        client = get_jira_client(credentials)
        
        if limit < 0:
            raise ValidationError('limit must be non-negative')
        
        fields = client.get(client.api_path('field'))
        
        if keyword:
            keyword_lower = keyword.lower()
            filtered_fields = [
                field for field in fields
                if (keyword_lower in field.get('name', '').lower() or
                    keyword_lower in field.get('id', '').lower() or
                    keyword_lower in field.get('description', '').lower())
            ]
        else:
            filtered_fields = fields
        
        limited_fields = filtered_fields[:limit]
        
        simplified_fields = []
        for field in limited_fields:
            simplified = {
                'id': field.get('id', ''),
                'name': field.get('name', ''),
                'description': field.get('description', ''),
                'custom': field.get('custom', False),
                'schema': field.get('schema', {})
            }
            simplified_fields.append(simplified)
        
        result = {
            'fields': simplified_fields,
            'total': len(filtered_fields),
            'returned': len(simplified_fields)
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
