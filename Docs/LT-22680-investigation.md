# LT-22680 investigation

## Reported behavior

The reporter said that changing a morpheme to the correct lexeme form for
`akakwaatya` in the text `V: extensions` caused FLEx to freeze and close after
about a minute.

The supplied backup was created on 2026-08-10 by FieldWorks 9.3. Its copy of
`V: extensions` contains `akakwaatera`, analyzed as `a+ka+kwaat+er+a`; the exact
form `akakwaatya` is not present in the backup. This may mean that the backup
does not preserve the state needed to trigger the failure or that the reported
form was approximate.

## Reproduction attempts

The project was restored from a fresh copy of the supplied backup for each
version tested. Editing the morpheme in Texts & Words did not reproduce a
freeze or crash in the current development build.

The same project was then opened with a locally built FieldWorks 9.3.10
(`9.3.10.46247`, commit `98886863d`). The full `build.ps1` build completed with
no warnings or errors. Manual testing also did not reproduce the failure.

No customer backup or extracted project data is committed with this note.

## Parsing paths considered

Try a Word and Texts & Words ultimately use the same parser infrastructure,
but they do not exercise an identical UI and data-update path. Try a Word uses
a transient sandbox to request and display parser results. Text analysis edits
persistent wordform analyses and applies morpheme, gloss, and category changes
through the interlinear sandbox. A successful Try a Word parse therefore does
not by itself cover the operation described in the report.

The two paths contain some similar sandbox orchestration, but consolidating
their interfaces would be a broad architectural change with little direct
evidence that it would prevent this failure. That refactoring was deliberately
deferred so that any LT-22680 change can remain narrow and behavior-driven.

## Relevant subsequent change

Current development includes commit `14bf8f8a6` (LT-22616, "Word Cat contents
change when bundle selected"). It changes the interlinear sandbox's pending
morpheme update, gloss, and category handling. This overlaps the reported user
workflow and could have affected the symptom, but it does not establish that
LT-22680 was fixed: the problem also failed to reproduce on 9.3.10, which does
not contain that commit.

## Conclusion and next evidence needed

There is not enough evidence to make or validate a code fix. Further work
needs one of the following:

- exact steps and selections starting from a fresh restore of the supplied
  backup;
- a backup that still contains the reported `akakwaatya` state;
- a FieldWorks crash report, Windows event entry, or stack trace from the
  failure; or
- confirmation of the exact FieldWorks build on which it occurred.

Until that evidence is available, LT-22680 should be treated as not
reproducible rather than assumed fixed by a particular code change.
