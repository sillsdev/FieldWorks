"""Confluence page management tools.

Tools:
    - confluence_get_page: Get a page by ID or title
"""

import sys
from pathlib import Path
sys.path.insert(0, str(Path(__file__).parent))

from typing import Any, Dict, Optional

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


def _simplify_page(page_data: Dict[str, Any]) -> Dict[str, Any]:
    """Simplify page data to essential fields."""
    body = page_data.get('body', {})
    storage = body.get('storage', {}) or body.get('view', {})
    
    return {
        'id': page_data.get('id', ''),
        'title': page_data.get('title', ''),
        'space_key': page_data.get('space', {}).get('key', ''),
        'version': page_data.get('version', {}).get('number', 1),
        'content': storage.get('value', ''),
        'created': page_data.get('history', {}).get('createdDate', ''),
        'updated': page_data.get('version', {}).get('when', ''),
        'url': page_data.get('_links', {}).get('webui', '')
    }


def confluence_get_page(
    page_id: Optional[str] = None,
    title: Optional[str] = None,
    space_key: Optional[str] = None
,
    credentials: Optional[AtlassianCredentials] = None) -> str:
    """Get a Confluence page by ID or by title and space.
    
    Args:
        page_id: Page ID (optional if title and space_key provided)
        title: Page title (optional if page_id provided)
        space_key: Space key (required if using title)
    
    Returns:
        JSON string with page data or error information
    """
    try:
        client = get_confluence_client(credentials)
        
        if not page_id and not title:
            raise ValidationError('Either page_id or title is required')
        if title and not space_key:
            raise ValidationError('space_key is required when using title')
        
        if page_id:
            params = {'expand': 'body.storage,version,space,history'}
            page_data = client.get(f'/rest/api/content/{page_id}', params=params)
        else:
            params = {
                'title': title,
                'spaceKey': space_key,
                'expand': 'body.storage,version,space,history'
            }
            response = client.get('/rest/api/content', params=params)
            results = response.get('results', [])
            if not results:
                raise NotFoundError(f'Page not found: {title} in space {space_key}')
            page_data = results[0]
        
        simplified = _simplify_page(page_data)
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
