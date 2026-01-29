"""Common utilities for Atlassian Skills scripts.

This module provides shared functionality used by all Jira and Confluence scripts:
- Configuration management
- HTTP client for API requests
- Error handling
- Response formatting
"""

import base64
import json
import os
import re
from dataclasses import dataclass
from pathlib import Path
from typing import Any, Dict, List, Optional

import requests
from dotenv import load_dotenv

# =============================================================================
# Auto-load .env file (with override=False to respect existing env vars)
# =============================================================================
# Find .env file in parent directory (atlassian-skills/.env)
_env_path = Path(__file__).parent.parent / '.env'
if _env_path.exists():
    # override=False ensures existing environment variables are NOT overwritten
    # Priority: explicit config > environment variables > .env file
    load_dotenv(dotenv_path=_env_path, override=False)


# =============================================================================
# Error Classes
# =============================================================================

class ConfigurationError(Exception):
    """Raised when configuration is missing or invalid."""
    pass


class AuthenticationError(Exception):
    """Raised when authentication fails."""
    pass


class ValidationError(Exception):
    """Raised when input validation fails."""
    pass


class NotFoundError(Exception):
    """Raised when a resource is not found."""
    pass


class APIError(Exception):
    """Raised when the Atlassian API returns an error."""
    pass


class NetworkError(Exception):
    """Raised when network connectivity issues occur."""
    pass


# =============================================================================
# Response Formatting
# =============================================================================

def format_error_response(error_type: str, message: str, details: str = "") -> str:
    """Format an error as a JSON response.
    
    Args:
        error_type: Type of error (e.g., 'AuthenticationError', 'ValidationError')
        message: Main error message
        details: Additional error details (optional)
    
    Returns:
        JSON string with error information
    """
    error_data: Dict[str, Any] = {
        "success": False,
        "error": message,
        "error_type": error_type
    }
    if details:
        error_data["details"] = details
    return json.dumps(error_data, ensure_ascii=False, indent=2)


def format_json_response(data: Any) -> str:
    """Format data as a JSON string with UTF-8 encoding.
    
    Args:
        data: Data to serialize (dict, list, or other JSON-serializable type)
    
    Returns:
        JSON formatted string with proper UTF-8 encoding
    """
    return json.dumps(data, ensure_ascii=False, indent=2)


# =============================================================================
# Credentials Data Class
# =============================================================================

@dataclass
class AtlassianCredentials:
    """Unified credentials configuration for all Atlassian services.
    
    This class wraps authentication credentials for Jira, Confluence, and Bitbucket.
    When deployed in an Agent environment without environment variables, pass this
    object to skill functions to provide credentials programmatically.
    
    For each service, provide either:
    - PAT Token (for Data Center/Server), or
    - Username + API Token (for Cloud)
    
    If a service's credentials are not provided, that service will be unavailable.
    """
    
    # Jira configuration
    jira_url: Optional[str] = None
    jira_username: Optional[str] = None
    jira_api_token: Optional[str] = None
    jira_pat_token: Optional[str] = None
    jira_api_version: Optional[str] = None
    jira_ssl_verify: bool = False
    
    # Confluence configuration
    confluence_url: Optional[str] = None
    confluence_username: Optional[str] = None
    confluence_api_token: Optional[str] = None
    confluence_pat_token: Optional[str] = None
    confluence_api_version: Optional[str] = None
    confluence_ssl_verify: bool = False
    
    # Bitbucket configuration
    bitbucket_url: Optional[str] = None
    bitbucket_username: Optional[str] = None
    bitbucket_api_token: Optional[str] = None
    bitbucket_pat_token: Optional[str] = None
    bitbucket_api_version: Optional[str] = None
    bitbucket_ssl_verify: bool = False
    
    def is_jira_available(self) -> bool:
        """Check if Jira credentials are complete and valid.
        
        Returns:
            True if Jira can be used, False otherwise
        """
        if not self.jira_url:
            return False
        has_pat = bool(self.jira_pat_token)
        has_basic = bool(self.jira_username and self.jira_api_token)
        return has_pat or has_basic
    
    def is_confluence_available(self) -> bool:
        """Check if Confluence credentials are complete and valid.
        
        Returns:
            True if Confluence can be used, False otherwise
        """
        if not self.confluence_url:
            return False
        has_pat = bool(self.confluence_pat_token)
        has_basic = bool(self.confluence_username and self.confluence_api_token)
        return has_pat or has_basic
    
    def is_bitbucket_available(self) -> bool:
        """Check if Bitbucket credentials are complete and valid.
        
        Returns:
            True if Bitbucket can be used, False otherwise
        """
        if not self.bitbucket_url:
            return False
        has_pat = bool(self.bitbucket_pat_token)
        has_basic = bool(self.bitbucket_username and self.bitbucket_api_token)
        return has_pat or has_basic
    
    def get_available_services(self) -> List[str]:
        """Get list of available services based on provided credentials.
        
        Returns:
            List of service names that have complete credentials
            Example: ["jira", "confluence"]
        """
        services = []
        if self.is_jira_available():
            services.append("jira")
        if self.is_confluence_available():
            services.append("confluence")
        if self.is_bitbucket_available():
            services.append("bitbucket")
        return services
    
    def get_unavailable_services(self) -> Dict[str, str]:
        """Get dictionary of unavailable services with reasons.
        
        Returns:
            Dictionary mapping service name to reason for unavailability
            Example: {"bitbucket": "Missing bitbucket_url"}
        """
        unavailable = {}
        
        if not self.is_jira_available():
            if not self.jira_url:
                unavailable["jira"] = "Missing jira_url"
            else:
                unavailable["jira"] = "Missing authentication credentials (provide jira_pat_token or jira_username + jira_api_token)"
        
        if not self.is_confluence_available():
            if not self.confluence_url:
                unavailable["confluence"] = "Missing confluence_url"
            else:
                unavailable["confluence"] = "Missing authentication credentials (provide confluence_pat_token or confluence_username + confluence_api_token)"
        
        if not self.is_bitbucket_available():
            if not self.bitbucket_url:
                unavailable["bitbucket"] = "Missing bitbucket_url"
            else:
                unavailable["bitbucket"] = "Missing authentication credentials (provide bitbucket_pat_token or bitbucket_username + bitbucket_api_token)"
        
        return unavailable


