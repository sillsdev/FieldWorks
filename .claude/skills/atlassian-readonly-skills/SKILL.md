---
name: atlassian-readonly-skills
description: Read-only Python utilities for Jira (SIL Data Center): fetch an issue, search with JQL, read transitions, links, worklogs, agile boards and projects. Use when users need to look up or query a Jira issue -- including any LT-prefixed FieldWorks ticket. Excludes every write operation, so it cannot modify anything.
license: Complete terms in LICENSE
---

# Atlassian Readonly Skills

The read-only half of `atlassian-skills`: same client, same configuration, same
response shapes, with every create/update/delete function removed. Prefer it
whenever the task only reads -- it cannot modify anything by accident.

Jira only; Confluence and Bitbucket were removed from this copy.

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
