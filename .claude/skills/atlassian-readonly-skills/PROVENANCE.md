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
2. **`scripts/*.py` import fix.** Eight modules raised `NameError` on import:
   `Optional` or `AtlassianCredentials` was missing from their import lines
   while the surviving signatures still referenced them. Affected:
   `confluence_comments`, `confluence_labels`, `confluence_pages`,
   `jira_agile`, `jira_links`, `jira_projects`, `jira_workflow`,
   `jira_worklog`.

   A ninth defect was runtime-only and easy to miss: `jira_projects` raised
   `NotFoundError` at `:179` without importing it, so the module imported
   cleanly and broke only when a project was absent. **A successful import is
   not evidence a module works** -- `python -m pyflakes <files>` finds this
   class of bug, and a shell that does not expand globs will silently scan
   nothing, so pass real paths.

   **This was upstream's bug, and upstream had already fixed it before we
   vendored.** It arrived in upstream `cdd1823f6` (2025-12-20) and was fixed in
   upstream `0fafb48e7`, "Fix missing imports in readonly variant scripts"
   (2026-02-10). Our files are byte-identical to upstream at `cdd1823f6`, and
   `d1a9bc66d` vendored that pre-fix state on 2026-06-11 -- four months after
   the fix existed. So this was a stale snapshot, not an inherited live defect,
   and a re-sync would have *fixed* it rather than reintroducing it. Upstream
   issue #14 was filed here against the stale copy and has been closed as
   already-fixed.

   Still true upstream today: the four `bitbucket_*` modules use
   `from ._common import`, a relative import that cannot resolve under the
   invocation this `SKILL.md` documents. Moot here, since those modules are
   removed -- see "Jira only" below.

3. **TLS verification is off.** `_common.py` sets `ssl_verify = False` on the
   credentials dataclass (`:128`), on the config (`:192`), and as the
   `*_SSL_VERIFY` environment default (`:236`), where upstream sets `True`.
   Deliberate: `jira.sil.org`'s certificate chain does not validate here, so
   every call would fail with verification on. The cost is an
   `InsecureRequestWarning` per request. Do not align this with upstream
   without first confirming the chain validates.

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

## Jira only

Confluence and Bitbucket were removed entirely on 2026-08-21. FieldWorks uses
Jira and nothing else, and nothing in the repository referenced either.

Removed: the eight `confluence_*` and `bitbucket_*` script modules, their
documentation in `SKILL.md` and `REFERENCE.md`, their configuration blocks and
credential fields, and their plumbing in `_common.py` -- the dataclass fields,
`is_*_available` checks, `get_*_client` factories and the service branches in
`AtlassianConfig.from_credentials`.

This is a hard fork from upstream for these two skills. A re-sync is no longer
a merge; treat upstream as a source to cherry-pick Jira fixes from.