# =============================================================================
# Configuration
# =============================================================================

class AtlassianConfig:
    """Configuration for Atlassian API authentication."""

    def __init__(
        self,
        url: str,
        username: Optional[str] = None,
        api_token: Optional[str] = None,
        pat_token: Optional[str] = None,
        api_version: Optional[str] = None,
        ssl_verify: bool = False
    ):
        """Initialize configuration.

        Args:
            url: Base URL for the Atlassian instance
            username: Username for Cloud authentication (optional)
            api_token: API token for Cloud authentication (optional)
            pat_token: Personal Access Token for Data Center authentication (optional)
            api_version: API version to use ('2' or '3'). If not specified, will auto-detect (optional)
            ssl_verify: Whether to verify SSL certificates (default: False)
        """
        self.url = url.rstrip('/') if url else ""
        self.username = username
        self.api_token = api_token
        self.pat_token = pat_token
        self.api_version = api_version
        self.ssl_verify = ssl_verify

        if pat_token:
            self.auth_type = "pat"
        elif username and api_token:
            self.auth_type = "basic"
        else:
            self.auth_type = "unknown"
    
    @classmethod
    def from_env(cls, prefix: str) -> "AtlassianConfig":
        """Load configuration from environment variables.

        Args:
            prefix: Prefix for environment variables (e.g., 'JIRA' or 'CONFLUENCE')

        Returns:
            AtlassianConfig instance with values from environment

        Raises:
            ConfigurationError: If required environment variables are missing
        """
        url = os.getenv(f"{prefix}_URL")
        username = os.getenv(f"{prefix}_USERNAME")
        api_token = os.getenv(f"{prefix}_API_TOKEN")
        pat_token = os.getenv(f"{prefix}_PAT_TOKEN")
        api_version = os.getenv(f"{prefix}_API_VERSION")
        ssl_verify_str = os.getenv(f"{prefix}_SSL_VERIFY", "false").lower()
        ssl_verify = ssl_verify_str in ("true", "1", "yes")

        config = cls(
            url=url or "",
            username=username,
            api_token=api_token,
            pat_token=pat_token,
            api_version=api_version,
            ssl_verify=ssl_verify
        )
        config._validate(prefix)
        return config
    
    @classmethod
    def from_credentials(cls, credentials: AtlassianCredentials, service: str) -> "AtlassianConfig":
        """Create configuration from AtlassianCredentials object.
        
        Args:
            credentials: AtlassianCredentials instance with service credentials
            service: Service name ('jira', 'confluence', or 'bitbucket')
        
        Returns:
            AtlassianConfig instance
        
        Raises:
            ConfigurationError: If credentials for the service are incomplete
        """
        service = service.lower()
        
        if service == "jira":
            if not credentials.is_jira_available():
                raise ConfigurationError(
                    "Jira credentials not provided or incomplete. "
                    "Please provide jira_url and either jira_pat_token or (jira_username + jira_api_token)."
                )
            config = cls(
                url=credentials.jira_url or "",
                username=credentials.jira_username,
                api_token=credentials.jira_api_token,
                pat_token=credentials.jira_pat_token,
                api_version=credentials.jira_api_version,
                ssl_verify=credentials.jira_ssl_verify
            )
        elif service == "confluence":
            if not credentials.is_confluence_available():
                raise ConfigurationError(
                    "Confluence credentials not provided or incomplete. "
                    "Please provide confluence_url and either confluence_pat_token or (confluence_username + confluence_api_token)."
                )
            config = cls(
                url=credentials.confluence_url or "",
                username=credentials.confluence_username,
                api_token=credentials.confluence_api_token,
                pat_token=credentials.confluence_pat_token,
                api_version=credentials.confluence_api_version,
                ssl_verify=credentials.confluence_ssl_verify
            )
        elif service == "bitbucket":
            if not credentials.is_bitbucket_available():
                raise ConfigurationError(
                    "Bitbucket credentials not provided or incomplete. "
                    "Please provide bitbucket_url and either bitbucket_pat_token or (bitbucket_username + bitbucket_api_token)."
                )
            config = cls(
                url=credentials.bitbucket_url or "",
                username=credentials.bitbucket_username,
                api_token=credentials.bitbucket_api_token,
                pat_token=credentials.bitbucket_pat_token,
                api_version=credentials.bitbucket_api_version,
                ssl_verify=credentials.bitbucket_ssl_verify
            )
        else:
            raise ConfigurationError(f"Unknown service: {service}. Must be 'jira', 'confluence', or 'bitbucket'.")
        
        return config
    
    def _validate(self, prefix: Optional[str] = None) -> None:
        """Validate configuration."""
        if not self.url:
            raise ConfigurationError(
                f"Missing required configuration: {prefix}_URL. "
                f"Please set this environment variable."
            )
        
        has_pat = bool(self.pat_token)
        has_basic = bool(self.username and self.api_token)
        
        if not has_pat and not has_basic:
            raise ConfigurationError(
                f"Missing authentication credentials for {prefix}. "
                f"Please provide either:\n"
                f"  - {prefix}_PAT_TOKEN (for Data Center/Server), or\n"
                f"  - {prefix}_USERNAME and {prefix}_API_TOKEN (for Cloud)"
            )
    
    def get_auth_header(self) -> dict:
        """Get the appropriate Authorization header."""
        if self.auth_type == "pat":
            return {"Authorization": f"Bearer {self.pat_token}"}
        elif self.auth_type == "basic":
            credentials = f"{self.username}:{self.api_token}"
            encoded = base64.b64encode(credentials.encode()).decode()
            return {"Authorization": f"Basic {encoded}"}
        else:
            raise ConfigurationError("Cannot generate auth header: unknown auth type.")
    
    @property
    def is_cloud(self) -> bool:
        """Check if this is a Cloud instance."""
        return "atlassian.net" in self.url.lower() if self.url else False

    def detect_api_version(self) -> str:
        """Detect the appropriate API version to use.

        Returns:
            '2' or '3' based on detection logic

        Detection logic:
        1. If api_version is explicitly set, use it
        2. If URL contains 'atlassian.net', use v3 (Cloud)
        3. Otherwise, use v2 (Data Center/Server)
        """
        if self.api_version:
            return self.api_version

        # Cloud instances use API v3
        if self.is_cloud:
            return "3"

        # Data Center/Server instances typically use API v2
        return "2"


