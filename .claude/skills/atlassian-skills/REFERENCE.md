# Atlassian Skills API Reference

Detailed usage examples and API documentation for Jira, Confluence, and Bitbucket tools.

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

## Confluence Examples

### Search for Pages

```python
from scripts.confluence_search import confluence_search

result = confluence_search("API documentation", limit=10)
```

### Get a Page

```python
from scripts.confluence_pages import confluence_get_page

# Get by ID
result = confluence_get_page(page_id="123456")

# Get by title and space
result = confluence_get_page(title="API Guide", space_key="DEV")
```

### Create a Page

```python
from scripts.confluence_pages import confluence_create_page

result = confluence_create_page(
    space_key="DEV",
    title="New Documentation Page",
    content="# Welcome\n\nThis is **markdown** content.",
    parent_id="123456"
)
```

### Update a Page

```python
from scripts.confluence_pages import confluence_update_page

result = confluence_update_page(
    page_id="123456",
    title="Updated Title",
    content="# Updated Content\n\nNew information here."
)
```

### Manage Comments

```python
from scripts.confluence_comments import confluence_add_comment, confluence_get_comments

# Get comments
comments = confluence_get_comments(page_id="123456")

# Add comment
result = confluence_add_comment(
    page_id="123456",
    content="Great documentation!"
)
```

### Manage Labels

```python
from scripts.confluence_labels import confluence_add_label, confluence_remove_label

# Add label
result = confluence_add_label(page_id="123456", name="api-docs")

# Remove label
result = confluence_remove_label(page_id="123456", name="outdated")
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

## CQL Query Examples

Common CQL patterns for searching Confluence:

```
# Search by text
text ~ "API documentation"

# Search in specific space
space = DEV AND text ~ "guide"

# Search by title
title ~ "Getting Started"

# Search by label
label = "api-docs"

# Search by type
type = page AND space = DEV

# Recently modified
lastModified >= now("-7d")

# Created by user
creator = "user@example.com"
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

## Bitbucket Examples

### Get Recent Commits

```python
from scripts.bitbucket_commits import bitbucket_get_commits
import json

# Get latest 10 commits from master branch
result = bitbucket_get_commits(
    project_key="PROJ",
    repository_slug="my-repo",
    branch="master",
    limit=10
)

# Parse and display commits
data = json.loads(result)
for commit in data['commits']:
    print(f"{commit['display_id']}: {commit['message']}")
    print(f"  Author: {commit['author_name']}")
    print(f"  Date: {commit['timestamp']}")
```

### Get Specific Commit Details

```python
from scripts.bitbucket_commits import bitbucket_get_commit
import json

# Get details of a specific commit
result = bitbucket_get_commit(
    project_key="PROJ",
    repository_slug="my-repo",
    commit_id="1da11eaec25aed8b251de24841885c91493b3173"
)

# Parse commit details
commit = json.loads(result)
print(f"Commit: {commit['display_id']}")
print(f"Message: {commit['message']}")
print(f"Author: {commit['author_name']} <{commit['author_email']}>")
print(f"Timestamp: {commit['timestamp']}")
```

### List Pull Requests

```python
from scripts.bitbucket_pull_requests import bitbucket_get_pull_request

# Get PR details
result = bitbucket_get_pull_request(
    project_key="PROJ",
    repository_slug="my-repo",
    pr_id=123
)
```

### Search Code

```python
from scripts.bitbucket_files import bitbucket_search

# Search for specific code pattern
result = bitbucket_search(
    query="def authenticate",
    project_key="PROJ",
    search_type="code",
    limit=25
)
```

### Bitbucket Commit Response Structure

```json
{
  "project_key": "PROJ",
  "repository": "my-repo",
  "branch": "master",
  "total": 5,
  "is_last_page": false,
  "commits": [
    {
      "id": "1da11eaec25aed8b251de24841885c91493b3173",
      "display_id": "1da11eaec25",
      "message": "Feature: Add new API endpoint",
      "author_name": "John Doe",
      "author_email": "john.doe@company.com",
      "committer_name": "John Doe",
      "committer_email": "john.doe@company.com",
      "timestamp": 1765534577000,
      "parents": ["b97ad25d330e36480b045c5dea36f97999297ff6"]
    }
  ]
}
```

## Agent Mode Complete Example

When deploying in Agent environments without environment variables:

