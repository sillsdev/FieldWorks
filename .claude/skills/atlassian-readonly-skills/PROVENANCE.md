# Provenance

Vendored from **https://github.com/langpingxue/atlassian-skills**
(`atlassian-readonly-skills/`), first committed here in d1a9bc66d.

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
2. **`scripts/*.py` import fix.** Eight modules were unusable: they raised
   `NameError: name 'Optional' is not defined` or
   `NameError: name 'AtlassianCredentials' is not defined` on import, because
   the read-only variant appears to be generated from the write variant by
   stripping write functions, and the stripper also pruned those two names
   from the import lines while the surviving signatures still referenced them.

   Affected: `confluence_comments`, `confluence_labels`, `confluence_pages`,
   `jira_agile`, `jira_links`, `jira_projects`, `jira_workflow`,
   `jira_worklog`. The fix restores the pruned names, nothing else.

   **This is an upstream bug and should be reported there.** Until it is fixed
   upstream, a re-sync will reintroduce it.

## Deliberately not done

This skill has **not** been compressed or restructured, unlike the
FieldWorks-owned skills. It is ~560 lines of API reference that loads only
when Atlassian work happens, and keeping it close to upstream is worth more
than the context saving. The near-total duplication between this variant and
`atlassian-skills` is upstream's design, not something introduced here.