# =============================================================================
# HTTP Client
# =============================================================================

class AtlassianClient:
    """HTTP client for Atlassian API requests."""

    def __init__(self, config: AtlassianConfig):
        """Initialize the client with configuration.

        Args:
            config: AtlassianConfig instance with URL and credentials
        """
        self.config = config
        self.api_version = config.detect_api_version()
        self.ssl_verify = config.ssl_verify
        self.session = requests.Session()

        if config.auth_type == "pat":
            self.session.headers.update(config.get_auth_header())
        elif config.auth_type == "basic":
            self.session.auth = (config.username, config.api_token)

        self.session.headers.update({
            "Accept": "application/json",
            "Content-Type": "application/json"
        })

    def api_path(self, endpoint: str) -> str:
        """Build API path with the correct version.

        Args:
            endpoint: API endpoint (e.g., '/issue/PROJ-123')

        Returns:
            Full API path (e.g., '/rest/api/2/issue/PROJ-123')
        """
        # Remove leading slash if present
        endpoint = endpoint.lstrip('/')
        return f"/rest/api/{self.api_version}/{endpoint}"
    
    def get(self, path: str, params: Optional[Dict[str, Any]] = None) -> Any:
        """Perform a GET request."""
        url = f"{self.config.url}{path}"
        try:
            response = self.session.get(url, params=params, timeout=30, verify=self.ssl_verify)
            self._handle_error(response)
            return response.json() if response.content else {}
        except requests.exceptions.Timeout:
            raise NetworkError("Request timed out")
        except requests.exceptions.ConnectionError as e:
            raise NetworkError(f"Connection failed: {str(e)}")
        except requests.exceptions.RequestException as e:
            raise NetworkError(f"Network error: {str(e)}")
    
    def post(self, path: str, json: Optional[Dict[str, Any]] = None) -> Any:
        """Perform a POST request."""
        url = f"{self.config.url}{path}"
        try:
            response = self.session.post(url, json=json, timeout=30, verify=self.ssl_verify)
            self._handle_error(response)
            return response.json() if response.content else {}
        except requests.exceptions.Timeout:
            raise NetworkError("Request timed out")
        except requests.exceptions.ConnectionError as e:
            raise NetworkError(f"Connection failed: {str(e)}")
        except requests.exceptions.RequestException as e:
            raise NetworkError(f"Network error: {str(e)}")
    
    def put(self, path: str, json: Optional[Dict[str, Any]] = None) -> Any:
        """Perform a PUT request."""
        url = f"{self.config.url}{path}"
        try:
            response = self.session.put(url, json=json, timeout=30, verify=self.ssl_verify)
            self._handle_error(response)
            return response.json() if response.content else {}
        except requests.exceptions.Timeout:
            raise NetworkError("Request timed out")
        except requests.exceptions.ConnectionError as e:
            raise NetworkError(f"Connection failed: {str(e)}")
        except requests.exceptions.RequestException as e:
            raise NetworkError(f"Network error: {str(e)}")
    
    def delete(self, path: str) -> bool:
        """Perform a DELETE request."""
        url = f"{self.config.url}{path}"
        try:
            response = self.session.delete(url, timeout=30, verify=self.ssl_verify)
            self._handle_error(response)
            return True
        except requests.exceptions.Timeout:
            raise NetworkError("Request timed out")
        except requests.exceptions.ConnectionError as e:
            raise NetworkError(f"Connection failed: {str(e)}")
        except requests.exceptions.RequestException as e:
            raise NetworkError(f"Network error: {str(e)}")
    
    def _handle_error(self, response: requests.Response) -> None:
        """Handle HTTP error responses."""
        if response.status_code < 400:
            return
        
        error_message = ""
        try:
            error_data = response.json()
            if isinstance(error_data, dict):
                error_message = (
                    error_data.get("errorMessages", [""])[0] if "errorMessages" in error_data
                    else error_data.get("message", "")
                    or error_data.get("error", "")
                )
        except Exception:
            error_message = response.text or response.reason
        
        if response.status_code == 401:
            raise AuthenticationError(f"Authentication failed: {error_message}")
        elif response.status_code == 400:
            raise ValidationError(f"Invalid request: {error_message}")
        elif response.status_code == 404:
            raise NotFoundError(f"Resource not found: {error_message}")
        elif response.status_code == 403:
            raise AuthenticationError(f"Access forbidden: {error_message}")
        else:
            raise APIError(f"API error ({response.status_code}): {error_message}")