```python
from scripts._common import AtlassianCredentials, check_available_skills
from scripts.jira_issues import jira_create_issue, jira_get_issue
from scripts.jira_search import jira_search
from scripts.confluence_pages import confluence_create_page
import json

# Step 1: Create credentials object with all needed services
credentials = AtlassianCredentials(
    # Jira configuration
    jira_url="https://company.atlassian.net",
    jira_username="user@company.com",
    jira_api_token="jira_token_here",
    
    # Confluence configuration
    confluence_url="https://company.atlassian.net/wiki",
    confluence_username="user@company.com",
    confluence_api_token="confluence_token_here",
    
    # Bitbucket configuration (optional)
    bitbucket_url="https://bitbucket.company.com",
    bitbucket_pat_token="bitbucket_pat_here"
)

# Step 2: Check which services are available
availability = check_available_skills(credentials)
print(f"Available: {availability['available_services']}")
print(f"Unavailable: {availability['unavailable_services']}")

# Step 3: Use skills with credentials parameter
if "jira" in availability["available_services"]:
    # Create an issue
    result = jira_create_issue(
        project_key="PROJ",
        summary="New task from Agent",
        issue_type="Task",
        credentials=credentials  # Pass credentials
    )
    issue = json.loads(result)
    
    if not issue.get("error"):
        print(f"Created issue: {issue['key']}")
        
        # Get the issue
        result = jira_get_issue(
            issue_key=issue['key'],
            credentials=credentials
        )
        
        # Search for issues
        result = jira_search(
            jql=f"project = PROJ AND key = {issue['key']}",
            credentials=credentials
        )

if "confluence" in availability["available_services"]:
    # Create a Confluence page
    result = confluence_create_page(
        space_key="TEAM",
        title="Documentation from Agent",
        content="<p>Created by Agent</p>",
        credentials=credentials
    )
    page = json.loads(result)
    if not page.get("error"):
        print(f"Created page: {page['title']}")

# Step 4: Handle unavailable services
if "bitbucket" not in availability["available_services"]:
    reason = availability["unavailable_services"].get("bitbucket", "Unknown")
    print(f"Bitbucket unavailable: {reason}")
```

## Partial Service Configuration

You can configure only the services you need:

```python
from scripts._common import AtlassianCredentials, check_available_skills

# Only Jira configured
jira_only_creds = AtlassianCredentials(
    jira_url="https://company.atlassian.net",
    jira_username="user@company.com",
    jira_api_token="token"
)

# Check availability
availability = check_available_skills(jira_only_creds)
# Returns: {
#   "available_services": ["jira"],
#   "unavailable_services": {
#     "confluence": "Missing confluence_url",
#     "bitbucket": "Missing bitbucket_url"
#   }
# }

# Jira functions work
from scripts.jira_issues import jira_get_issue
result = jira_get_issue("PROJ-123", credentials=jira_only_creds)  # ✓ Works

# Confluence functions fail with clear error
from scripts.confluence_pages import confluence_get_page
result = confluence_get_page("Page", "SPACE", credentials=jira_only_creds)  # ✗ Fails
# Returns: {"error": "Confluence credentials not provided...", "error_type": "ConfigurationError"}
```

## Credentials Object Reference

```python
AtlassianCredentials(
    # Jira
    jira_url: Optional[str] = None,
    jira_username: Optional[str] = None,
    jira_api_token: Optional[str] = None,
    jira_pat_token: Optional[str] = None,
    jira_api_version: Optional[str] = None,  # '2' or '3', auto-detected if not set
    jira_ssl_verify: bool = False,
    
    # Confluence
    confluence_url: Optional[str] = None,
    confluence_username: Optional[str] = None,
    confluence_api_token: Optional[str] = None,
    confluence_pat_token: Optional[str] = None,
    confluence_api_version: Optional[str] = None,
    confluence_ssl_verify: bool = False,
    
    # Bitbucket
    bitbucket_url: Optional[str] = None,
    bitbucket_username: Optional[str] = None,
    bitbucket_api_token: Optional[str] = None,
    bitbucket_pat_token: Optional[str] = None,
    bitbucket_api_version: Optional[str] = None,
    bitbucket_ssl_verify: bool = False
)
```

For each service, provide either:
- **PAT Token** (for Data Center/Server): `{service}_pat_token`
- **Username + API Token** (for Cloud): `{service}_username` + `{service}_api_token`
