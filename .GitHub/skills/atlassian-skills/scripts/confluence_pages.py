"""Confluence page management tools.

Tools:
    - confluence_get_page: Get a page by ID or title
    - confluence_create_page: Create a new page
    - confluence_update_page: Update an existing page
    - confluence_delete_page: Delete a page
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


def confluence_create_page(
    space_key: str,
    title: str,
    content: str,
    parent_id: Optional[str] = None
,
    credentials: Optional[AtlassianCredentials] = None) -> str:
    """Create a new Confluence page.
    
    Args:
        space_key: Space key where the page will be created
        title: Page title
        content: Page content (HTML or storage format)
        parent_id: Parent page ID (optional)
    
    Returns:
        JSON string with created page data or error information
    """
    try:
        client = get_confluence_client(credentials)
        
        if not space_key:
            raise ValidationError('space_key is required')
        if not title:
            raise ValidationError('title is required')
        if not content:
            raise ValidationError('content is required')
        
        payload: Dict[str, Any] = {
            'type': 'page',
            'title': title,
            'space': {'key': space_key},
            'body': {
                'storage': {
                    'value': content,
                    'representation': 'storage'
                }
            }
        }
        
        if parent_id:
            payload['ancestors'] = [{'id': parent_id}]
        
        response = client.post('/rest/api/content', json=payload)
        
        # Get full page data
        page_id = response.get('id')
        if page_id:
            return confluence_get_page(page_id=page_id)
        
        return format_json_response(_simplify_page(response))
        
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


def confluence_update_page(page_id: str, title: str, content: str,
    credentials: Optional[AtlassianCredentials] = None) -> str:
    """Update an existing Confluence page.
    
    Args:
        page_id: Page ID to update
        title: New page title
        content: New page content (HTML or storage format)
    
    Returns:
        JSON string with updated page data or error information
    """
    try:
        client = get_confluence_client(credentials)
        
        if not page_id:
            raise ValidationError('page_id is required')
        if not title:
            raise ValidationError('title is required')
        if not content:
            raise ValidationError('content is required')
        
        # Get current version
        current = client.get(f'/rest/api/content/{page_id}', params={'expand': 'version'})
        current_version = current.get('version', {}).get('number', 0)
        
        payload: Dict[str, Any] = {
            'type': 'page',
            'title': title,
            'body': {
                'storage': {
                    'value': content,
                    'representation': 'storage'
                }
            },
            'version': {'number': current_version + 1}
        }
        
        response = client.put(f'/rest/api/content/{page_id}', json=payload)
        
        return confluence_get_page(page_id=page_id)
        
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


def confluence_delete_page(page_id: str,
    credentials: Optional[AtlassianCredentials] = None) -> str:
    """Delete a Confluence page.
    
    Args:
        page_id: Page ID to delete
    
    Returns:
        JSON string with success message or error information
    """
    try:
        client = get_confluence_client(credentials)
        
        if not page_id:
            raise ValidationError('page_id is required')
        
        client.delete(f'/rest/api/content/{page_id}')
        
        return format_json_response({
            'success': True,
            'message': f'Page {page_id} deleted successfully'
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
