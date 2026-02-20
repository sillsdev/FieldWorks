"""Jira issue linking tools.

Tools:
    - jira_get_link_types: Get available link types
"""

import sys
from pathlib import Path
sys.path.insert(0, str(Path(__file__).parent))

from typing import Any, Dict

from _common import (
    get_jira_client,
    format_json_response,
    format_error_response,
    ConfigurationError,
    AuthenticationError,
    APIError,
    NetworkError,
)


def _simplify_link_type(link_type_data: Dict[str, Any]) -> Dict[str, Any]:
    """Simplify link type data to essential fields."""
    return {
        'id': link_type_data.get('id', ''),
        'name': link_type_data.get('name', ''),
        'inward': link_type_data.get('inward', ''),
        'outward': link_type_data.get('outward', ''),
    }


def jira_get_link_types(
    credentials: Optional[AtlassianCredentials] = None) -> str:
    """Get all available issue link types.
    
    Returns:
        JSON string with list of link types or error information
    """
    try:
        client = get_jira_client(credentials)
        
        response = client.get(client.api_path('issueLinkType'))
        
        link_types = response.get('issueLinkTypes', [])
        simplified_link_types = [_simplify_link_type(lt) for lt in link_types]
        
        result = {
            'link_types': simplified_link_types,
            'count': len(simplified_link_types)
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
