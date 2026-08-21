---
name: atlassian-skills
description: Python utilities for Jira (SIL Data Center) covering issue management, JQL search, workflows and transitions, links, agile boards, worklogs and projects. Use when users need to create, update, comment on, link or transition a Jira issue, or search Jira with JQL -- including any LT-prefixed FieldWorks ticket.
license: Complete terms in LICENSE
---

# Atlassian Skills

Python utilities for Jira, supporting both Cloud and Data Center deployments.

## FieldWorks / SIL JIRA Integration

**LT-prefixed tickets** (e.g., `LT-22382`, `LT-19288`) are JIRA issues from SIL's JIRA instance:
- **Base URL:** `https://jira.sil.org`
- **Browse URL:** `https://jira.sil.org/browse/LT-XXXXX`
- **Project key:** `LT` (Language Technology)

**Trigger patterns:** Use this skill when you see:
- LT-prefixed identifiers in user queries, code comments, commit messages, or git log output
- References to `jira.sil.org` URLs
- Requests to create/update/comment on JIRA tickets

> **Note**: Default to `atlassian-readonly-skills` for read operations. Use this full skill set only when the user explicitly requests create/update/delete operations.

### ⚠️ Critical: Always Use Python Scripts

**NEVER** attempt to:
- Browse to `jira.sil.org` URLs directly (requires authentication)
- Use `fetch_webpage` or similar tools on JIRA URLs
- Use GitHub issue tools for LT-* tickets

**ALWAYS** use these Python modules. The scripts are Python modules (not CLI tools), so use them via inline Python or import:

```powershell
# Create a new issue
python -c "import sys; sys.path.insert(0, '.claude/skills/atlassian-skills/scripts'); from jira_issues import jira_create_issue; print(jira_create_issue('LT', 'Issue title', 'Bug'))"

# Update an existing issue
python -c "import sys; sys.path.insert(0, '.claude/skills/atlassian-skills/scripts'); from jira_issues import jira_update_issue; print(jira_update_issue('LT-22382', summary='Updated title'))"

# Add a comment
python -c "import sys; sys.path.insert(0, '.claude/skills/atlassian-skills/scripts'); from jira_issues import jira_add_comment; print(jira_add_comment('LT-22382', 'Comment text'))"

# Transition issue status
python -c "import sys; sys.path.insert(0, '.claude/skills/atlassian-skills/scripts'); from jira_workflow import jira_transition_issue; print(jira_transition_issue('LT-22382', 'In Progress'))"
```

For read-only operations (get issue, search, get comments), use `atlassian-readonly-skills` instead.

## Configuration

Jira only. Confluence and Bitbucket support was removed from this copy -- see
`PROVENANCE.md`.

### Mode 1: environment variables

SIL JIRA (Data Center, PAT token) -- this is the one FieldWorks uses:

```bash
JIRA_URL=https://jira.sil.org
# Generate at https://jira.sil.org/secure/ViewProfile.jspa -> Personal Access Tokens
JIRA_PAT_TOKEN=your_jira_pat_token_here
```

Jira Cloud (API token), for completeness:

```bash
JIRA_URL=https://your-company.atlassian.net
JIRA_USERNAME=your.email@company.com
JIRA_API_TOKEN=your_api_token
```

A PAT token takes precedence if both are provided.

### Mode 2: credentials parameter (agent environments)

```python
from scripts._common import AtlassianCredentials

credentials = AtlassianCredentials(
    jira_url="https://jira.sil.org",
    jira_pat_token="your_pat_token",
)
```

Every function takes an optional `credentials` argument. Without one, the
environment variables above are used.

## Core Workflow

```python
import json
from scripts.jira_issues import jira_get_issue
from scripts.jira_search import jira_search

issue = json.loads(jira_get_issue(issue_key="LT-22382"))
print(f"{issue['key']} - {issue['summary']}")

results = json.loads(jira_search(
    jql="project = LT AND status = 'In Progress'",
    fields="summary,status,assignee",
    limit=50,
))
```

Pass `credentials=credentials` to any of them to use Mode 2 instead of the
environment.

## Available Utilities

### Jira Issue Management (`scripts.jira_issues`)

```python
from scripts.jira_issues import (
    jira_get_issue,      # Get issue by key
    jira_create_issue,   # Create new issue
    jira_update_issue,   # Update existing issue
    jira_delete_issue,   # Delete issue
    jira_add_comment     # Add comment to issue
)

# Create issue with full options
jira_create_issue(
    project_key="PROJ",
    summary="Bug fix",
    issue_type="Bug",
    description="Description",
    assignee="user@company.com",
    priority="High",
    labels=["urgent", "backend"],
    custom_fields={
        "customfield_10001": "Custom value",
        "customfield_10002": 123
    }
)
```

### Jira Search (`scripts.jira_search`)

```python
from scripts.jira_search import jira_search, jira_search_fields

# Search with JQL
jira_search(
    jql="project = PROJ AND status = 'In Progress'",
    fields="summary,status,assignee",
    limit=50
)

# Find field definitions
jira_search_fields(keyword="custom")
```

### Jira Workflow (`scripts.jira_workflow`)

