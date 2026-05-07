"""Confluence label management tools.

Tools:
    - confluence_get_labels: Get labels for a page
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


def confluence_get_labels(
    page_id: str,
    credentials: Optional[AtlassianCredentials] = None
) -> str:
    """Get all labels for a Confluence page.
    
    Args:
        page_id: Page ID to get labels for
    
    Returns:
        JSON string with list of labels or error information
    """
    try:
        client = get_confluence_client(credentials)
        
        if not page_id:
            raise ValidationError('page_id is required')
        
        response = client.get(f'/rest/api/content/{page_id}/label')
        
        labels = response.get('results', [])
        simplified_labels = [
            {'name': label.get('name', ''), 'prefix': label.get('prefix', '')}
            for label in labels
        ]
        
        result = {
            'labels': simplified_labels,
            'count': len(simplified_labels),
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