# =============================================================================
# Helper Functions
# =============================================================================

def get_jira_client(credentials: Optional[AtlassianCredentials] = None) -> AtlassianClient:
    """Get configured Jira client.
    
    Args:
        credentials: Optional AtlassianCredentials object. If not provided,
                    configuration will be loaded from environment variables.
    
    Returns:
        Configured AtlassianClient instance
        
    Raises:
        ConfigurationError: If configuration is missing or invalid
    """
    if credentials:
        config = AtlassianConfig.from_credentials(credentials, 'jira')
    else:
        config = AtlassianConfig.from_env('JIRA')
    return AtlassianClient(config)


def get_confluence_client(credentials: Optional[AtlassianCredentials] = None) -> AtlassianClient:
    """Get configured Confluence client.
    
    Args:
        credentials: Optional AtlassianCredentials object. If not provided,
                    configuration will be loaded from environment variables.
    
    Returns:
        Configured AtlassianClient instance
        
    Raises:
        ConfigurationError: If configuration is missing or invalid
    """
    if credentials:
        config = AtlassianConfig.from_credentials(credentials, 'confluence')
    else:
        config = AtlassianConfig.from_env('CONFLUENCE')
    return AtlassianClient(config)


def get_bitbucket_client(credentials: Optional[AtlassianCredentials] = None) -> AtlassianClient:
    """Get configured Bitbucket client.
    
    Args:
        credentials: Optional AtlassianCredentials object. If not provided,
                    configuration will be loaded from environment variables.
    
    Returns:
        Configured AtlassianClient instance
        
    Raises:
        ConfigurationError: If configuration is missing or invalid
    """
    if credentials:
        config = AtlassianConfig.from_credentials(credentials, 'bitbucket')
    else:
        config = AtlassianConfig.from_env('BITBUCKET')
    return AtlassianClient(config)