```python
from scripts.jira_workflow import jira_get_transitions, jira_transition_issue

# Get available transitions
jira_get_transitions(issue_key="PROJ-123")

# Transition issue to new status
jira_transition_issue(
    issue_key="PROJ-123",
    transition_id="31",
    comment="Moving to review"
)
```

### Jira Agile (`scripts.jira_agile`)

```python
from scripts.jira_agile import (
    jira_get_agile_boards,
    jira_get_sprints_from_board,
    jira_get_sprint_issues,
    jira_create_sprint,
    jira_update_sprint
)

# Get boards
jira_get_agile_boards(project_key="PROJ")

# Get active sprints
jira_get_sprints_from_board(board_id=1, state="active")

# Create new sprint
jira_create_sprint(
    board_id=1,
    sprint_name="Sprint 5",
    start_date="2024-01-15",
    end_date="2024-01-29",
    goal="Complete feature X"
)
```

### Jira Links (`scripts.jira_links`)

```python
from scripts.jira_links import (
    jira_get_link_types,
    jira_create_issue_link,
    jira_link_to_epic,
    jira_remove_issue_link
)

# Link issues
jira_create_issue_link(
    link_type="Blocks",
    inward_issue_key="PROJ-123",
    outward_issue_key="PROJ-456"
)

# Link to epic
jira_link_to_epic(issue_key="PROJ-123", epic_key="PROJ-100")
```

### Jira Worklog (`scripts.jira_worklog`)

```python
from scripts.jira_worklog import jira_get_worklog, jira_add_worklog

# Add time spent
jira_add_worklog(
    issue_key="PROJ-123",
    time_spent="2h 30m",
    comment="Code review"
)
```

### Jira Projects (`scripts.jira_projects`)

```python
from scripts.jira_projects import (
    jira_get_all_projects,
    jira_get_project_issues,
    jira_get_project_versions,
    jira_create_version
)

# Get all projects
jira_get_all_projects()

# Create version
jira_create_version(
    project_key="PROJ",
    name="v2.0.0",
    release_date="2024-03-01"
)
```

### Jira Users (`scripts.jira_users`)

```python
from scripts.jira_users import jira_get_user_profile

# Get Jira user profile
jira_get_user_profile(
    user_identifier="user@company.com",
    credentials=credentials  # Optional
)
```

## Function Signature Pattern

All skill functions follow this signature pattern:

```python
def skill_function(
    required_param1: str,
    required_param2: str,
    optional_param: Optional[str] = None,
    credentials: Optional[AtlassianCredentials] = None  # Always last parameter
) -> str:
    """Function description.
    
    Args:
        required_param1: Description
        required_param2: Description
        optional_param: Description (optional)
        credentials: Optional AtlassianCredentials for Agent environments.
                    If not provided, uses environment variables.
    
    Returns:
        JSON string with result or error information
    """
```

The `credentials` parameter is:
- **Optional**: If not provided, configuration loads from environment variables
- **Always the last parameter**: Maintains consistent function signatures
- **Backward compatible**: Existing code without credentials parameter continues to work

## Response Data Structures

All functions return JSON strings with **flattened** data structures (not nested API responses).

### Jira Issue Structure

```json
{
  "key": "PROJ-123",
  "id": "10001",
  "summary": "Issue title",
  "description": "Issue description",
  "status": "In Progress",
  "issue_type": "Task",
  "priority": "High",
  "assignee": "user@company.com",
  "reporter": "reporter@company.com",
  "created": "2024-01-15T10:30:00.000+0000",
  "updated": "2024-01-16T14:20:00.000+0000",
  "labels": ["backend", "urgent"],
  "components": ["API", "Auth"],
  "custom_fields": {}
}
```

## Error Handling

All functions return JSON strings. Check for errors:

```python
import json

result = jira_get_issue(issue_key="PROJ-999")
data = json.loads(result)

if not data.get("success", True):
    print(f"Error: {data['error']}")
    print(f"Type: {data['error_type']}")
else:
    print(f"Issue: {data['key']}")
```

### Error Types

- `ConfigurationError` - Missing environment variables
- `AuthenticationError` - Invalid credentials
- `ValidationError` - Invalid input parameters
- `NotFoundError` - Resource not found
- `APIError` - Atlassian API error
- `NetworkError` - Connection issues

## Philosophy

This skill provides:
- **Utilities**: Ready-to-use functions for common Atlassian operations
- **Flexibility**: Support for both Cloud and Data Center deployments
- **Dual Configuration**: Environment variables OR parameter-based credentials
- **Agent-Ready**: Works in environments without environment variable access
- **Consistency**: Unified error handling and response format
- **Backward Compatible**: Existing code continues to work without changes

It does NOT provide:
- Direct API access (use the provided functions instead)
- Webhook handling or event processing
- Bulk import/export operations

**Best practices**:
- Always check return values for errors
- Use JQL/CQL for efficient searching
- Batch operations when possible to reduce API calls
- In Agent environments, check service availability before calling skills:
  ```python
  availability = check_available_skills(credentials)
  if "jira" in availability["available_services"]:
      result = jira_get_issue("PROJ-123", credentials=credentials)
  ```

## Dependencies

```bash
pip install requests python-dotenv
```

Or use the requirements file:

```bash
pip install -r requirements.txt
```
