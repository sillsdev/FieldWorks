---
name: fieldworks-code-commenting
description: The FieldWorks code-comment standard for C# and PowerShell. Use whenever writing or editing a comment anywhere in this repository -- new code, refactors, or comment audits, in .cs or .ps1 alike. Covers doc-comment contracts, banned content categories, the 200-character cap on implementation comments, legacy references, XML doc tags, and XML-doc placement.
---

# FieldWorks Code Commenting

The standard for every comment an agent writes or audits in this repository.
Apply it while authoring -- do not write loose comments and clean them later.

The audience is the next reader of the code -- never the reviewer of the
current diff, never a coverage gate.

## Scope

Every comment in this repository, in any language: `//`/`///` in C#, `#`/
`<# #>` in PowerShell. The mechanical subset of this standard (banned content,
ASCII-only, the 200-character implementation-comment budget) is enforced by
`Build/Agent/comment-hygiene.ps1` against `.cs` and `.ps1` files alike; the
judgment-based rules (accuracy, WHAT-not-HOW, standalone clarity) are not
mechanically checked in either language and still require applying this
standard while authoring.

## The standard

1. **Accuracy first, then brevity.** A comment that misstates behavior is
   worse than none. Target 3-4 sentences (max ~6 lines); the ticket reference
   carries the background.
2. **Doc comments state WHAT and WHY, never HOW.** The how-test: would the
   sentence survive an equivalent reimplementation? If not it is a how, and a
   "so that..." clause does not redeem it -- keep the purpose, drop the
   mechanism.
3. **A member's summary states only its OWN contract.** Never narrate what
   callers do with it.
4. **Every comment must stand alone.** No reader has this conversation, the
   PR, or any design document open. A doc comment must serve someone hovering
   the symbol who will never read the body.
5. **Public members require a summary.** Private members get one only when
   genuinely non-obvious. Skip trivial properties, thin wrappers, and
   self-evident helpers.
6. **Delete restatements.** A comment repeating what the code plainly says is
   noise. State what the code does only when that is not obvious from the
   content closely following the comment. When a comment does not clearly
   earn its place, delete it.
7. **ASCII only.** No em-dashes, arrows, section signs, or smart quotes in
   comments or repo docs -- they render poorly in git tooling. Use "--",
   "->", plain quotes.
8. **No ambiguous abbreviations.** Write "ViewModel", never "VM" (VM also
   means virtual machine). Same for anything a reader could resolve two
   ways: spell it out. This applies to comments, documents, commit
   messages, and what you say to the developer.

## Banned content (mechanical -- no judgment needed)

1. **Migration/process framing**: no "Phase-1", "Stage 3", "this commit",
   "this pass", review/creation-process language, or forward-work notes
   ("later we'll...", "Stage N wires..."). A comment describes the code that
   is HERE.
2. **Internal document pointers**: no references to design/skill/working
   `.md` files or their section markers (no "winforms-free-lexeme-editor.md",
   "section 19b", "D1/M4/H1" finding codes, task numbers). If the comment
   carries a genuine WHY, rewrite it self-contained. Jira `LT-#####`
   references ARE allowed -- they are the sanctioned durable pointer.
3. **Absence narration**: no comments whose subject is that code is gone,
   was removed, "no longer" does something, or "used to" work differently.
   State positively what IS there; the absence needs no narration.
   (A legitimate null-input or current-behavior contract is not absence
   narration -- keep those.)
4. **Pointers to a comment in another file**: never "see X's note", "as
   documented on Y". No tooling checks a comment-to-comment link, so the
   target can be reworded or deleted with nothing flagging the break, and the
   reader must leave their current file to learn whether anything is there.
   Cite the code symbol that carries the behavior, or state the point here.
   Same-file pointers are fine.
5. **Consumers and provenance**: no "shared by X and Y", "the only caller
   is...", "internal so both create paths can use it", "extracted from Z".
   Callers change silently and provenance is not a contract. State what the
   member guarantees; callers stay anonymous.

## Legacy references

Naming legacy code is allowed ONLY as a behavioral-parity WHY that justifies
current behavior: "matches the legacy MatchingObjectsBrowser multi-column
list" stays. Temporal migration framing goes: not "the replacement for X",
not "until we build Y", not the history of how the code got here. Pointers to
real legacy source (`DataTree.cs:2455`) are acceptable parity evidence but
prefer symbol names over line numbers -- lines rot.

## References to other code

Reference another symbol only when the reader needs it to understand THIS
one -- never for completeness or navigation. In doc comments write
`<see cref="SomeType"/>`; in `//` comments, where tooling does not resolve
it, write the bare symbol path.

