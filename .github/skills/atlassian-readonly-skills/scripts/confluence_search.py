"""Confluence search tools.

Tools:
    - confluence_search: Search content using CQL
"""

import sys
from pathlib import Path
sys.path.insert(0, str(Path(__file__).parent))

from typing import Any, Dict, Optional

from _common import (
    AtlassianCredentials,
    get_confluence_client,
    format_json_response,
    format_error_response,
    ConfigurationError,
    AuthenticationError,
    ValidationError,
    APIError,
    NetworkError,
)


def _simplify_search_result(result: Dict[str, Any]) -> Dict[str, Any]:
    """Simplify search result to essential fields."""
    content = result.get('content', result)
    return {
        'id': content.get('id', ''),
        'title': content.get('title', result.get('title', '')),
        'type': content.get('type', ''),
        'space_key': content.get('space', {}).get('key', ''),
        'url': result.get('url', content.get('_links', {}).get('webui', '')),
        'excerpt': result.get('excerpt', ''),
        'last_modified': result.get('lastModified', '')
    }


def confluence_search(
    query: str,
    limit: int = 10,
    start_at: int = 0
,
    credentials: Optional[AtlassianCredentials] = None) -> str:
    """Search for Confluence content using CQL.
    
    Args:
        query: Search query (text or CQL)
        limit: Maximum number of results (default: 10)
        start_at: Index of first result for pagination (default: 0)
    
    Returns:
        JSON string with search results or error information
    """
    try:
        client = get_confluence_client(credentials)
        
        if not query:
            raise ValidationError('query is required')
        if limit < 0:
            raise ValidationError('limit must be non-negative')
        if start_at < 0:
            raise ValidationError('start_at must be non-negative')
        
        # Build CQL query
        if not any(op in query for op in ['=', '~', 'AND', 'OR', 'NOT']):
            cql = f'text ~ "{query}" OR title ~ "{query}"'
        else:
            cql = query
        
        params: Dict[str, Any] = {
            'cql': cql,
            'limit': limit,
            'start': start_at
        }
        
        response = client.get('/rest/api/content/search', params=params)
        
        results = response.get('results', [])
        simplified_results = [_simplify_search_result(r) for r in results]
        
        result = {
            'results': simplified_results,
            'total': response.get('totalSize', len(results)),
            'start_at': start_at,
            'limit': limit,
            'is_last': start_at + len(results) >= response.get('totalSize', 0)
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
