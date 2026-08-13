# Recommended search and replace contracts

Status: working recommendation requiring owner feedback.

## Compatibility policy

Preserve established behavior unless tests and product evidence identify it
as a defect or an accidental inconsistency. Preservation is not silent: every
supported semantic must be stated, forward-facing, and tested.

A behavior change requires one of these authorities:

- an approved product decision;
- a confirmed data-loss, crash, hang, invalid-range, or Unicode-corruption
  defect;
- an inconsistency whose intended side is established by stronger product
  evidence;
- an approved removal of an unsupported or misleading capability.

## Recommended module boundary

Consolidate the request, result, capability, Unicode, and transformation
infrastructure, but do not force every surface through one semantic engine.
The shared contract should make these items explicit:

- search intent and engine kind;
- supported, ignored, and rejected options;
- normalization and UTF-16 range policy;
- captures and the named replacement dialect;
- writing-system and text-property constraints;
- cancellation, resource limits, and diagnostics.

Keep specialized adapters for ICU collation, ICU regex, indexed discovery,
and phonology. Indexed discovery may reuse normalization and capability
vocabulary but remains candidate discovery rather than final matching.
Phonology may reuse Unicode segmentation and diagnostics but retains typed
grammar input and HermitCrab output. AlloVarGen initially retains an explicitly
named .NET replacement dialect rather than presenting it as ICU-compatible.

The first reusable deep module should surround `VwPattern` and `ITsString`:
range iteration, zero-width progress, capture results, writing-system-aware
slicing and rebuilding, preview/apply transformation, cancellation, and
resource bounds. Find and Replace, Bulk Edit, filters, and concordance can use
that infrastructure while their surface-specific defaults remain explicit.
User-interface option state should be driven by declared engine capabilities,
not scattered checks for regex mode.

## Contracts by user intent

### Exact find, replace, and filter

- Find, Replace, and Bulk Replace match diacritics by default. Filters are a
  candidate for the same default, but their persisted compatibility behavior
  must be characterized and approved before it changes.
- Offer an explicit option to ignore diacritics where the surface can honor it
  correctly.
- Honor pattern writing system, locale, and custom collation when those
  options are part of the surface.
- Treat canonically equivalent text consistently in literal search.
- Preserve valid UTF-16 ranges, text properties, writing-system runs, related
  annotations, and undo behavior.

### Fuzzy discovery

- Find Similar Entries ignores diacritics by default.
- Prefix and full-text indexed discovery may use different ranking and
  matching semantics from exact Find.
- Differences from exact Find must be named and tested rather than inherited
  accidentally from an external index implementation.

### Regular expressions

- ICU regex is the native FieldWorks regex contract.
- Match Case remains supported.
- Diacritic, whole-word, writing-system, style, tag, and collation options are
  unsupported until an explicit design defines their interaction with regex
  syntax, captures, and ranges.
- The user interface must make unsupported combinations clear. Disabled
  controls must not imply that their previous values still affect matching.
- Current normalization behavior remains a compatibility constraint until
  cross-form tests establish its effects. A future normalization change must
  preserve capture and replacement offsets through an explicit mapping.
- Regex execution must have an owned resource policy covering cancellation,
  time or work bounds, and error reporting.

### Replacement

- Replace and Replace All produce the same replacement semantics for the same
  selected match.
- The recommended Replace All contract uses the matches present in the
  original search snapshot. Replacement text created by the operation is not
  searched again during that operation.
- A zero-width match advances to the next Unicode scalar boundary, and a
  wrapped operation visits the original search space at most once. These are
  proposed semantics requiring characterization and owner approval before
  acceptance tests or implementation.
- Replacement never discards unrelated analyses, translations, discourse
  annotations, formatting, styles, writing systems, or object runs.
- Native ICU replacement and .NET AlloVarGen replacement are separate
  contracts until a tested compatibility subset is approved.
- Invalid patterns and replacement expressions produce actionable localized
  errors and never return silently transformed partial data.

### Locale and collation

- Product searches derive locale and tailoring from the relevant writing
  system.
- Tests pin locale and never depend on the host machine.
- ICU remains authoritative for literal collation behavior.
- An optimization must prove equivalence against locale, tailoring,
  punctuation, ignorables, normalization, and range behavior before replacing
  ICU for any input class.

### Unicode ranges

- User-visible literal search treats canonical equivalents consistently.
- Returned positions are UTF-16 offsets because that is the current API
  contract.
- A returned range never splits a surrogate pair.
- Combining marks, extenders, joiners, and ignorables at search limits have
  explicitly characterized range behavior.

## Decisions requiring feedback

1. Whether filters should continue restoring a missing diacritic setting as
   false despite the exact-search default of true.
2. Whether regex should eventually offer canonical-equivalence or diacritic
   options, and how those options affect regex character classes and captures.
3. Which work or time bound applies to ICU and .NET regex execution.
4. Whether AlloVarGen should adopt ICU replacement, retain .NET replacement,
   or document a common subset.
5. Which indexed-search differences are intentional for each Find or Go
   surface.
6. Which component owns the final cross-surface contract and approves future
   semantic changes.

The final approved contract will be published as
`Docs/architecture/search-and-replace.md`. This working recommendation does not
become normative merely because its research workflow was approved.
