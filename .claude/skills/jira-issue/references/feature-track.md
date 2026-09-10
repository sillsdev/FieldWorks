# Feature track

What a FLEx feature request needs: the user story -- what does the user want to
do -- what has been tried including workarounds, and ideas to resolve it.

## The interview

**Who wants it, and what are they actually trying to accomplish.** Users
request a widget when they have a goal. "Add a button that clears generated
natural classes" is a widget; "stop my rule list filling with entries I never
created" is the goal. File the goal, mention the widget as an idea. A ticket
filed as a widget forecloses every better solution.

**How often it comes up.** Once, in one project, is a different ticket from
every workshop for three years.

**What they do today.** The workaround is the single most useful thing in a
feature request. It shows the shape of the gap, proves the need is real, and
sometimes turns out to be good enough with one small change.

**What "done" looks like.** In the user's terms, not ours. This is what the
reporter will check when a build ships.

## Description skeleton

```
*What they want:* <the goal, in the user's words>
*Who wants it:* <who, and how often it comes up>
*Why it matters:* <what it costs them today>

h3. What they do today
<the workaround, and where it breaks down>

h3. Ideas to resolve
# <idea> -- <cost shape>
# <idea> -- <cost shape>

h3. Open questions
<what a team decision is needed on>

*Not known:* <anything unanswered>

_Detail in the first comment._

*Next:* <who does what>
```

## Ideas to resolve

Cap at five, ranked, one line each. Each idea carries a **cost shape**, not
hours:

- "One branch in one file."
- "Needs a liblcm release and a package bump, so no longer a FieldWorks-only
  change."
- "New model field, so a data migration."

Cost shape is what lets a triager sequence the work. Hours are a commitment
nobody in the conversation is authorised to make.

Detail belongs in the comment. The description gets the one-liners.

## Open questions

If the feature needs a decision that is not the implementer's to make -- what
happens to existing data, whether a field becomes mandatory, whether a
migration runs once or repeatedly -- say so, in one line each, under a heading
that says a team decision is needed. Burying a blocking question inside a
paragraph of analysis is how a ticket sits untouched for a year.

## Scope

**One ticket, one problem**, and features break this more often than bugs. A
request that reads "and while we are there we should also" is two tickets.

When one underlying cause produces several user-visible problems, file the
problems separately and link them to one ticket describing the cause. Each
problem can then be triaged, prioritised and fixed on its own, which is the
whole point of separating them.
