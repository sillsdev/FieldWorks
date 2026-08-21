---
name: atlassian-readonly-skills
description: Read-only Python utilities for Jira, Confluence, and Bitbucket integration. Provides read access to issues, search, workflows, pages, pull requests, commit history, and more. Use when users need to query Atlassian products like "get a Jira issue", "search Confluence pages", "view pull request details", or "get commit history". This variant excludes all write operations for token efficiency and safety.
license: Complete terms in LICENSE
---

# Atlassian Readonly Skills

The read-only half of `atlassian-skills`: same client, same configuration, same
response shapes, with every create/update/delete function removed. Prefer it
whenever the task only reads -- it cannot modify anything by accident.

## FieldWorks / SIL JIRA Integration

**LT-prefixed tickets** (e.g., `LT-22382`, `LT-19288`) are JIRA issues from SIL's JIRA instance:
- **Base URL:** `https://jira.sil.org`
- **Browse URL:** `https://jira.sil.org/browse/LT-XXXXX`
- **Project key:** `LT` (Language Technology)

**Trigger patterns:** Use this skill when you see:
- LT-prefixed identifiers in user queries, code comments, commit messages, or git log output
- References to `jira.sil.org` URLs
- Requests to "look up" or "check" a JIRA ticket

### ⚠️ Critical: Always Use Python Scripts

**NEVER** attempt to:
- Browse to `jira.sil.org` URLs directly (requires authentication)
- Use `fetch_webpage` or similar tools on JIRA URLs
- Use GitHub issue tools for LT-* tickets

**ALWAYS** use these Python modules. The scripts are Python modules (not CLI tools), so use them via inline Python or import:

```powershell
# Get a single issue (inline Python one-liner)
python -c "import sys; sys.path.insert(0, '.claude/skills/atlassian-readonly-skills/scripts'); from jira_issues import jira_get_issue; print(jira_get_issue('LT-22382'))"

# Search for issues (JQL query)
python -c "import sys; sys.path.insert(0, '.claude/skills/atlassian-readonly-skills/scripts'); from jira_search import jira_search; print(jira_search('project = LT AND status = Open'))"

# Get issue workflow transitions
python -c "import sys; sys.path.insert(0, '.claude/skills/atlassian-readonly-skills/scripts'); from jira_workflow import jira_get_transitions; print(jira_get_transitions('LT-22382'))"
```

## Configuration

Two configuration modes are supported:

### Mode 1: Environment Variables (Traditional)

Set environment variables based on your deployment type. This mode is used when `credentials` parameter is not provided to skill functions.

#### SIL JIRA (Data Center / PAT Token)

```bash
# SIL JIRA instance for LT-* tickets
JIRA_URL=https://jira.sil.org
# Personal Access Token - generate at: https://jira.sil.org/secure/ViewProfile.jspa → Personal Access Tokens
JIRA_PAT_TOKEN=your_jira_pat_token_here
```

#### Cloud (API Token)

```bash
# Jira Cloud
JIRA_URL=https://your-company.atlassian.net
JIRA_USERNAME=your.email@company.com
JIRA_API_TOKEN=your_api_token

# Confluence Cloud
CONFLUENCE_URL=https://your-company.atlassian.net/wiki
CONFLUENCE_USERNAME=your.email@company.com
CONFLUENCE_API_TOKEN=your_api_token
```

Generate API tokens at: https://id.atlassian.com/manage-profile/security/api-tokens

#### Data Center / Server (PAT Token)

```bash
# Jira Data Center
JIRA_URL=https://jira.your-company.com
JIRA_PAT_TOKEN=your_pat_token

# Confluence Data Center
CONFLUENCE_URL=https://confluence.your-company.com
CONFLUENCE_PAT_TOKEN=your_pat_token

# Bitbucket Server/Data Center
BITBUCKET_URL=https://bitbucket.your-company.com
BITBUCKET_PAT_TOKEN=your_pat_token
```

> **Note**: PAT Token takes precedence if both are provided.

### Mode 2: Parameter-Based (Agent Environments)

Alternatively, call the scripts in this skill directly.

# Create credentials object
credentials = AtlassianCredentials(
    # Jira configuration
    jira_url="https://your-company.atlassian.net",
    jira_username="your.email@company.com",
    jira_api_token="your_api_token",

    # Confluence configuration (optional)
    confluence_url="https://your-company.atlassian.net/wiki",
    confluence_username="your.email@company.com",
    confluence_api_token="your_api_token",

    # Bitbucket configuration (optional)
    # bitbucket_url="https://bitbucket.your-company.com",
    # bitbucket_pat_token="your_pat_token"
)

# Check which services are available
availability = check_available_skills(credentials)
print(availability["available_services"])  # ["jira", "confluence"]
print(availability["unavailable_services"])  # {"bitbucket": "Missing bitbucket_url"}

