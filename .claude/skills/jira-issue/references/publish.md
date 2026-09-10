# Publishing: the calls, and what bites

Order: issue, comment, attachments, links. Write description and comment to
files first; never inline multi-line Jira markup into a command.

## Create

```powershell
python -c @'
import sys; sys.path.insert(0, ".claude/skills/atlassian-skills/scripts")
from jira_issues import jira_create_issue
desc = open("desc.txt", encoding="utf-8").read()
print(jira_create_issue("LT", "<summary>", "Bug", description=desc,
    custom_fields={"versions": [{"name": "FW 9.3"}]}))
'@
```

`FW 9.3` above is an example, not a constant -- derive it per `format.md`.

Then `jira_add_comment(key, comment)`, then attachments once Phase 5 is
answered, then one link per related ticket.

## Attachments

```powershell
python -c @'
import sys; sys.path.insert(0, ".claude/skills/atlassian-skills/scripts")
from jira_attachments import jira_add_attachment
print(jira_add_attachment("LT-XXXXX", ["01-before.png", "02-after.png"]))
'@
```

Reference them with `!01-before.png!`, or `!01-before.png|thumbnail!` to keep a
long description scannable. Images belong in the analysis comment unless the
picture *is* the report. Trimming, captioning and provenance labelling are in
`.claude/references/evidence.md`; they are not optional.

## Fields that need the custom_fields back door

`jira_create_issue` and `jira_update_issue` do not expose these, and their
docstrings describe Jira Cloud rather than SIL's Data Center.

| Field | Pass |
| --- | --- |
| Affects Version | `custom_fields={"versions": [{"name": "FW <major>.<minor>"}]}` -- see `format.md` |
| Assignee | `custom_fields={"assignee": {"name": "<jiraUsername>"}}` -- from `.claude/.jira-issue-prefs.json`; a username, **not** an accountId |
| Resolution | **Not settable by update at all.** It is not on the edit screen; only a transition sets it |

Usernames are not email addresses. Read the caller's own with
`client.get(client.api_path("myself"))["name"]`.

## Link types -- read them, never guess

```powershell
python -c @'
import sys; sys.path.insert(0, ".claude/skills/atlassian-skills/scripts")
from jira_links import jira_get_link_types
print(jira_get_link_types())
'@
```

**There is no `Relates` in this Jira.** As of 2026-08-21 the types are
`Cloners, Depends on, Duplicate, Issue split, partially implements, Redesign,
Related, Requires, Solution, Story/Task, Test`.

- Splitting one ticket into several -> **`Issue split`**
- Merely related -> **`Related`**
- Same defect -> **`Duplicate`**

**Never fall back to the first name in the list.** Doing that once produced
four `Cloners` links between a cause ticket and its children, which reads as a
claim nobody made. If the intended type is absent, stop and ask.

Then `jira_create_issue_link(link_type, inward_issue_key, outward_issue_key)`.

## Rewriting an existing ticket

**Post the original as a comment before replacing the description.** Someone
has already read that text and may have replied to it; replacing it outright
destroys the record.

```
h3. Full detail (original description, preserved <date>)

The description above was shortened so a triager can act on the first line.
Nothing was deleted -- the original text follows verbatim.

----
```

Then update the description. Every edit notifies watchers, so a bulk pass is a
mail burst: do it in one sitting and tell the team it is coming.

## Phase 8 -- starting work

1. Branch `LT-XXXXX-short-slug` off fresh `origin/main`.
2. Workspace per the saved preference. Say which one was used.
3. Comment on the ticket naming both, so the ticket indexes the worktree list:

   > Taken. Working on branch `LT-22715-nc-delete-warning`, worktree
   > `<worktree path from the script>`.

4. Hand to `jira-bugfix` at its Step 3. Its Steps 0-2 are already done.

Resolve transitions by ID from `jira_get_transitions`, never by guessing a
name. A permissions failure degrades to "assigned, please move it to In
Progress yourself" rather than aborting. **Never transition to Done or
Resolved** -- that follows a merged PR.
