# Search and replace correctness design

Status: research workflow and test-first execution design approved. Product
recommendations remain subject to product and domain-owner feedback.

## Goal

Make FieldWorks search, regular expression, and replacement behavior explicit,
well characterized, and safe across literal Find, Find and Replace, Bulk Edit,
filters, concordance, indexed discovery, AlloVarGen, and the adjacent
phonological Unicode boundary.

## Governing principle

Preserve established behavior unless tests and product evidence identify it
as a defect or an accidental inconsistency. Every preserved semantic must be
stated explicitly, forward-facing, and tested. Historical behavior is evidence
but is not permanent product authority.

## Working artifacts

The working package under `Docs/architecture/search-and-replace/` separates:

- current capability and ownership boundaries;
- history from Git, GitHub pull requests, and Jira;
- recommended forward-facing contracts;
- characterization and acceptance-test strategy;
- the separate phonological grammar boundary.

These documents are a reviewable notebook. Before the final pull request, the
accepted contract will be consolidated into one concise architecture document.
Historical archaeology and detailed test planning will remain only if
reviewers request them.

## Contract direction

Search intent controls defaults. Exact Find, Replace, and Bulk Replace match
diacritics by default. Filters are recommended to follow that rule, but their
persisted false default is a compatibility constraint requiring
characterization and owner approval before change. Fuzzy discovery, including
Find Similar Entries, ignores diacritics by default. Each approved surface sets
its default explicitly.

ICU remains authoritative for native literal search. Product searches derive
locale and tailoring from the relevant writing system, while tests pin locale.
User-facing literal search treats canonically equivalent text consistently and
returns valid UTF-16 ranges without splitting surrogate pairs.

Regex is a distinct contract. Initially preserve its supported option set and
make unsupported combinations clear. Match Case remains supported;
diacritics, whole word, writing system, style, tags, and collation require a
separate approved design before they affect regex matching. Normalization and
resource behavior must be characterized before correction.

Replace and Replace All preserve unrelated annotations, translations,
formatting, writing-system runs, object runs, undo behavior, and deterministic
iteration. Native ICU replacement and .NET AlloVarGen replacement remain
separate contracts until parity or a common subset is approved.

The recommended Replace All model operates on matches from the original search
snapshot and does not reprocess matches created by replacement text. Proposed
zero-width advancement and wrap semantics require characterization and owner
approval before acceptance tests.

Indexed and fuzzy search may differ from exact Find, but each difference must
be deliberate, named, and tested. Phonological environments remain a separate
domain grammar with shared Unicode and malformed-input concerns.

The architectural direction is a shared request, result, capability, Unicode,
and transformation layer over specialized engines. ICU collation, ICU regex,
indexed discovery, .NET replacement, and phonology must not be collapsed into
an interface that implies semantic interchangeability. Reuse normalization
policy, UTF-16 ranges, captures, writing-system-aware rebuilding, progress
guards, cancellation, resource limits, diagnostics, and preview/apply
orchestration. Let declared capabilities drive user-interface option state.

## Test architecture

Shared input and expected-result records drive small surface-specific adapters.
Native `VwPattern` tests establish engine semantics. Managed matcher tests
establish option and error wiring. Consumer tests establish Find and Replace,
Bulk Edit, filters, concordance, indexed discovery, and AlloVarGen behavior.
Focused integration tests establish preservation of formatted strings,
writing systems, related data, preview and apply parity, and undo boundaries.

Characterization tests record current behavior without approving it.
Acceptance tests state an approved change and must fail for the expected reason
before production code changes. Data loss, crashes, hangs, invalid ranges, and
Unicode corruption are presumptive defects; semantic differences remain
decision items until approved.

## Execution and review

1. Commit the working evidence package without product-code changes.
2. Verify claims against source, Git, GitHub pull requests, and Jira.
3. Commit a test-first implementation plan.
4. Add characterization tests without changing product semantics.
5. Add separate phonology characterization at its grammar and Unicode boundary.
6. Solicit product and domain-owner feedback on recommended contracts.
7. Revise and commit the accepted recommendations.
8. Create one umbrella Jira issue linking stable documents and stating the
   forward-facing compatibility policy.
9. Add separate failing acceptance tests for approved corrections.
10. Implement corrections in small semantic slices.
11. Consolidate the final approved contract into
    `Docs/architecture/search-and-replace.md`.

## Finding classes

Every finding is classified as one of:

- confirmed contract;
- compatibility constraint;
- known defect;
- intentional surface-specific difference;
- decision requiring feedback;
- hypothesis requiring a test;
- historical behavior with no present authority.

No characterization failure automatically authorizes a product change.

The consolidated contract becomes normative only after owner approval and
merge. Approval of this design authorizes research, planning, and
characterization tests, not unresolved product semantics.