**Link the contract, not the collaborator.** Naming the helper a member
delegates to ("resolves via the shared `BuildSandboxMsa`", "forwards to
`PerformAddAllomorph`") documents HOW it works. State what the delegation
guarantees and leave the callee unnamed. Rewrite rather than delete -- the
guarantee is the useful half.

Never reference another member's locals, another type's private internals, or
a test -- describe the behavior instead.

## XML doc tags

Omit `<param>`/`<returns>` that only restate the name and type. Keep ones
carrying what the declaration cannot: units, what null means, ownership, side
effects, constraints. Never `<param name="cache">The cache.</param>`.

**All-or-nothing (overrides the omit rule):** never document only some
parameters. When one deserves a note, either document every parameter -- when
each has something real to say, never padding -- or fold the note into the
summary prose and drop the tags. Folding edits the summary: rewrite it to
carry the note.

Which way to go: what the method DOES with an argument ("matched against the
candidate morphemes", "used in the thrown message") folds into prose. What
qualifies the VALUE -- units, meaning of absence, a constraint the type cannot
express -- stays a tag.

**No double documentation.** A fact lives in the summary or in a tag, never
both.

**No mirrored member docs.** When a parameter's type documents its own members
-- a state or options class -- do not restate them as `<param>`. The member
docs are the single source of truth.

`<exception>` for every error condition the caller must handle; omit it when
the member does not throw.

## Types and enums

Interfaces, classes, structs, records, and enums follow the same rules.
Document each member whose purpose is not self-evident from its name and type,
individually rather than in the type-level summary.

## File and section comments

No decorative file-header banners beyond the license header. No section
dividers that merely restate the adjacent member name -- a divider must carry
information of its own.

## Inline comments

Sparingly: only when the reasoning is not clear from the code, or a bugfix is
non-obvious. **200 characters, across as many lines as that takes to respect
the line-length limit.** If the WHY needs more than that, either it belongs
as a doc comment on a named symbol (which may run long-form), or the comment
is trying to explain too much at once -- cut it to the single sentence that
would confuse a reader most if it were missing, and drop the rest, even if
that loses nuance the original draft had. `Build/Agent/comment-hygiene.ps1`
enforces this mechanically (category `comment-too-long`) for `//` and `#`
alike -- see "Scope" above.
Located above the level of nesting that the code being described spans.

In tests, two extra tells of noise: restating what the test method name
already says, and justifying a test to the coverage gate ("covers the false
branch of..."). Why a dependency is faked, or why fixture data is shaped a
particular way, stays fine -- that is reasoning the code cannot show.

## XML-doc placement gotchas

- One `<summary>` per member; two consecutive doc blocks both attach to the
  NEXT declaration -- the first lands on the wrong member. Verify each
  summary sits directly above its own declaration.
- Public constructors with `<param>` docs also get a one-line `<summary>`.
- `<see cref>` resolves only inside `///` doc comments. In a `//` comment it
  renders as literal text -- use the bare symbol path there.
- Escape `<` and `>` in doc text as `&lt;`/`&gt;`.
- String literals are not comments: never edit assertion messages,
  automation ids, or resx values during a comment pass.
- resx accessor files may carry APPEND-ONLY section rules -- respect them.

## Worked examples (from the live audit)

Phase framing stripped:
- Before: `// Phase 3 test (b): picking a style applies it to the selection`
- After:  `// Picking a style applies it to the selection`

Doc marker removed, WHY kept self-contained:
- Before: `// winforms-free-lexeme-editor.md D1: a plugin-claimed custom slice renders its plugin's own control`
- After:  `// A plugin-claimed custom slice renders its plugin's own control`

Absence rewritten to current behavior:
- Before: `// An ORC run no longer forces the whole value read-only.`
- After:  `// An ORC run does not force the whole value read-only.`

Temporal legacy framing trimmed to parity:
- Before: `/// the Avalonia replacement for the legacy ReallySimpleListChooser`
- After:  `/// the Avalonia analog of the legacy ReallySimpleListChooser`

Over-long summary trimmed to purpose + non-obvious contracts:
- A 37-line dialog ViewModel summary enumerating implementation steps becomes ~10
  lines: purpose, the LCModel-free rule, and the two non-obvious contracts
  (commit-on-select single-stage; opt-in two-stage auxiliary).

## When auditing existing comments

Report every deletion and rewrite (before -> after) so a human can object;
when unsure whether a comment is a current-behavior contract or absence
narration, KEEP it and flag it. Never let a comment edit change behavior:
comment-only diffs, string literals untouched.
