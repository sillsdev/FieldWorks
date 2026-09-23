# Inline text editing in converted reference rows

Status: proposed as current principle; pending human review
Sources: PR for LT-22672 (Allomorphs Environments row); `PhoneEnvReferenceView`
and `PhoneEnvReferenceSlice` as the characterized source
Human review: pending

## Question tested

Can a converted reference-vector row gain inline typed editing -- so an item
already on the field can be changed, not only added and removed -- while
matching a source view whose editing semantics are reference reconciliation
rather than text entry?

## Observations

- The row's editing semantics were not text semantics. An item's identity was
  its text with literal spaces removed, so a purely cosmetic re-spacing renamed
  a shared object across the whole project, while any other change re-pointed
  the reference and created a target only when none existed.
- The full behaviour was **not** recoverable from the source view's commit
  method. One of six user-visible outcomes -- emptying an item to remove it --
  lived in a helper that decided which lines the commit would consider at all.
  Reading the commit path alone produced a five-case model that looked complete
  and was not.
- An input guard rejecting blank text made the conversion look merely
  incomplete. It was also masking a second defect: with the guard removed, the
  unguarded path created an empty domain object for the item to point at. The
  guard had made a wrong answer unreachable rather than correct.
- The source view carried no tests for any of this. Its single test covered an
  adjacent edge case, and was the only written evidence that blank lines were
  treated differently from populated ones.
- Two rendering defects were indistinguishable from one another by report. A
  row that arranged items past its own width, and an editor whose measured
  width fell short of the text it drew, both presented as "the end of the value
  is cut off".
- Headless rendering could not reproduce the measurement defect. The headless
  text shaper uses uniform advances, so measurement and rendering agree there
  by construction and the defect cannot arise.
- Repeated small changes each produced "no visible difference" in the running
  application. That observation was consistent with a wrong diagnosis, with a
  correct diagnosis and an insufficient change, and with the build not reaching
  the application at all. A single deliberately extreme change separated all
  three at once.

## What failed or was retired

An earlier attempt replaced the row with a bespoke control. It was reverted
because it lost the chooser, the item menu, reordering, and per-item
validation, all of which the row already provided. The retained approach
changed only how a row renders its items, leaving the row's identity intact.

Three diagnoses of the rendering defects were tried and discarded: reducing
each item's width by removing editor chrome, widening padding to leave room for
a caret, and treating the row's overflow as the whole cause. The first two were
wrong; the third was a real and separate defect that did not explain the report.

## Durable lessons

1. Characterize a source view by enumerating the user's gestures against it,
   not by reading the method that writes changes back. A gesture's handling may
   live in a helper that filters what the write-back ever sees.
2. Before adding a guard that rejects an input, establish what the guarded path
   would do with it. A guard that makes a wrong answer unreachable conceals the
   defect instead of fixing it, and hides it from tests as well.
3. Treat blank input as a possible gesture rather than an absent value. In this
   area emptying an item is how a user removes it.
4. Where a behaviour depends on text metrics or on arrangement against a real
   surface, headless tests cannot reproduce it. Pin the invariant that must
   hold rather than the mechanism, and require a pass in the running
   application.
5. When a change yields no visible difference, force an unmistakable variant of
   it before reasoning further. "No change" cannot distinguish a wrong
   diagnosis from a change too small to see or from a build that never arrived.

## Evidence needed next time

- The complete gesture list for the row -- add, remove, retype, clear, reorder,
  menu, and the keyboard equivalents -- each exercised against the source view
  before any of it is designed.
- The source view's helper methods, not only its commit method, read for
  gestures that never reach the write-back.
- For every behavioural test, a suppression run showing that it, and only it,
  fails.
- A manual pass in the running application for anything touching text metrics,
  wrapping, or available width, with the result recorded rather than assumed.
- The behaviour of a shared domain object when one reference to it is edited or
  removed, asserted from a *second* holder of that reference. A single-holder
  fixture cannot show project-wide reach.

## Decision boundary

This record constrains how a reference-vector row's editing behaviour is
discovered and evidenced. It does not decide which rows should become editable,
what a row should do when a single item exceeds the whole row's width, or
whether matching a surprising source behaviour is preferable to correcting it.
Those remain domain-owner decisions, taken per row.

## Do not infer

- That reproducing a source view's surprising behaviour is generally correct.
  It was chosen here because divergence would have made two views disagree
  about shared data, and the choice was recorded rather than assumed.
- That every reference-vector row should render editable items. The capability
  is asked per field and only one row answers to it.
- That an unconditional write-back on commit is a design to copy. It is matched
  parity with a specific source view.
- That the rendering fixes settle row layout generally. Wrapping happens
  between items, not within one.
- That the tests here cover the rendering defects themselves. They cover the
  invariants those defects violated.
