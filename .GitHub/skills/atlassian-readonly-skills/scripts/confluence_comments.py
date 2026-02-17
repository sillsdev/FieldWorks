"""Confluence comment management tools.

Tools:
    - confluence_get_comments: Get comments for a page
"""

import sys
from pathlib import Path
sys.path.insert(0, str(Path(__file__).parent))

from typing import Any, Dict

from _common import (
    get_confluence_client,
    format_json_response,
    format_error_response,
    ConfigurationError,
    AuthenticationError,
    ValidationError,
    NotFoundError,
    APIError,
    NetworkError,
)


def _simplify_comment(comment_data: Dict[str, Any]) -> Dict[str, Any]:
    """Simplify comment data to essential fields."""
    body = comment_data.get('body', {})
    storage = body.get('storage', {}) or body.get('view', {})
    
    return {
        'id': comment_data.get('id', ''),
        'content': storage.get('value', ''),
        'created': comment_data.get('history', {}).get('createdDate', ''),
        'author': comment_data.get('history', {}).get('createdBy', {}).get(
            'displayName', ''
        )
    }


def confluence_get_comments(
    page_id: str,
    credentials: Optional[AtlassianCredentials] = None
) -> str:
    """Get all comments for a Confluence page.
    
    Args:
        page_id: Page ID to get comments for
    
    Returns:
        JSON string with list of comments or error information
    """
    try:
        client = get_confluence_client(credentials)
        
        if not page_id:
            raise ValidationError('page_id is required')
        
        params = {
            'expand': 'body.storage,history',
            'depth': 'all'
        }
        response = client.get(
            f'/rest/api/content/{page_id}/child/comment', params=params
        )
        
        comments = response.get('results', [])
        simplified_comments = [_simplify_comment(c) for c in comments]
        
        result = {
            'comments': simplified_comments,
            'count': len(simplified_comments),
            'page_id': page_id
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