def check_available_skills(credentials: AtlassianCredentials) -> Dict[str, Any]:
    """Check which Atlassian skills are available based on provided credentials.
    
    This function helps Agents determine which skills can be used before attempting
    to call them. Services without complete credentials will be listed as unavailable.
    
    Args:
        credentials: AtlassianCredentials object with service credentials
    
    Returns:
        Dictionary with availability information:
        {
            "available_services": ["jira", "confluence"],
            "unavailable_services": {
                "bitbucket": "Missing bitbucket_url"
            }
        }
    
    Example:
        >>> creds = AtlassianCredentials(
        ...     jira_url="https://company.atlassian.net",
        ...     jira_username="user@company.com",
        ...     jira_api_token="token123"
        ... )
        >>> result = check_available_skills(creds)
        >>> print(result["available_services"])
        ["jira"]
    """
    return {
        "available_services": credentials.get_available_services(),
        "unavailable_services": credentials.get_unavailable_services()
    }


def simplify_issue(issue_data: Dict[str, Any]) -> Dict[str, Any]:
    """Simplify issue data to essential fields.
    
    Args:
        issue_data: Raw issue data from Jira API
    
    Returns:
        Simplified issue dictionary with essential fields
    """
    fields = issue_data.get('fields', {})
    
    assignee = fields.get('assignee')
    assignee_email = assignee.get('emailAddress', '') if assignee else None
    
    reporter = fields.get('reporter')
    reporter_email = reporter.get('emailAddress', '') if reporter else None
    
    status = fields.get('status', {})
    issue_type = fields.get('issuetype', {})
    priority = fields.get('priority', {})
    
    simplified = {
        'key': issue_data.get('key', ''),
        'id': issue_data.get('id', ''),
        'summary': fields.get('summary', ''),
        'description': fields.get('description', ''),
        'status': status.get('name', '') if status else '',
        'issue_type': issue_type.get('name', '') if issue_type else '',
        'priority': priority.get('name', '') if priority else '',
        'assignee': assignee_email,
        'reporter': reporter_email,
        'created': fields.get('created', ''),
        'updated': fields.get('updated', ''),
        'labels': fields.get('labels', []),
        'components': [c.get('name', '') for c in fields.get('components', [])],
    }
    
    custom_fields = {}
    for key, value in fields.items():
        if key.startswith('customfield_'):
            custom_fields[key] = value
    if custom_fields:
        simplified['custom_fields'] = custom_fields
    
    return simplified


def parse_time_spent(time_spent: str) -> int:
    """Parse time spent string into seconds.
    
    Args:
        time_spent: Time spent string (e.g., '1h 30m', '2d', '1w')
    
    Returns:
        Time spent in seconds
        
    Raises:
        ValidationError: If time format is invalid
    """
    if not time_spent or not time_spent.strip():
        raise ValidationError('time_spent is required and cannot be empty')
    
    time_spent = time_spent.strip()
    
    if time_spent.endswith('s'):
        try:
            return int(time_spent[:-1])
        except ValueError:
            raise ValidationError(f'Invalid seconds format: {time_spent}')
    
    total_seconds = 0
    time_units = {
        'w': 7 * 24 * 60 * 60,
        'd': 24 * 60 * 60,
        'h': 60 * 60,
        'm': 60,
    }
    
    pattern = r'(\d+)([wdhm])'
    matches = re.findall(pattern, time_spent.lower())
    
    for value, unit in matches:
        seconds = int(value) * time_units[unit]
        total_seconds += seconds
    
    if total_seconds > 0:
        return total_seconds
    
    try:
        raw_value = int(float(time_spent))
        if raw_value > 0:
            return raw_value
    except ValueError:
        pass
    
    raise ValidationError(
        f'Invalid time format: {time_spent}. '
        'Use formats like "1h 30m", "2d", "1w", "45m", or seconds as a number.'
    )
