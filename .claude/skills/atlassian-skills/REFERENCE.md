# Atlassian Skills API Reference

Detailed usage examples and API documentation for the Jira tools.

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

### Create an Issue

```python
from scripts.jira_issues import jira_create_issue

# Environment variable mode
result = jira_create_issue(
    project_key="MYPROJ",
    summary="Implement new feature",
    issue_type="Task",
    description="Detailed description here",
    priority="High",
    labels=["backend", "api"],
    custom_fields={
        "customfield_10001": "Sprint 5",
        "customfield_10002": {"value": "Option A"}
    }
)

# Agent mode with credentials
result = jira_create_issue(
    project_key="MYPROJ",
    summary="Implement new feature",
    issue_type="Task",
    description="Detailed description here",
    priority="High",
    labels=["backend", "api"],
    custom_fields={
        "customfield_10001": "Sprint 5",
        "customfield_10002": {"value": "Option A"}
    },
    credentials=credentials  # Pass credentials
)
```

### Update an Issue

```python
from scripts.jira_issues import jira_update_issue

result = jira_update_issue(
    issue_key="MYPROJ-123",
    summary="Updated summary",
    priority="Critical",
    custom_fields={
        "customfield_10001": "Updated value"
    }
)
```

### Transition an Issue

```python
from scripts.jira_workflow import jira_get_transitions, jira_transition_issue

# Get available transitions
transitions = jira_get_transitions("MYPROJ-123")

# Transition to a new status
result = jira_transition_issue(
    issue_key="MYPROJ-123",
    transition_id="31",
    comment="Moving to In Progress"
)
```

### Add Worklog

```python
from scripts.jira_worklog import jira_add_worklog

result = jira_add_worklog(
    issue_key="MYPROJ-123",
    time_spent="2h 30m",
    comment="Worked on implementation"
)
```

### Link Issues

```python
from scripts.jira_links import jira_create_issue_link, jira_link_to_epic

# Create a link between issues
result = jira_create_issue_link(
    link_type="Blocks",
    inward_issue_key="MYPROJ-123",
    outward_issue_key="MYPROJ-456"
)

# Link to an epic
result = jira_link_to_epic(
    issue_key="MYPROJ-123",
    epic_key="MYPROJ-100"
)
```

### Agile Boards and Sprints

```python
from scripts.jira_agile import (
    jira_get_agile_boards,
    jira_get_sprints_from_board,
    jira_create_sprint
)

# Find boards
boards = jira_get_agile_boards(project_key="MYPROJ")

# Get active sprints
sprints = jira_get_sprints_from_board(board_id="123", state="active")

# Create a sprint
result = jira_create_sprint(
    board_id="123",
    sprint_name="Sprint 5",
    start_date="2024-01-15",
    end_date="2024-01-29",
    goal="Complete API integration"
)
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

## Time Format Reference

For worklog entries, use these time formats:

| Format | Example | Description |
|--------|---------|-------------|
| Weeks | `1w` | 1 week |
| Days | `2d` | 2 days |
| Hours | `3h` | 3 hours |
| Minutes | `30m` | 30 minutes |
| Combined | `1d 4h 30m` | 1 day, 4 hours, 30 minutes |
| Seconds | `3600` | 3600 seconds (1 hour) |

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
from scripts.jira_issues import jira_create_issue, jira_add_comment

credentials = AtlassianCredentials(
    jira_url="https://jira.sil.org",
    jira_pat_token="your_pat_token",
)

availability = check_available_skills(credentials)
if "jira" not in availability["available_services"]:
    raise SystemExit(availability["unavailable_services"]["jira"])

created = json.loads(jira_create_issue(
    project_key="LT",
    summary="Example issue",
    issue_type="Bug",
    description="Filed from agent mode.",
    custom_fields={"versions": [{"name": "FW 9.3"}]},
    credentials=credentials,
))
jira_add_comment(created["key"], "First comment.", credentials=credentials)
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
