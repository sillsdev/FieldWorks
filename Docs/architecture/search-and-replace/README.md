# Search and replace correctness working package

Status: working evidence, not a normative product contract.

Tracking issue: [LT-22696](https://jira.sil.org/browse/LT-22696). The issue
summarizes current behavior and owns the product and architecture decisions
that must be made before this package becomes a normative contract.

This package records the evidence and recommendations needed to make
FieldWorks search and replacement behavior explicit. It separates current
architecture, historical intent, recommended contracts, and test planning so
reviewers can challenge each independently.

The governing compatibility rule is:

> Preserve established behavior unless tests and product evidence identify it
> as a defect or an accidental inconsistency. Every preserved behavior must be
> stated explicitly, forward-facing, and covered by tests.

Historical behavior is evidence, not permanent authority. A passing
characterization test records what FieldWorks does today; it does not by itself
decide what FieldWorks should do.

## Working documents

- [Capability inventory](capability-inventory.md)
- [Historical evidence](historical-evidence.md)
- [Recommended contracts](recommended-contracts.md)
- [Test strategy](test-strategy.md)
- [Phonology boundary](phonology-boundary.md)
- [ICU 70 to 78 upgrade research](icu-70-to-78-research.md)
- [Approved research and execution design](../../superpowers/specs/2026-08-13-search-and-replace-correctness-design.md)

## Review lifecycle

1. Verify every claim against source, Git history, pull requests, or Jira.
2. Characterize current behavior without changing product semantics.
3. Ask product and domain owners to review the recommendations.
4. Record approved decisions and separate acceptance tests from
   characterization tests.
5. Consolidate the accepted forward-facing contract into one concise document
   for the final pull request.

The final artifact will be `Docs/architecture/search-and-replace.md`. It
becomes normative only after product and domain-owner approval and merge. The
working evidence files may be removed from the final tree after consolidation;
until then, they remain evidence and recommendations rather than authority.
