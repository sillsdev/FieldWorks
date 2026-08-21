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

   **Reported upstream as langpingxue/atlassian-skills#14.** Until it is fixed
   there, a re-sync will reintroduce it.

## Restructuring done here

`SKILL.md` dropped its `## Available Utilities` section -- 237 lines restating
all 48 function signatures that `REFERENCE.md` already documents in the same
folder, copied from the write variant. It is now a module-to-function index
generated from the scripts, with `REFERENCE.md` carrying the signatures. 560
lines to 265.

Only three sections proved byte-identical to the write variant and were safe to
document once there: Response Data Structures, Error Handling, Dependencies.
Configuration, Core Workflow and Philosophy differ -- the write variant's are
supersets carrying write examples -- so the first two are kept here verbatim.

This is the largest local divergence from upstream. A re-sync has to re-apply
it, along with the import fix and the SIL section.
