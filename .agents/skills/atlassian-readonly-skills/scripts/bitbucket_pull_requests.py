#!/usr/bin/env python3
"""Bitbucket pull request management utilities.

This module provides functions for viewing pull requests
in Bitbucket Server/Data Center.
"""

from typing import Optional, Dict, Any, List
from ._common import (
    get_bitbucket_client,
    format_json_response,
    format_error_response,
    ConfigurationError,
    AuthenticationError,
    ValidationError,
    NotFoundError,
    APIError,
    NetworkError,
)


def _simplify_pull_request(pr_data: Dict[str, Any]) -> Dict[str, Any]:
    """Simplify pull request data to essential fields."""
    from_ref = pr_data.get("fromRef", {})
    to_ref = pr_data.get("toRef", {})
    author = pr_data.get("author", {}).get("user", {})
    
    reviewers = []
    for reviewer in pr_data.get("reviewers", []):
        user = reviewer.get("user", {})
        reviewers.append({
            "name": user.get("name", ""),
            "email": user.get("emailAddress", ""),
            "status": reviewer.get("status", ""),
            "approved": reviewer.get("approved", False),
        })
    
    return {
        "id": pr_data.get("id"),
        "title": pr_data.get("title", ""),
        "description": pr_data.get("description", ""),
        "state": pr_data.get("state", ""),
        "version": pr_data.get("version"),
        "source_branch": from_ref.get("displayId", ""),
        "target_branch": to_ref.get("displayId", ""),
        "source_repo": from_ref.get("repository", {}).get("slug", ""),
        "target_repo": to_ref.get("repository", {}).get("slug", ""),
        "project_key": to_ref.get("repository", {}).get("project", {}).get("key", ""),
        "author": author.get("name", ""),
        "author_email": author.get("emailAddress", ""),
        "created": pr_data.get("createdDate"),
        "updated": pr_data.get("updatedDate"),
        "reviewers": reviewers,
        "open": pr_data.get("open", False),
        "closed": pr_data.get("closed", False),
        "locked": pr_data.get("locked", False),
    }


def bitbucket_get_pull_request(
    project_key: str,
    repository_slug: str,
    pr_id: int
,
    credentials: Optional[AtlassianCredentials] = None) -> str:
    """Get details of a pull request.

    Args:
        project_key: Project key (e.g., 'PROJ')
        repository_slug: Repository slug
        pr_id: Pull request ID

    Returns:
        JSON string with pull request details or error information
    """
    try:
        if not project_key:
            raise ValidationError("project_key is required")
        if not repository_slug:
            raise ValidationError("repository_slug is required")
        if not pr_id:
            raise ValidationError("pr_id is required")
        
        client = get_bitbucket_client(credentials)
        
        endpoint = f"/rest/api/1.0/projects/{project_key}/repos/{repository_slug}/pull-requests/{pr_id}"
        data = client.get(endpoint)
        
        return format_json_response(_simplify_pull_request(data))
        
    except ConfigurationError as e:
        return format_error_response("ConfigurationError", str(e))
    except AuthenticationError as e:
        return format_error_response("AuthenticationError", str(e))
    except ValidationError as e:
        return format_error_response("ValidationError", str(e))
    except NotFoundError as e:
        return format_error_response("NotFoundError", str(e))
    except APIError as e:
        return format_error_response("APIError", str(e))
    except NetworkError as e:
        return format_error_response("NetworkError", str(e))
    except Exception as e:
        return format_error_response("UnexpectedError", str(e))


def bitbucket_get_pr_diff(
    project_key: str,
    repository_slug: str,
    pr_id: int
,
    credentials: Optional[AtlassianCredentials] = None) -> str:
    """Get the diff of a pull request.

    Args:
        project_key: Project key (e.g., 'PROJ')
        repository_slug: Repository slug
        pr_id: Pull request ID

    Returns:
        JSON string with diff information or error information
    """
    try:
        if not project_key:
            raise ValidationError("project_key is required")
        if not repository_slug:
            raise ValidationError("repository_slug is required")
        if not pr_id:
            raise ValidationError("pr_id is required")
        
        client = get_bitbucket_client(credentials)
        
        endpoint = f"/rest/api/1.0/projects/{project_key}/repos/{repository_slug}/pull-requests/{pr_id}/diff"
        data = client.get(endpoint)
        
        return format_json_response(data)
        
    except ConfigurationError as e:
        return format_error_response("ConfigurationError", str(e))
    except AuthenticationError as e:
        return format_error_response("AuthenticationError", str(e))
    except ValidationError as e:
        return format_error_response("ValidationError", str(e))
    except NotFoundError as e:
        return format_error_response("NotFoundError", str(e))
    except APIError as e:
        return format_error_response("APIError", str(e))
    except NetworkError as e:
        return format_error_response("NetworkError", str(e))
    except Exception as e:
        return format_error_response("UnexpectedError", str(e))
