# Field contract, budgets, and Jira markup

## Budgets

| Field | Budget | Why |
| --- | --- | --- |
| Summary | 80 characters | Truncates in queue views past roughly that |
| Lede | 60 words, three labelled lines | One glance, no scrolling |
| Description | 250 words total, ending in one `*Next:*` line | Nothing can be collapsed |
| Analysis comment | As long as the reasoning deserves | Nobody is forced to scroll past it |

Count words before publishing. Over budget means the overflow was comment
material, not that the budget was wrong.

## Summary

Shape: `Area: what goes wrong`.

- Names the **symptom**, not the cause. The cause is often wrong at filing
  time; the symptom is what a duplicate search will match.
- No ticket references, no "Bug:", no trailing punctuation.
- Uses the reporter's vocabulary so the next person searching finds it.

Good: `Natural Classes: deleting a feature class empties rules that use it`
Bad: `PhNCFeatures.DeletionTextTSS override suppresses the delete warning`
Bad: `Issue with natural classes`

## Description skeleton

```
*What happens:* <one or two sentences>
*Who hits it:* <who, and roughly how many>
*How bad:* <data loss, workaround, blocking>

h3. Steps to reproduce
# <one bounded action>
# <one bounded action>

h3. Expected
<one line>

h3. Actual
<one line>

h3. Environment
FLEx <version>, <OS>. <Project, if shareable.>

*Not known:* <anything the reporter could not answer>

_Analysis in the first comment._

*Next:* <who does what>
```

Feature track replaces Steps/Expected/Actual with the sections in
`feature-track.md`. Everything else is identical.

## Jira Data Center wiki markup

There is **no `{expand}`**. It is a Confluence macro. Do not write one, and do
not plan a description around content being hidden.

| Need | Markup |
| --- | --- |
| Heading | `h3. Text` |
| Bold | `*text*` |
| Italic | `_text_` |
| Monospace | `{{text}}` |
| Numbered list | `# item` |
| Bullet list | `* item` |
| Code block | `{code:java}...{code}` or `{noformat}...{noformat}` |
| Quote block | `{quote}...{quote}` |
| Table | `||head||head||` then `|cell|cell|` |
| Link | `[text|https://example.com]` |
| Ticket reference | `LT-12345` -- linkifies automatically |
| Attached image | `!name.png!` or `!name.png\|thumbnail!` |

Two traps:

- `*` at the start of a line is a bullet, not bold. The lede labels work
  because `*What happens:*` is followed by text on the same line.
- Underscores inside identifiers turn on italics. Wrap any identifier in
  `{{...}}`.

## What goes in the analysis comment

Everything true and useful that a triager does not need in the first ten
seconds:

- Root-cause analysis and the code path
- Evidence, probes, test output
- Inferred mechanism, labelled inferred
- Options considered and their trade-offs
- Cost shape for each option
- Anything second-hand, labelled second-hand

Open the comment with one line saying what it is, so a reader scrolling past
knows whether to stop:

```
h3. Analysis (inferred unless marked verified)
```
