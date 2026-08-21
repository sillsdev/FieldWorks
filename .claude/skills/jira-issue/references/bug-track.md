# Bug track

What a FLEx bug report needs, from a real reporter's list: a brief description
of the problem, a sample project, and the steps to reproduce. Everything below
serves those three.

## The interview

Six questions maximum, one at a time. Stop early if the answers are already in
the raw report.

**Who** -- which user, what role (linguist, consultant, translation advisor),
and how many people are affected. "One user" and "every project on the team"
are different tickets.

**When** -- FLEx version *and* build number, the date it happened, whether it
is the first time or recurring, and whether it worked before. Version is
usually the decisive fact: a guard that shipped in 9.2.5 changes the whole
diagnosis depending on which side of it the reporter is on.

**Where** -- the tool and window, the exact menu path, and which project. Ask
whether the project can be shared before assuming it can.

**How** -- the exact actions. For text-entry bugs this is where the answer
hides: typed directly, pasted, dragged, typed with a vernacular keyboard or
IME, or arrived through Send/Receive. "Just typed it" and "pasted it" are
different bugs with different fixes.

**How bad** -- data loss, a workaround, or blocked work. Silent data loss
outranks a visible error. Say whether Undo recovers it.

## Steps to reproduce

Numbered, one bounded action per step, no step containing "and then" twice.
Each step is something the reader can do without knowing the codebase.

```
# Open Grammar > Natural Classes.
# Create a natural class from phonological features and name it.
# Insert that class into a phonological rule formula.
# Return to Natural Classes and delete the class.
# Open the rule again.
```

Rules:

- Start from a state the reader can reach: a new project, or a named sample.
- Never start at "with the corrupted project open".
- If a step needs specific data, say exactly what data.
- If reproduction is unreliable, say how many attempts out of how many. An
  intermittent bug reported as reliable wastes the first hour of the fix.
- If nobody has reproduced it, say so in one line and put the inferred path in
  the comment. A ticket that claims a reproduction it does not have is worse
  than one that admits the gap.

## Expected and Actual

One line each, both observable. "It should work" is neither.

## Environment

FLEx version and build, Windows version, and anything unusual: Send/Receive in
use, a non-default keyboard or IME, a project migrated from an older version.

## Sample project

**Ask before attaching. Always.**

> Do you have permission to post this?

FLEx projects contain unpublished lexical data, vernacular text, and often
speaker names. The reporter may not own the data, and a Jira attachment is a
publication to everyone with project access.

If the answer is no, or unclear:

- Describe the shape of the data instead: how many entries, which writing
  systems, which fields populated.
- Ask whether a minimal synthetic project reproduces it.
- Record in the ticket that a sample exists but was not attached, so nobody
  re-asks.

Never attach a project the agent found on disk. Never attach a screenshot of a
live project without the same question.

## Priority

Do not set a priority number. Give the triager the facts that determine one:
whether data is lost, whether Undo recovers it, whether a workaround exists,
and how many users are affected. Those four lines are worth more than a guess
at a field value, and the lede already carries them.
