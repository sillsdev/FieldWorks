# Compact style for issues and PR bodies

Shared reference. Pointed at by `.claude/skills/jira-issue/SKILL.md` and
`.claude/skills/pr-pitch/SKILL.md`. Adapted from the MIT-licensed
`i-have-adhd` skill (https://github.com/ayghri/i-have-adhd), which shapes chat
turns; these rules shape written artifacts instead.

## What changes when the reader is not in the conversation

A chat reader has just asked you something. A Jira reader is scanning a queue
of forty tickets, and a PR reader has eleven other reviews open. Both arrive
cold, months after the work, with none of your context and no way to ask a
follow-up. Four consequences:

1. The first line is the only line guaranteed to be read. It carries the
   decision, not the setup.
2. Nothing can be held in the reader's head from a previous paragraph.
3. There is no "let me know if you need anything else". Whatever is unanswered
   stays unanswered.
4. Length is not a cost the reader chooses to pay. Jira Data Center has no
   collapsible sections, so every word in a description is on the screen.

## The rules

### 1. Lead with what the reader must decide

Not context, not history, not the investigation. For a triager: what breaks,
who it hits, how bad. For a reviewer: what the change does and what it risks.

Bad: `h3. The underlying problem` / "While investigating LT-22710 we noticed"
Good: "Deleting a feature-based natural class empties every rule that used it."

### 2. Number every multi-step sequence

Repro steps, verification steps, migration steps. One bounded action per step.
**No step contains "and then" twice.** If a step needs a sub-list, it is two
steps.

### 3. End with one named next action

Every description ends with a single `*Next:*` line naming who does what.
Tickets and PRs that end without one stall, because nobody is named.

Good: `*Next:* reporter to confirm the FLEx version (see comment).`
Good: `Next: approve, or tell me to split the installer change out.`

### 4. One artifact, one problem

A ticket describing four problems is four tickets, linked. A PR doing three
unrelated things is three PRs, stacked. Splitting is cheap at filing time and
expensive at review time.

### 5. State cause, not concern

No "seems to", no "there may be an issue with", no apology. Name the symptom
and, if known, the mechanism. If the mechanism is inferred rather than
observed, say "inferred" -- that is information, whereas hedging is noise.

### 6. Cost shape, not hours

Hours on a ticket read as a commitment nobody made. Size the work by what it
touches.

Bad: "This will take some work." / "About two days."
Good: "One branch in one file." / "Needs a liblcm release and a package bump."

### 7. Cap any list at five

Past five, split into "must" and "nice to have", or accept that the artifact
is really an epic. Five ranked beats ten unranked.

### 8. No preamble, no recap, no closer

Banned openers: "This issue describes", "This PR refactors", "While
investigating", "As a note", "I have been looking into". Banned closers:
"Please let me know", "Hope this helps", "Happy to provide more detail".

Start with the answer. Stop when the answer is done.

### 9. Say what is not known

An explicit `*Not known:*` line is worth more than a confident guess. It tells
the next reader where to dig and stops a fabrication becoming folklore.

### 10. Never assert what was not verified

Everything above the fold is either something a reporter said or something we
observed. Analysis, inference and reconstruction go in a comment or an
accordion, labelled as such. A screenshot from a headless test is not a
screenshot of the product; say which it is.

## Pre-send check

Delete before publishing:

1. The first sentence, if it announces what the artifact is about to do.
2. The last sentence, if it recaps or asks for further questions.
3. Any "by the way" sidebar. It is a separate ticket.
4. Hedging adverbs carrying no information. Keep a hedge that carries real
   uncertainty; deleting that one manufactures confidence.
5. Any idiom. Replace with the literal action.

Then verify: **reading only the title and the last line, does the reader know
what is wrong and what happens next?** If yes, publish.
