#!/usr/bin/env python3
"""Bitbucket pull request management utilities.

This module provides functions for creating, viewing, merging,
and managing pull requests in Bitbucket Server/Data Center.
"""

from typing import Optional, Dict, Any, List
from ._common import (
    AtlassianCredentials,
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


def bitbucket_create_pull_request(
    project_key: str,
    repository_slug: str,
    title: str,
    source_branch: str,
    target_branch: str,
    description: Optional[str] = None,
    reviewers: Optional[List[str]] = None,
    credentials: Optional[AtlassianCredentials] = None
) -> str:
    """Create a new pull request.

    Args:
        project_key: Project key (e.g., 'PROJ')
        repository_slug: Repository slug
        title: Pull request title
        source_branch: Source branch name
        target_branch: Target branch name
        description: Pull request description (optional)
        reviewers: List of reviewer usernames (optional)

    Returns:
        JSON string with created pull request details or error information
    """
    try:
        if not project_key:
            raise ValidationError("project_key is required")
        if not repository_slug:
            raise ValidationError("repository_slug is required")
        if not title:
            raise ValidationError("title is required")
        if not source_branch:
            raise ValidationError("source_branch is required")
        if not target_branch:
            raise ValidationError("target_branch is required")
        
        client = get_bitbucket_client(credentials)
        
        payload: Dict[str, Any] = {
            "title": title,
            "description": description or "",
            "fromRef": {
                "id": f"refs/heads/{source_branch}",
                "repository": {
                    "slug": repository_slug,
                    "project": {"key": project_key}
                }
            },
            "toRef": {
                "id": f"refs/heads/{target_branch}",
                "repository": {
                    "slug": repository_slug,
                    "project": {"key": project_key}
                }
            }
        }
        
        if reviewers:
            payload["reviewers"] = [{"user": {"name": r}} for r in reviewers]
        
        endpoint = f"/rest/api/1.0/projects/{project_key}/repos/{repository_slug}/pull-requests"
        data = client.post(endpoint, json=payload)
        
        result = _simplify_pull_request(data)
        result["success"] = True
        return format_json_response(result)
        
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



def bitbucket_merge_pull_request(
    project_key: str,
    repository_slug: str,
    pr_id: int,
    version: int,
    strategy: str = "merge-commit"
,
    credentials: Optional[AtlassianCredentials] = None) -> str:
    """Merge a pull request.

    Args:
        project_key: Project key (e.g., 'PROJ')
        repository_slug: Repository slug
        pr_id: Pull request ID
        version: The version of the PR (from get_pull_request)
        strategy: Merge strategy ('merge-commit', 'squash', 'fast-forward')

    Returns:
        JSON string with merge result or error information
    """
    try:
        if not project_key:
            raise ValidationError("project_key is required")
        if not repository_slug:
            raise ValidationError("repository_slug is required")
        if not pr_id:
            raise ValidationError("pr_id is required")
        if version is None:
            raise ValidationError("version is required")
        
        valid_strategies = ["merge-commit", "squash", "fast-forward"]
        if strategy not in valid_strategies:
            raise ValidationError(f"strategy must be one of: {', '.join(valid_strategies)}")
        
        client = get_bitbucket_client(credentials)
        
        payload = {
            "version": version,
            "strategy": strategy
        }
        
        endpoint = f"/rest/api/1.0/projects/{project_key}/repos/{repository_slug}/pull-requests/{pr_id}/merge"
        data = client.post(endpoint, json=payload)
        
        result = _simplify_pull_request(data)
        result["success"] = True
        result["message"] = "Pull request merged successfully"
        return format_json_response(result)
        
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


def bitbucket_decline_pull_request(
    project_key: str,
    repository_slug: str,
    pr_id: int,
    version: int
,
    credentials: Optional[AtlassianCredentials] = None) -> str:
    """Decline a pull request.

    Args:
        project_key: Project key (e.g., 'PROJ')
        repository_slug: Repository slug
        pr_id: Pull request ID
        version: The version of the PR (from get_pull_request)

    Returns:
        JSON string with decline result or error information
    """
    try:
        if not project_key:
            raise ValidationError("project_key is required")
        if not repository_slug:
            raise ValidationError("repository_slug is required")
        if not pr_id:
            raise ValidationError("pr_id is required")
        if version is None:
            raise ValidationError("version is required")
        
        client = get_bitbucket_client(credentials)
        
        payload = {"version": version}
        
        endpoint = f"/rest/api/1.0/projects/{project_key}/repos/{repository_slug}/pull-requests/{pr_id}/decline"
        data = client.post(endpoint, json=payload)
        
        result = _simplify_pull_request(data)
        result["success"] = True
        result["message"] = "Pull request declined"
        return format_json_response(result)
        
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


def bitbucket_add_pr_comment(
    project_key: str,
    repository_slug: str,
    pr_id: int,
    text: str,
    parent_id: Optional[int] = None
,
    credentials: Optional[AtlassianCredentials] = None) -> str:
    """Add a comment to a pull request.

    Args:
        project_key: Project key (e.g., 'PROJ')
        repository_slug: Repository slug
        pr_id: Pull request ID
        text: Comment text
        parent_id: Parent comment ID for replies (optional)

    Returns:
        JSON string with comment details or error information
    """
    try:
        if not project_key:
            raise ValidationError("project_key is required")
        if not repository_slug:
            raise ValidationError("repository_slug is required")
        if not pr_id:
            raise ValidationError("pr_id is required")
        if not text:
            raise ValidationError("text is required")
        
        client = get_bitbucket_client(credentials)
        
        payload: Dict[str, Any] = {"text": text}
        if parent_id:
            payload["parent"] = {"id": parent_id}
        
        endpoint = f"/rest/api/1.0/projects/{project_key}/repos/{repository_slug}/pull-requests/{pr_id}/comments"
        data = client.post(endpoint, json=payload)
        
        author = data.get("author", {})
        result = {
            "success": True,
            "id": data.get("id"),
            "text": data.get("text", ""),
            "author": author.get("name", ""),
            "author_email": author.get("emailAddress", ""),
            "created": data.get("createdDate"),
            "updated": data.get("updatedDate"),
        }
        
        return format_json_response(result)
        
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
