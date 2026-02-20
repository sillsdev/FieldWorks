#!/usr/bin/env python3
"""Bitbucket commit utilities.

This module provides functions for retrieving commit information
from Bitbucket Server/Data Center repositories.
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


def bitbucket_get_commits(
    project_key: str,
    repository_slug: str,
    branch: str = "master",
    limit: int = 10,
    start: int = 0,
    credentials: Optional[AtlassianCredentials] = None
) -> str:
    """Get commits from a repository.

    Args:
        project_key: Project key (e.g., 'IN')
        repository_slug: Repository slug (e.g., 'insights-pipeline')
        branch: Branch name or commit reference (default: 'master')
        limit: Maximum number of commits to retrieve (default: 10, max: 100)
        start: Start index for pagination (default: 0)

    Returns:
        JSON string with commit history or error information
    """
    try:
        if not project_key:
            raise ValidationError("project_key is required")
        if not repository_slug:
            raise ValidationError("repository_slug is required")

        client = get_bitbucket_client(credentials)

        # Use the commits API endpoint
        endpoint = f"/rest/api/1.0/projects/{project_key}/repos/{repository_slug}/commits"
        params = {
            "until": branch,
            "limit": min(limit, 100),
            "start": start
        }

        data = client.get(endpoint, params=params)

        commits = []
        for commit in data.get("values", []):
            author = commit.get("author", {})
            committer = commit.get("committer", {})

            commits.append({
                "id": commit.get("id", ""),
                "display_id": commit.get("displayId", ""),
                "message": commit.get("message", ""),
                "author_name": author.get("name", ""),
                "author_email": author.get("emailAddress", ""),
                "committer_name": committer.get("name", ""),
                "committer_email": committer.get("emailAddress", ""),
                "timestamp": commit.get("committerTimestamp", 0),
                "parents": [p.get("id", "") for p in commit.get("parents", [])],
            })

        result = {
            "project_key": project_key,
            "repository": repository_slug,
            "branch": branch,
            "total": data.get("size", 0),
            "is_last_page": data.get("isLastPage", True),
            "commits": commits,
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


def bitbucket_get_commit(
    project_key: str,
    repository_slug: str,
    commit_id: str
,
    credentials: Optional[AtlassianCredentials] = None) -> str:
    """Get details of a specific commit.

    Args:
        project_key: Project key (e.g., 'IN')
        repository_slug: Repository slug (e.g., 'insights-pipeline')
        commit_id: Commit ID or hash

    Returns:
        JSON string with commit details or error information
    """
    try:
        if not project_key:
            raise ValidationError("project_key is required")
        if not repository_slug:
            raise ValidationError("repository_slug is required")
        if not commit_id:
            raise ValidationError("commit_id is required")

        client = get_bitbucket_client(credentials)

        # Use the commit details API endpoint
        endpoint = f"/rest/api/1.0/projects/{project_key}/repos/{repository_slug}/commits/{commit_id}"

        commit = client.get(endpoint)

        author = commit.get("author", {})
        committer = commit.get("committer", {})

        result = {
            "id": commit.get("id", ""),
            "display_id": commit.get("displayId", ""),
            "message": commit.get("message", ""),
            "author_name": author.get("name", ""),
            "author_email": author.get("emailAddress", ""),
            "committer_name": committer.get("name", ""),
            "committer_email": committer.get("emailAddress", ""),
            "timestamp": commit.get("committerTimestamp", 0),
            "parents": [p.get("id", "") for p in commit.get("parents", [])],
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
