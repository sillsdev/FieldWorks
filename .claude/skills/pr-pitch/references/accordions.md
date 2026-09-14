# The accordions (the bottom zone)

Everything below the pitch, in collapsed `<details>` blocks after a `---`.
A closed `<details>` costs a reader nothing, so length here is free -- but
**synthesise, do not paste.** Dumping the branch's specs into `<details>`
blocks is the failure mode this skill exists to prevent.

Open with a short orienting block, so a reader a year out knows the working
documents were deliberately deleted rather than lost.

```markdown
---

<details>
<summary><b>Reading this a year from now</b> -- start here</summary>

What this record is, and why the reasoning lives here instead of in the tree.

</details>
```

## Sections worth writing, when the branch has them

| Section | Carries |
| --- | --- |
| **Reading this a year from now** | The orienting preamble. Always first |
| **The layer cake** | For a branch introducing an architecture: input to output, with the real type at each hop. The most useful thing for someone arriving cold |
| **Decisions, and why** | The choice, the alternatives, what tipped it. Prefer decisions where the code looks arbitrary until you know the reason |
| **Paths not taken** | What was tried or seriously considered and rejected. The highest-value section -- it stops the next person re-proposing a dead end |
| **Reversals** | Built then removed, and why. Note which are invisible in `git log` because they happened inside a squash |
| **Surprising findings** | What contradicted the initial assumption |
| **What this does NOT authorize** | For a foundational branch, the limits. Otherwise it gets cited as precedent for more than it decided |
| **Deferred, and what would unblock it** | Scoped out, and what would need to be true to pick it up |
| **Evidence** | The proof behind the pitch's "Where to look" bullets. Written for a reviewer rather than a maintainer, so it sits last |

## Rules

- Each section stands alone. Nobody reads these top to bottom.
- Attribute nothing to a person. Describe the decision, not the deciders.
- GitHub caps a body at 65,536 characters. Near it means you are pasting.
- **Reasoning recoverable only from git history** -- because the branch
  deleted the document that argued it -- is the highest-value content here.
  Prefer it over anything a reader could derive by reading the tree.
