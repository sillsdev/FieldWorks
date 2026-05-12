#!/usr/bin/env python3
"""Bitbucket project and repository management utilities.

This module provides functions for listing projects and repositories
in Bitbucket Server/Data Center.
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


def bitbucket_list_projects(
    limit: int = 25,
    start: int = 0,
    credentials: Optional[AtlassianCredentials] = None
) -> str:
    """List projects in Bitbucket Server.

    Args:
        limit: Number of projects to return (default: 25, max: 100)
        start: Start index for pagination (default: 0)

    Returns:
        JSON string with list of projects or error information
    """
    try:
        client = get_bitbucket_client(credentials)
        
        params = {
            "limit": min(limit, 100),
            "start": start
        }
        
        data = client.get("/rest/api/1.0/projects", params=params)
        
        projects = []
        for project in data.get("values", []):
            projects.append({
                "key": project.get("key", ""),
                "name": project.get("name", ""),
                "description": project.get("description", ""),
                "public": project.get("public", False),
                "type": project.get("type", ""),
            })
        
        result = {
            "projects": projects,
            "total": len(projects),
            "is_last_page": data.get("isLastPage", True),
            "next_page_start": data.get("nextPageStart")
        }
        
        return format_json_response(result)
        
    except ConfigurationError as e:
        return format_error_response("ConfigurationError", str(e))
    except AuthenticationError as e:
        return format_error_response("AuthenticationError", str(e))
    except NotFoundError as e:
        return format_error_response("NotFoundError", str(e))
    except APIError as e:
        return format_error_response("APIError", str(e))
    except NetworkError as e:
        return format_error_response("NetworkError", str(e))
    except Exception as e:
        return format_error_response("UnexpectedError", str(e))


def bitbucket_list_repositories(
    project_key: Optional[str] = None,
    limit: int = 25,
    start: int = 0
,
    credentials: Optional[AtlassianCredentials] = None) -> str:
    """List repositories in Bitbucket Server.

    Args:
        project_key: Project key to filter repositories (optional)
        limit: Number of repositories to return (default: 25, max: 100)
        start: Start index for pagination (default: 0)

    Returns:
        JSON string with list of repositories or error information
    """
    try:
        client = get_bitbucket_client(credentials)
        
        params = {
            "limit": min(limit, 100),
            "start": start
        }
        
        if project_key:
            endpoint = f"/rest/api/1.0/projects/{project_key}/repos"
        else:
            endpoint = "/rest/api/1.0/repos"
        
        data = client.get(endpoint, params=params)
        
        repositories = []
        for repo in data.get("values", []):
            project = repo.get("project", {})
            repositories.append({
                "slug": repo.get("slug", ""),
                "name": repo.get("name", ""),
                "description": repo.get("description", ""),
                "project_key": project.get("key", ""),
                "project_name": project.get("name", ""),
                "public": repo.get("public", False),
                "state": repo.get("state", ""),
                "forkable": repo.get("forkable", True),
            })
        
        result = {
            "repositories": repositories,
            "total": len(repositories),
            "is_last_page": data.get("isLastPage", True),
            "next_page_start": data.get("nextPageStart")
        }
        
        return format_json_response(result)
        
    except ConfigurationError as e:
        return format_error_response("ConfigurationError", str(e))
    except AuthenticationError as e:
        return format_error_response("AuthenticationError", str(e))
    except NotFoundError as e:
        return format_error_response("NotFoundError", str(e))
    except APIError as e:
        return format_error_response("APIError", str(e))
    except NetworkError as e:
        return format_error_response("NetworkError", str(e))
    except Exception as e:
        return format_error_response("UnexpectedError", str(e))
