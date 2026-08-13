# Bulk replacement optimization evidence

## Retained scope

The focused implementation retains four changes:

- Avoid normalization work when a replacement result is already NFD.
- Compute each preview value once and reuse it for the enabled result.
- Expose one-session rich-string replacement through the optional `IVwPattern2`
  capability without changing the `IVwPattern` ABI or coclass default.
- Use the capability when available and preserve the repeated-`FindIn` fallback.

The correctness corpus covers literal and regular-expression replacement,
captures, zero-width matches, whole-word and case/diacritic options, Unicode
normalization and scripts, ICU tailoring, rich-text properties, object
replacement characters, capability fallback, failures, and terminal state.

## Single-pass preview measurement

This is an incremental comparison of the single-pass preview change, not a
branch-point-to-final result. Both builds used Debug x64, 50,000 real entries,
100 percent matching CitationForm values with one match each, one warmup, and
five timed preview repetitions per process. Process order was B-C-B-C.

- Baseline process 1: 73.973706, 87.429190, 85.850784, 89.671968,
  93.842072 microseconds/entry; median 87.429190.
- Candidate process 1: 53.317292, 49.020970, 49.454658, 47.650816,
  54.720194; median 49.454658.
- Baseline process 2: 68.099794, 68.588244, 70.913828, 70.782702,
  69.719412; median 69.719412.
- Candidate process 2: 48.884480, 49.327828, 58.699344, 78.705420,
  90.564270; median 58.699344.
- Pooled medians: baseline 72.443767, candidate 51.385975
  microseconds/entry; 29.068 percent reduction and 1.409796x throughput.

The second candidate process was retried unchanged after a transient test
discovery failure produced no timing result.

## ReplaceAllIn measurement

This is an incremental comparison of `ReplaceAllIn`, not a cumulative branch
result. Both trees used `9307443e494d23066d9d40bc41c4d4b30944b972`.
The baseline changed only the managed capability assignment from the
`IVwPattern2` cast to `null`; native and search code was identical. Both used
`build.ps1 -BuildTests -Serial`, Debug x64, and the same temporary harness
(SHA-256 `52AB194E89486BFB94722A444B077B894B84128B46F8F0CF7EDD62DAE48D5481`).

Each fresh process used 50,000 real entries. Matching values were
`old old old old-######` (22 UTF-16 units and four matches); unmatched values
were `keep keep keep-######` (21 units). Each fresh process had one warmup and
five timed apply repetitions. Reset, garbage collection, validation, and undo
were outside the stopwatch. There was no cache prepass. All comparisons
reported zero mismatches.

### 25 percent matching

- Baseline: 109.070736, 108.331108, 108.476428, 109.597136, 109.750080;
  median 109.070736 microseconds/entry.
- Candidate: 80.400070, 86.023792, 81.024132, 83.734014, 100.501314;
  median 83.734014.
- Incremental result: 23.23 percent reduction and 1.303x throughput.

### 50 percent matching

- Baseline process 1: 235.016240, 278.735516, 215.068208, 291.738206,
  245.199594; median 245.199594.
- Candidate process 1: 160.970294, 161.559868, 161.132232, 161.602208,
  178.361492; median 161.559868.
- Baseline process 2: 220.388066, 222.015458, 215.116946, 233.213528,
  239.261800; median 222.015458.
- Candidate process 2: 172.943354, 173.446744, 170.288498, 194.350902,
  182.141472; median 173.446744.
- Pooled medians: baseline 234.114884, candidate 171.615926; 26.69 percent
  reduction and 1.364x throughput. The two paired reductions were 34.11 and
  21.88 percent.

### 100 percent matching

- Baseline process 1: 468.296628, 437.760106, 437.642286, 586.596700,
  518.897366; median 468.296628.
- Candidate process 1: 316.618992, 314.000950, 337.447102, 317.614622,
  315.852194; median 316.618992.
- Baseline process 2: 421.689880, 425.018894, 428.718602, 444.715500,
  429.318474; median 428.718602.
- Candidate process 2: 325.698984, 335.498302, 325.648910, 364.891896,
  362.333156; median 335.498302.
- Pooled medians: baseline 448.507615, candidate 326.058643; 27.30 percent
  reduction and 1.376x throughput. The two paired reductions were 32.39 and
  21.75 percent.

Debug-process variance was material, especially in the first baseline
process. The raw sample bands stayed separated, and each independent 50 and
100 percent pair exceeded the 15 percent acceptance gate. These measurements
cover the tested short CitationForm values only.

## Rejected and deferred work

- A printable-ASCII `memcmp` shortcut was reverted because ICU collation can
  equate strings that differ in punctuation and other non-ordinal ways.
- A managed ordinal negative precheck was exactly reverted because it did not
  produce sufficient benefit.
- A one-character collation-ignorable cache and early extension exit were
  reverted. Its answer survived a locale change and produced an incorrect
  match span; the single-character premise also lacked an ICU contract for
  contextual collation.
- The virtual table and budgeted coordinator were test-owned prototypes with
  no shipping activation path. They and their acceptance and attribution
  harnesses were discarded.
- Homograph renumber batching was deferred after characterization only. It
  needs separate cross-repository design and measurements for undo, redo,
  notifications, cache membership, and group numbering.
