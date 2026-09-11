# Atlassian Readonly Skills API Reference

Detailed usage examples and API documentation for the read-only Jira operations.

> **Note**: This is a read-only variant. For write operations (create, update, delete), use `atlassian-skills`.

## Configuration Modes

All functions support two configuration modes:

### Mode 1: Environment Variables (Traditional)

```python
# Set environment variables first
# JIRA_URL=https://company.atlassian.net
# JIRA_USERNAME=user@company.com
# JIRA_API_TOKEN=your_token

from scripts.jira_issues import jira_get_issue

# Uses environment variables automatically
result = jira_get_issue("PROJ-123")
```

### Mode 2: Parameter-Based (Agent Mode)

```python
from scripts._common import AtlassianCredentials, check_available_skills
from scripts.jira_issues import jira_get_issue

# Create credentials object
credentials = AtlassianCredentials(
    jira_url="https://company.atlassian.net",
    jira_username="user@company.com",
    jira_api_token="your_token"
)

# Check which services are available
availability = check_available_skills(credentials)
print(availability["available_services"])  # ["jira"]

# Pass credentials to functions
result = jira_get_issue("PROJ-123", credentials=credentials)
```

**Note:** All examples below can use either mode. For Agent mode, simply add `credentials=credentials` as the last parameter to any function call.

## Jira Examples

### Search for Issues

```python
from scripts.jira_search import jira_search

# Using environment variables
result = jira_search("project = MYPROJ AND status = 'In Progress'", limit=10)

# Using credentials parameter (Agent mode)
from scripts._common import AtlassianCredentials

credentials = AtlassianCredentials(
    jira_url="https://company.atlassian.net",
    jira_username="user@company.com",
    jira_api_token="token"
)

result = jira_search(
    jql="project = MYPROJ AND status = 'In Progress'",
    limit=10,
    credentials=credentials
)

# Search with specific fields
result = jira_search(
    jql="assignee = currentUser() AND status != Done",
    fields="summary,status,priority",
    limit=20,
    credentials=credentials  # Optional
)
```

### Get Available Fields

```python
from scripts.jira_search import jira_search_fields

# Get all available fields for searching
result = jira_search_fields()
```

### Get Issue Details

```python
from scripts.jira_issues import jira_get_issue

# Get issue by key
result = jira_get_issue("MYPROJ-123")

# Get issue with specific fields
result = jira_get_issue("MYPROJ-123", fields="summary,status,assignee,priority")
```

### Get Available Transitions

```python
from scripts.jira_workflow import jira_get_transitions

# Get available transitions for an issue
transitions = jira_get_transitions("MYPROJ-123")
```

### Get Worklog Entries

```python
from scripts.jira_worklog import jira_get_worklog

# Get worklog for an issue
result = jira_get_worklog("MYPROJ-123")
```

### Get Link Types

```python
from scripts.jira_links import jira_get_link_types

# Get available link types
result = jira_get_link_types()
```

### Get Projects and Versions

```python
from scripts.jira_projects import jira_get_all_projects, jira_get_project_issues, jira_get_project_versions

# Get all projects
projects = jira_get_all_projects()

# Get issues for a project
issues = jira_get_project_issues("MYPROJ", limit=50)

# Get versions for a project
versions = jira_get_project_versions("MYPROJ")
```

### Get User Profile

```python
from scripts.jira_users import jira_get_user_profile

# Get user profile by account ID
result = jira_get_user_profile("5d123abc456def789")

# Get user profile by email
result = jira_get_user_profile(email="user@example.com")
```

### Agile Boards and Sprints

```python
from scripts.jira_agile import (
    jira_get_agile_boards,
    jira_get_board_issues,
    jira_get_sprints_from_board,
    jira_get_sprint_issues
)

# Find boards
boards = jira_get_agile_boards(project_key="MYPROJ")

# Get issues on a board
issues = jira_get_board_issues(board_id="123", limit=50)

# Get sprints from a board
sprints = jira_get_sprints_from_board(board_id="123", state="active")

# Get issues in a sprint
sprint_issues = jira_get_sprint_issues(sprint_id="456", limit=50)
```

## JQL Query Examples

Common JQL patterns for searching Jira issues:

```
# Issues assigned to current user
assignee = currentUser()

# Open issues in a project
project = MYPROJ AND status != Done

# High priority bugs
project = MYPROJ AND issuetype = Bug AND priority = High

# Issues updated in last 7 days
updated >= -7d

# Issues in current sprint
sprint in openSprints()

# Unassigned issues
assignee is EMPTY

# Issues with specific label
labels = "backend"

# Issues created by specific user
reporter = "user@example.com"

# Combined query
project = MYPROJ AND status = "In Progress" AND assignee = currentUser() ORDER BY priority DESC
```

## Response Format

### Success Response

```json
{
  "key": "MYPROJ-123",
  "summary": "Issue title",
  "status": "In Progress",
  "assignee": "user@example.com"
}
```

### Error Response

```json
{
  "success": false,
  "error": "Issue not found: MYPROJ-999",
  "error_type": "NotFoundError"
}
```

### Paginated Response

```json
{
  "issues": [...],
  "total": 150,
  "start_at": 0,
  "max_results": 10,
  "is_last": false
}
```

## Agent Mode Complete Example

```python
import json
from scripts._common import AtlassianCredentials, check_available_skills
from scripts.jira_issues import jira_get_issue
from scripts.jira_search import jira_search

credentials = AtlassianCredentials(
    jira_url="https://jira.sil.org",
    jira_pat_token="your_pat_token",
)

availability = check_available_skills(credentials)
if "jira" not in availability["available_services"]:
    raise SystemExit(availability["unavailable_services"]["jira"])

issue = json.loads(jira_get_issue("LT-22382", credentials=credentials))
print(f"{issue['key']}: {issue['summary']}")

results = json.loads(jira_search(
    jql="project = LT AND status = 'In Progress'",
    fields="summary,status,assignee",
    limit=50,
    credentials=credentials,
))
```

## Credentials Object Reference

```python
AtlassianCredentials(
    jira_url: Optional[str] = None,
    jira_username: Optional[str] = None,
    jira_api_token: Optional[str] = None,
    jira_pat_token: Optional[str] = None,
    jira_api_version: Optional[str] = None,
    jira_ssl_verify: bool = True,
)
```

For SIL's Data Center instance, `jira_url` plus `jira_pat_token` is enough.
`jira_username` and `jira_api_token` are the Cloud pairing. A PAT token wins if
both are supplied.

`check_available_skills(credentials)` returns `available_services` and
`unavailable_services`, the latter naming the missing field.
