# Search and replace historical evidence

Status: working evidence, not a normative product contract.

This document distinguishes explicit product decisions from implementation
history. A merged change proves that behavior shipped; it does not prove that
the behavior remains desirable.

## Evidence classes

- Explicit decision: the issue, review, commit, or test states the desired
  behavior.
- Inferred intent: behavior is asserted without recorded rationale.
- Workaround: the source identifies a constrained response to a defect.
- Superseded: later evidence intentionally replaces the earlier rule.
- Unresolved: the evidence does not determine the desired contract.

## Key decisions and incidents

| Evidence | Classification | Present implication |
| --- | --- | --- |
| Jira [LT-8191](https://jira.sil.org/browse/LT-8191) and commits `c844c74d8`, `274c712e3` | Explicit decision | Find and Replace and filters match diacritics by default because users expect typed distinctions to matter. Find Similar Entries intentionally remains fuzzy. |
| Jira [LT-7412](https://jira.sil.org/browse/LT-7412), [LT-5058](https://jira.sil.org/browse/LT-5058), and [LT-5200](https://jira.sil.org/browse/LT-5200) | Explicit defect evidence | Ignoring diacritics during Bulk Replace can change the wrong entries and remove meaningful marks. |
| Jira [LT-4318](https://jira.sil.org/browse/LT-4318) | Explicit surface-specific intent | Find Similar Entries should find canonically related forms with or without optional diacritics. |
| Jira [LT-13579](https://jira.sil.org/browse/LT-13579) and commit `c235f5380` | Explicit decision | Filters and searches must honor the writing system's standard or custom ICU collation. Host locale is not a valid substitute. |
| Commit `2ea7026a7` | Explicit test policy | Collation tests must set a locale because the host locale is nondeterministic. |
| Jira [LT-9129](https://jira.sil.org/browse/LT-9129), [LT-16331](https://jira.sil.org/browse/LT-16331), and commit `bf736c128` | Explicit safety decision and workaround | Replace All must not discard analyses, translations, discourse data, or unrelated paragraph content. Repeated Replace Once behavior was chosen to avoid the destructive path. |
| Jira [LT-16537](https://jira.sil.org/browse/LT-16537) | Explicit defect evidence | Growing replacement text must not make search wrapping stop before every original occurrence is processed. |
| Jira [LT-20729](https://jira.sil.org/browse/LT-20729) and commit `7d61e72fe` | Explicit correctness decision | Bulk Replace and native matching must not split supplementary-plane UTF-16 pairs or crash on upper-plane text. |
| Jira [LT-21845](https://jira.sil.org/browse/LT-21845), commit `2ddc69be3`, and [PR 682](https://github.com/sillsdev/FieldWorks/pull/682) | Explicit user-visible decision | Match Writing System and replacement writing-system selection must be honored on the first operation. |
| Jira [LT-18927](https://jira.sil.org/browse/LT-18927), commit `cacbd3842`, and [PR 625](https://github.com/sillsdev/FieldWorks/pull/625) | Explicit data invariant | Imported lookup data is converted to NFD because internal list data is NFD. This does not by itself decide every search normalization contract. |
| Commits `a5a380e85`, `d5e73f6fe`, `9dc07d8d8`, `60ecfe2d6` on `table-speedup` | Characterization and superseded experiment | Preserve the characterization matrix. ICU remains authoritative after an ASCII shortcut was shown to risk collation behavior. Tests pin locale to `root`. |
| Jira [LT-18767](https://jira.sil.org/browse/LT-18767) and [PR 381](https://github.com/sillsdev/FieldWorks/pull/381) | Explicit parser robustness decision | Invalid indexed reduplication references are reported without a yellow-box crash. |
| Jira [LT-21585](https://jira.sil.org/browse/LT-21585), [PR 161](https://github.com/sillsdev/FieldWorks/pull/161), Jira [LT-22353](https://jira.sil.org/browse/LT-22353), and [PR 646](https://github.com/sillsdev/FieldWorks/pull/646) | Superseded phonology restriction | A crash-prevention restriction on multi-item rewrite rules was later removed when HermitCrab added merge and split support. |
| Commit `2552cc590` / Jira [LT-19489](https://jira.sil.org/browse/LT-19489) | Implementation history, not search intent | The Phonemes tool and IPA-symbol editor were repaired in 2019, but the change does not define normal or regex filter semantics. |
| [`MorphologyParts.xml`](../../../DistFiles/Language%20Explorer/Configuration/Parts/MorphologyParts.xml#L2719) comment for Jira [LT-22171](https://jira.sil.org/browse/LT-22171) | Configuration workaround | The Natural Classes Phonemes/Features column is kept visible through a class/layout workaround. This raises configured-cell extraction risk but does not itself prove the reported filter failure. |
| Commit `7f52d9cdb` | Dependency migration | FieldWorks moved from checked-in ICU 54 headers to SIL ICU 70 native packages in 2022. Later version-centralization work retained ICU 70; no later product decision requiring that major was found. |

## Evidence warning

Commit `71716c207` says `LT-17356` and describes ignored `VwPattern` tests in
2012. Jira currently identifies LT-17356 as an unrelated 2016 Reversal
configuration issue. The commit content remains valid source history, but the
ticket link is unreliable and must not be cited as product authority.

## Pull request review result

PRs 161, 381, 625, 646, and 682 were merged and approved. Their descriptions
confirm the associated Jira correction but contain little additional product
rationale. Jira acceptance text and the code changes carry the stronger
behavioral evidence.

## Unresolved historical questions

- The original rationale for NFD-normalizing regex patterns but not explicitly
  normalizing regex source text is not present in available history.
- No historical decision defines regex time, stack, or cancellation limits.
- No evidence establishes parity between ICU matching and .NET AlloVarGen
  replacement.
- The rationale for disabling whole-word, writing-system, and diacritic
  options in regex mode is not recorded beyond implementation comments.
- No single historical rule covers indexed discovery, exact find, filtering,
  concordance, and fuzzy entry discovery. Jira evidence instead supports
  intent-specific contracts.
- No historical evidence defines what text generated phonological feature
  columns must expose to normal or regex table filtering. That contract must be
  established with configured-table tests and owner feedback.
