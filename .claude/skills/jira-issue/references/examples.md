# Worked example: LT-22715

A real ticket, filed before this skill existed. The analysis in it is good.
The shape is the failure.

## Before

Summary: `No distinction between user-created and auto-generated natural classes`

Description opened with:

```
h3. The underlying problem

FLEx has two kinds of feature-based natural class and no way to tell them apart.
```

and ran past a thousand words through `h3. Symptoms`, `h3. Why the LT-22576
approach cannot be extended`, `h3. Proposed signal: presence of an
Abbreviation`, a five-column table, `h3. Open challenges`, and `h3. Three ways
to resolve them`.

Four things went wrong:

1. **The first rendered line is a heading**, so the first thing a triager reads
   is the word "problem" and nothing else.
2. **It contains four problems.** The description says so outright: "that
   single missing distinction produces four separate user-visible problems."
   None of the four can be triaged, prioritised or closed on its own.
3. **Analysis sits above the fold.** The comparison with LT-22576, the
   proposed signal, and the three resolution options are all real and useful,
   and none of them is what a triager needs in ten seconds.
4. **The blocking question is buried.** Four open challenges needing a team
   decision appear after roughly eight hundred words.

## After

Summary: `Natural Classes: generated classes are indistinguishable from real ones`

```
*What they want:* A rule should show the natural class the user picked, not a
stack of features, and the Natural Classes list should not fill with entries
nobody created.
*Who wants it:* Anyone building phonological rules from features. Surfaced by
LT-22576.
*Why it matters:* Editing a shared class from inside one rule silently changes
every other rule using it.

h3. The cause

FLEx has two kinds of feature-based natural class and nothing in the model
separates them: ones created deliberately in Grammar > Natural Classes, and
ones fabricated silently when features are inserted into a rule. Both are
{{PhNCFeatures}}.

h3. Symptoms, filed separately

# LT-AAAAA -- a shared class is rewritten from inside a rule, with nothing on
screen saying so
# LT-BBBBB -- a named class renders as a decomposed feature list
# LT-CCCCC -- generated classes accumulate in the user's list, never cleaned up
# LT-DDDDD -- no way to promote a feature bundle into a real class

h3. Ideas to resolve

# Treat a filled Abbreviation as "this is a real class" -- one branch in
{{RuleFormulaVcBase.Display}}, no model change
# Migrate, filling Abbreviation from Name -- needs a liblcm release and a
package bump, so no longer FieldWorks-only
# Stop naming generated classes at all -- no migration, but legacy data stays
ambiguous

h3. Open question

Existing real classes with no Abbreviation become indistinguishable from
generated ones. A team decision is needed before any of the three are built.

_Trade-offs, the LT-22576 comparison and the migration detail are in the first
comment._

*Next:* team decision on the migration question.
```

228 words. The four symptom tickets are linked, each triageable on its own.

## What moved to the comment

Everything cut is still on the ticket, one scroll down:

- Why the LT-22576 display-name heuristic cannot be extended
- The five-column rendering table
- Full detail on all three options
- All four open challenges, not just the blocking one
- The implementation note about the existing label-rendering path

Nothing was lost. It stopped being the first thing a triager reads.