# Use skills with credentials parameter
result = jira_get_issue(
    issue_key="PROJ-123",
    credentials=credentials  # Pass credentials here
)
```

#### Partial Service Configuration

You can configure only the services you need. Services without complete credentials will be unavailable:

```python
# Only configure Jira
credentials = AtlassianCredentials(
    jira_url="https://your-company.atlassian.net",
    jira_username="your.email@company.com",
    jira_api_token="your_api_token"
)

# Jira skills will work
jira_get_issue("PROJ-123", credentials=credentials)  # ✓ Works

# Confluence/Bitbucket skills will fail with ConfigurationError
confluence_get_page("Page Title", "SPACE", credentials=credentials)  # ✗ Fails
```

For credentials object fields and authentication options, see the full documentation in `atlassian-skills`.

## Core Workflow

### Using Environment Variables

```python
from scripts.jira_issues import jira_get_issue
from scripts.jira_search import jira_search
from scripts.confluence_pages import confluence_get_page
import json

# 1. Get a Jira issue
result = jira_get_issue(issue_key="PROJ-123")
issue = json.loads(result)
print(f"Issue: {issue['key']} - {issue['summary']}")

# 2. Search for issues
result = jira_search(
    jql="project = PROJ AND status = 'In Progress'",
    fields="summary,status,assignee",
    limit=50
)
issues = json.loads(result)

# 3. Get a Confluence page
result = confluence_get_page(title="Feature Documentation", space_key="DEV")
page = json.loads(result)
print(f"Page: {page['title']}")
```

### Using Credentials Parameter (Agent Mode)

```python
from scripts._common import AtlassianCredentials
from scripts.jira_issues import jira_get_issue
from scripts.jira_search import jira_search
from scripts.confluence_pages import confluence_get_page
import json

# Create credentials
credentials = AtlassianCredentials(
    jira_url="https://company.atlassian.net",
    jira_username="user@company.com",
    jira_api_token="token123",
    confluence_url="https://company.atlassian.net/wiki",
    confluence_username="user@company.com",
    confluence_api_token="token123"
)

# 1. Get a Jira issue with credentials
result = jira_get_issue(
    issue_key="PROJ-123",
    credentials=credentials
)
issue = json.loads(result)

# 2. Search for issues with credentials
result = jira_search(
    jql="project = PROJ AND status = 'In Progress'",
    fields="summary,status,assignee",
    limit=50,
    credentials=credentials
)

# 3. Get a Confluence page with credentials
result = confluence_get_page(
    title="Feature Documentation",
    space_key="DEV",
    credentials=credentials
)
```

## What is available here

Every function in this variant, by module. Anything not listed is a write
operation and lives in `atlassian-skills` instead.

| Module | Functions |
| --- | --- |
| `jira_issues` | `jira_get_issue` |
| `jira_search` | `jira_search`, `jira_search_fields` |
| `jira_workflow` | `jira_get_transitions` |
| `jira_projects` | `jira_get_all_projects`, `jira_get_project_issues`, `jira_get_project_versions` |
| `jira_agile` | `jira_get_agile_boards`, `jira_get_board_issues`, `jira_get_sprints_from_board`, `jira_get_sprint_issues` |
| `jira_links` | `jira_get_link_types` |
| `jira_worklog` | `jira_get_worklog` |
| `jira_users` | `jira_get_user_profile` |
| `confluence_pages` | `confluence_get_page` |
| `confluence_search` | `confluence_search` |
| `confluence_comments` | `confluence_get_comments` |
| `confluence_labels` | `confluence_get_labels` |
| `bitbucket_projects` | `bitbucket_list_projects`, `bitbucket_list_repositories` |
| `bitbucket_pull_requests` | `bitbucket_get_pull_request`, `bitbucket_get_pr_diff` |
| `bitbucket_files` | `bitbucket_get_file_content`, `bitbucket_search` |
| `bitbucket_commits` | `bitbucket_get_commits`, `bitbucket_get_commit` |

Signatures, arguments and per-function detail are in `REFERENCE.md`.

## Shared with `atlassian-skills`, documented once

These three sections are **byte-identical** between the two variants, so they
live only in `../atlassian-skills/SKILL.md`. Read them there:

- **Response data structures** -- the simplified issue and page shapes
- **Error handling** -- the error envelope and the error type list
- **Dependencies** -- `requirements.txt`

Its *Configuration*, *Core workflow* and *Philosophy* sections are supersets of
the ones above, carrying write examples that do not apply here. The versions
above are the ones that apply to this variant.

## Provenance and known bugs

See `PROVENANCE.md`: upstream repository, the MIT declaration and its gaps, the
local modifications, and the Data Center behaviours that upstream's
Cloud-oriented docstrings get wrong.
