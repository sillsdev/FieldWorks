# Provenance

Vendored from **https://github.com/langpingxue/atlassian-skills**
(`atlassian-skills/`), first committed here in d1a9bc66d.

## License

Upstream's README declares **MIT License**. Note two gaps in that declaration,
recorded here so nobody has to rediscover them:

- **Upstream ships no `LICENSE` file.** GitHub's license endpoint returns 404
  for the repository, and there is no copyright line anywhere in it.
- `SKILL.md`'s frontmatter says `license: Complete terms in LICENSE`, which is
  therefore a dangling reference upstream as well as here. It is left unchanged
  so this copy stays diffable against upstream.

MIT permits use and modification; the attribution it asks for is this file.

## Local modifications

Keep these few and listed, so re-syncing upstream stays possible.

1. **`SKILL.md`** gains a leading `## FieldWorks / SIL JIRA Integration`
   section (~39 lines) covering `jira.sil.org`, the `LT` project key and the
   Data Center specifics. Everything below it matches upstream.

No script changes. The import bug that made eight modules unusable affects only
the read-only variant -- see its `PROVENANCE.md`.

## Data Center gotchas worth knowing before reading the docs

Upstream's docstrings describe Jira Cloud. SIL's instance is Data Center, so:

- `assignee` wants a username, **not** an `accountId`.
  `jira_create_issue`/`jira_update_issue` send `{"accountId": ...}` and are
  rejected. Pass `custom_fields={"assignee": {"name": "<username>"}}`.
- Affects Version is not exposed at all. Pass
  `custom_fields={"versions": [{"name": "FW 9.3"}]}`.
- `resolution` cannot be set by an update -- it is not on the edit screen.
  Only a transition sets it.

These and the link-type list live in
`.claude/skills/jira-issue/references/publish.md`, which is the place to look
first for LT tickets.

## Deliberately not done

This skill has **not** been compressed or restructured, unlike the
FieldWorks-owned skills. It is ~740 lines of API reference that loads only when
Atlassian work happens, and keeping it close to upstream is worth more than the
context saving. The near-total duplication with `atlassian-readonly-skills` is
upstream's design, not something introduced here.
