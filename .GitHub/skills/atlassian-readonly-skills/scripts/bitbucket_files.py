#!/usr/bin/env python3
"""Bitbucket file and search utilities.

This module provides functions for retrieving file content
and searching code in Bitbucket Server/Data Center.
"""

from typing import Optional, Dict, Any
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


def bitbucket_get_file_content(
    project_key: str,
    repository_slug: str,
    file_path: str,
    branch: str = "master",
    credentials: Optional[AtlassianCredentials] = None
) -> str:
    """Get the content of a file from a repository.

    Args:
        project_key: Project key (e.g., 'PROJ')
        repository_slug: Repository slug
        file_path: Path to the file in the repository
        branch: Branch or commit reference (default: 'master')

    Returns:
        JSON string with file content or error information
    """
    try:
        if not project_key:
            raise ValidationError("project_key is required")
        if not repository_slug:
            raise ValidationError("repository_slug is required")
        if not file_path:
            raise ValidationError("file_path is required")
        
        client = get_bitbucket_client(credentials)
        
        # Use the browse API to get file content
        endpoint = f"/rest/api/1.0/projects/{project_key}/repos/{repository_slug}/browse/{file_path}"
        params = {"at": branch}
        
        data = client.get(endpoint, params=params)
        
        # Extract content from lines
        lines = data.get("lines", [])
        content = "\n".join([line.get("text", "") for line in lines])
        
        result = {
            "path": file_path,
            "branch": branch,
            "content": content,
            "size": data.get("size"),
            "is_last_page": data.get("isLastPage", True),
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


def bitbucket_search(
    query: str,
    project_key: Optional[str] = None,
    repository_slug: Optional[str] = None,
    search_type: str = "code",
    limit: int = 25,
    start: int = 0
,
    credentials: Optional[AtlassianCredentials] = None) -> str:
    """Search for code or files in Bitbucket Server.

    Args:
        query: Search query string
        project_key: Limit search to a specific project (optional)
        repository_slug: Limit search to a specific repository (optional)
        search_type: Type of search - 'code' or 'file' (default: 'code')
        limit: Maximum number of results (default: 25, max: 100)
        start: Start index for pagination (default: 0)

    Returns:
        JSON string with search results or error information
    """
    try:
        if not query:
            raise ValidationError("query is required")
        
        valid_types = ["code", "file"]
        if search_type not in valid_types:
            raise ValidationError(f"search_type must be one of: {', '.join(valid_types)}")
        
        client = get_bitbucket_client(credentials)
        
        # Build search query with filters
        search_query = query
        if search_type == "file":
            if "ext:" not in query and not query.startswith('"'):
                search_query = f'"{query}"'
        
        if project_key:
            search_query += f" project:{project_key}"
        if repository_slug and project_key:
            search_query += f" repo:{project_key}/{repository_slug}"
        
        payload = {
            "query": search_query,
            "entities": {
                "code": {
                    "start": start,
                    "limit": min(limit, 100)
                }
            }
        }
        
        # Search API uses a different endpoint
        endpoint = "/rest/search/latest/search"
        data = client.post(endpoint, json=payload)
        
        code_results = data.get("code", {})
        results = []
        
        for item in code_results.get("values", []):
            repo = item.get("repository", {})
            project = repo.get("project", {})
            file_info = item.get("file", {})
            
            # Handle file field - can be string or object depending on API version
            if isinstance(file_info, str):
                file_path = file_info
            else:
                file_path = file_info.get("toString", file_info.get("path", ""))
            
            results.append({
                "repository": repo.get("slug", ""),
                "project": project.get("key", ""),
                "file": file_path,
                "hit_count": item.get("hitCount", 0),
                "matches": [
                    m.get("text", "") if isinstance(m, dict) else str(m) if not isinstance(m, list) else ""
                    for m in item.get("hitContexts", [])
                ],
            })
        
        result = {
            "query": search_query,
            "total": code_results.get("count", 0),
            "results": results,
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
