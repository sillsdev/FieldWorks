"""Jira user management tools.

Tools:
    - jira_get_user_profile: Get user profile by identifier
"""

import sys
from pathlib import Path
sys.path.insert(0, str(Path(__file__).parent))

from typing import Any, Dict, Optional

from _common import (
    AtlassianCredentials,
    get_jira_client,
    format_json_response,
    format_error_response,
    ConfigurationError,
    AuthenticationError,
    ValidationError,
    NotFoundError,
    APIError,
    NetworkError,
)


def _simplify_user(user_data: Dict[str, Any]) -> Dict[str, Any]:
    """Simplify user data to essential fields."""
    return {
        'account_id': user_data.get('accountId', ''),
        'display_name': user_data.get('displayName', ''),
        'email': user_data.get('emailAddress', ''),
        'active': user_data.get('active', False),
        'account_type': user_data.get('accountType', ''),
        'timezone': user_data.get('timeZone', ''),
        'locale': user_data.get('locale', '')
    }


def jira_get_user_profile(
    user_identifier: str,
    credentials: Optional[AtlassianCredentials] = None
) -> str:
    """Get Jira user profile by identifier.
    
    Supports both Jira Cloud (accountId) and Jira Data Center/Server (username).
    
    Args:
        user_identifier: User identifier (email, account ID, username, or key)
        credentials: Optional AtlassianCredentials object for Agent environments.
                    If not provided, configuration will be loaded from environment variables.
    
    Returns:
        JSON string with user profile data or error information
    """
    try:
        client = get_jira_client(credentials)
        
        if not user_identifier:
            raise ValidationError('user_identifier is required')
        
        # Try different lookup methods
        user_data = None
        is_cloud = client.config.is_cloud
        
        # Method 1: Try as account ID (Cloud) or username (Data Center)
        try:
            if is_cloud:
                params = {'accountId': user_identifier}
            else:
                # Data Center/Server uses 'username' parameter
                params = {'username': user_identifier}
            user_data = client.get(client.api_path('user'), params=params)
        except (NotFoundError, APIError, ValidationError):
            pass
        
        # Method 2: Try the opposite parameter if Method 1 failed
        # This handles cases where we can't reliably detect Cloud vs Data Center
        if not user_data:
            try:
                if is_cloud:
                    # Try username parameter on Cloud (shouldn't work but worth trying)
                    params = {'username': user_identifier}
                else:
                    # Try accountId parameter on Data Center (for hybrid scenarios)
                    params = {'accountId': user_identifier}
                user_data = client.get(client.api_path('user'), params=params)
            except (NotFoundError, APIError, ValidationError):
                pass
        
        # Method 3: Try user search by query
        if not user_data:
            try:
                params = {'query': user_identifier, 'maxResults': 1}
                response = client.get(client.api_path('user/search'), params=params)
                if response and len(response) > 0:
                    user_data = response[0]
            except (NotFoundError, APIError):
                pass
        
        # Method 4: Try user picker
        if not user_data:
            try:
                params = {'query': user_identifier, 'maxResults': 1}
                response = client.get(client.api_path('user/picker'), params=params)
                users = response.get('users', [])
                if users:
                    # Try to get full user data using the appropriate parameter
                    if is_cloud and users[0].get('accountId'):
                        params = {'accountId': users[0].get('accountId')}
                        user_data = client.get(client.api_path('user'), params=params)
                    elif not is_cloud and users[0].get('name'):
                        params = {'username': users[0].get('name')}
                        user_data = client.get(client.api_path('user'), params=params)
            except (NotFoundError, APIError):
                pass
        
        if not user_data:
            raise NotFoundError(f'User not found: {user_identifier}')
        
        simplified = _simplify_user(user_data)
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
