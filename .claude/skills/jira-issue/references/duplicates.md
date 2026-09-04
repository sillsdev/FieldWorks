# Duplicate and related search (Phase 2)

**Search before drafting, not before posting.** If the ticket exists, the work
is a comment on it, and finding that out after twenty minutes wastes the
twenty minutes.

```powershell
python -c @'
import sys; sys.path.insert(0, ".claude/skills/atlassian-skills/scripts")
from jira_search import jira_search
jql = "project = LT AND text ~ \"natural class\" AND status != Closed ORDER BY updated DESC"
print(jira_search(jql, fields="key,summary,status,updated", limit=10))
'@
```

Four passes, ten results each:

1. **Symptom words** -- the reporter's vocabulary, not ours.
2. **Area** -- `project = LT AND component = "..." AND status != Closed`.
3. **Link walk** -- for every ticket already cited, read its links, follow one hop.
4. **Mechanism** -- the type or method name, when the code location is known.

`text ~` searches summary, description, comments and environment, so a common
word like "triage" returns hundreds of irrelevant hits. Say so when reporting a
noisy pass rather than listing its results.

Present at most five candidates:

| Key | Summary | Why it might be the same | Verdict |
| --- | --- | --- | --- |

Verdicts are duplicate, related or unrelated. **Never file without showing this
table and getting a yes.** If a duplicate exists, offer to comment on it
instead; if the developer still wants a new ticket, file it and link it.

Link types are in `publish.md` -- read them, never guess.
