# In-Depth Review

This note captures the deeper review pass for `add-opentype-font-features` and
turns that review into clarified OpenSpec scope. It is a planning artifact only;
it does not describe completed implementation work.

## Purpose

- record the clarified scope after the PR/spec/implementation review
- separate merge-blocking concerns from acceptable follow-up research
- map review findings into proposal, design, capability specs, and tasks

## Clarification Pass

- The earlier local native `MM` churn finding is intentionally not part of this
  artifact. The user confirmed another agent completed that work, so this note
  treats churn reconciliation as a validation/process check rather than feature
  scope.
- Phase 1 remains current WinForms plus current Views/Uniscribe, with Graphite
  preserved and HarfBuzz/Skia kept test-only.
- OpenType is now the intended default provider for dual-technology fonts.
- Valid OpenType tag acceptance is syntactic: any four-character printable ASCII
  tag is acceptable even if it is custom/private.
- Filtering applies to what the UI exposes, not to what storage accepts.
- Logging, graceful fallback, and malformed-input safety are Phase 1
  deliverables.

## Analysis Pass

### Overall Verdict

The core direction remains correct:

- keep renderer-neutral `tag=value` storage
- apply OpenType in the existing production renderer path
- preserve Graphite behavior during Phase 1
- use HarfBuzz/Skia only for comparison evidence

The review identified seven planning gaps that needed to become explicit scope.

### Verified Planning Gaps

1. **Raw feature discovery is too broad.**
   OpenType UI discovery can expose required or engine-controlled shaping
   features if it simply dumps GSUB/GPOS tags. The change now needs an explicit
   filtering rule plus tests.

2. **Dual-technology fonts were still Graphite-first.**
   Shared font-feature surfaces did not consistently carry provider context, and
   the control still defaulted toward Graphite assumptions. The change now needs
   OpenType-first behavior and a clear explicit provider toggle.

3. **Graceful fallback existed, but diagnostics were weak.**
   The feature should keep going on malformed tags, unsupported features, or bad
   fonts, but that behavior must be observable in trace logs.

4. **Legacy truncation logic had a hang risk.**
   Overlong feature strings without a comma boundary could leave old truncation
   logic without a safe forward-progress guarantee. The change now needs a
   fail-safe truncation task and tests.

5. **Tag acceptance and export safety were misaligned.**
   The right contract is to accept any valid OpenType tag while ensuring CSS and
   other outputs serialize those valid tags safely. The change now needs liberal
   validation plus safe emitters.

6. **A duplicate inheritance path was present.**
   Default font-feature loading existed in more than one place. The change now
   needs to make the existing inheritance path authoritative.

7. **State-of-the-art follow-ups were missing from scope.**
   Retryable `E_OUTOFMEMORY`, authoritative script/language inputs, CSS-safe tag
   serialization, Notebook export coverage, and explicit fallback diagnostics now
   belong in the plan.

## Planning Pass

### Required Implementation Workstreams

1. **Input validation and normalization**
   Accept valid tags, trace malformed tags, and make legacy truncation safe.

2. **Feature discovery and UI filtering**
   Expose optional user-configurable features only; keep required shaping
   features under engine control.

3. **OpenType-first shared UI behavior**
   Make OpenType the default provider for dual-technology fonts and add an
   explicit provider toggle.

4. **Native shaping robustness and diagnostics**
   Retry retryable sizing failures, preserve authoritative script/language
   inputs, and trace fallback reasons.

5. **Inheritance-path cleanup**
   Remove duplicate style/default loaders and rely on the authoritative existing
   path.

6. **Export and serializer safety**
   Keep storage liberal while ensuring CSS and DOCX outputs remain safe and
   documented.

7. **Layered test coverage**
   Add targeted tests for filtering, toggles, malformed input, truncation,
   fallback, exports, and cache invalidation.

### Acceptance Signals

- required shaping features are not shown as user toggles
- dual-technology fonts default to OpenType in shared UI surfaces
- malformed tags are logged and ignored without blocking valid entries
- overlong/no-comma feature strings do not hang or crash
- native fallback reasons and retry decisions are traceable
- default/inherited font features round-trip through one authoritative path
- CSS and DOCX export behavior is safe for accepted tags and documented subsets
- added tests cover the review-driven risk areas

### Artifact Mapping

- `proposal.md`: widened scope and impact
- `design.md`: clarified decisions for OpenType preference, filtering,
  validation, logging, inheritance, truncation, and state-of-the-art native
  behavior
- `research.md`: external references and review addendum
- `tasks.md`: new unchecked review-driven backlog sections
- capability specs: behavior requirements for provider choice, validation,
  fallback, and test coverage

## Not Planned As Separate Scope

- reopening the whole Phase 1 architecture around a production HarfBuzz renderer
- removing Graphite in Phase 1
- treating local review churn as a product requirement instead of a validation
  prerequisite