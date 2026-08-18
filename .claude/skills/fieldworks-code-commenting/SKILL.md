---
name: fieldworks-code-commenting
description: The FieldWorks code-comment standard for C#, C/C++, IDL, PowerShell, and project-file/Avalonia-view XML comments. Use whenever writing or editing a comment anywhere in this repository -- new code, refactors, or comment audits, in .cs, .cpp/.h, .idl, .ps1, .csproj/.vcxproj/.props/.targets, or .axaml alike. Covers doc-comment contracts, banned content categories, the 200-character cap on implementation comments, legacy references, XML doc tags, and XML-doc placement.
---

# FieldWorks Code Commenting

The standard for every comment an agent writes or audits in this repository.
Apply it while authoring, not as cleanup afterward. The audience is the next
reader of the code -- never the current reviewer, never a coverage gate.

## Scope

Every comment, any language: `//`/`///` in C#, C/C++, and IDL; `#`/`<# #>`
in PowerShell; `<!-- -->` in project files (`.csproj`/`.vcxproj`/`.props`/
`.targets`/`.proj`) and Avalonia views (`.axaml`). `Build/Agent/comment-
hygiene.ps1` mechanically enforces banned content, ASCII-punctuation-only,
and the 200-character implementation-comment budget against all of the
above (see `Get-CommentHygieneLanguage` for the exact extension list).
A C-style `/* */` block comment is not scanned -- only whole-line
`//`/`///`/`#` comments and `<!-- -->` XML comments are. Judgment-based
rules (accuracy, WHAT-not-HOW, standalone clarity) are not mechanically
checked and still apply while authoring.

In CI a violation fails nothing. Each one lands as a warning annotation on
the diff, and the whole set as a single pull request comment that updates
in place on every push. `build.ps1 -CommentHygiene` and
`test.ps1 -CommentHygiene` are what make the same violations blocking, and
an agent passes one of them on every run.

## The standard

1. **Accuracy first, then brevity.** A wrong comment is worse than none.
   Target 3-4 sentences; the ticket reference carries the background.
2. **WHAT and WHY, never HOW.** Test: would the sentence survive an
   equivalent reimplementation? If not, it's a how -- keep the purpose, drop
   the mechanism, even behind a "so that" clause.
3. **A member's summary states only its own contract**, never what callers
   do with it.
4. **Every comment stands alone.** No reader has this conversation, the PR,
   or a design document open.
5. **Public members need a summary; private members only when genuinely
   non-obvious.** Skip trivial properties, thin wrappers, self-evident
   helpers.
6. **Delete restatements.** If the code already says it plainly, the
   comment is noise.
7. **ASCII punctuation only.** No em-dashes, arrows, section signs, smart
   quotes -- use "--", "->", plain quotes. **Exception: inside an XML
   `<!-- -->` comment, use a single "-", never "--"** -- the XML spec
   forbids a literal `--` anywhere in comment content (not just adjacent to
   `-->`), so the usual em-dash replacement produces invalid XML there.
8. **No ambiguous abbreviations.** Write "ViewModel", not "VM". Spell out
   anything a reader could resolve two ways -- in comments, docs, commits,
   and conversation.

## Banned content (mechanical -- no judgment needed)

1. **Process framing**: no "Phase-1", "this commit", "later we'll...".
   Describe the code that is HERE.
2. **Internal doc pointers**: no `.md` file/section references or finding
   codes (`D1`, `M4`). Jira `LT-#####` is the sanctioned durable pointer.
3. **Absence narration**: no "no longer", "used to", "was removed". State
   what IS there. (A legitimate current-behavior or null-input contract is
   not absence narration -- keep those.)
4. **Cross-file comment pointers**: no "see X's note" -- nothing checks
   that link, so it silently rots. Cite the symbol, or state the point
   here. Same-file pointers are fine.
5. **Consumers/provenance**: no "shared by X and Y", "the only caller",
   "extracted from Z". Callers change silently -- state the guarantee, and
   leave the callers anonymous.
6. **Xml comments only**: no literal `--` in an `<!-- -->` comment's content
   -- invalid XML, not just a style violation. Use a single `-`.

## Legacy references

Only as a behavioral-parity WHY ("matches the legacy X"). No temporal
framing -- not "the replacement for X", not "until we build Y". Prefer
symbol names over line numbers, which rot.

## References to other code

Only when the reader needs it to understand THIS symbol, never for
completeness or navigation. `<see cref="X"/>` in doc comments; a bare
symbol path in `//` comments, where tooling won't resolve it.

**Link the contract, not the collaborator.** State what a delegation
guarantees; leave the callee unnamed -- naming it documents HOW, not WHAT.
Never reference another member's locals, another type's private internals,
or a test.

## XML doc tags

Omit `<param>`/`<returns>` that only restate the name and type; keep ones
carrying units, null meaning, ownership, or constraints.

**All-or-nothing:** document every parameter, or fold the note into summary
prose and drop the tags -- never document only some. What the method DOES
with an argument folds into prose; what qualifies the VALUE (units,
absence, a constraint) stays a tag. Never state a fact in both the summary
and a tag. When a parameter's type already documents its own members
(a state/options class), don't restate them as `<param>`.

`<exception>` for every error condition the caller must handle.

## Types, files, and sections

Interfaces, classes, structs, records, and enums follow the same rules;
document non-obvious members individually, not in the type-level summary.
No decorative file-header banners beyond the license header. No section
dividers that merely restate the adjacent member name.

## Inline comments

Sparingly: only when the reasoning isn't clear from the code, or a bugfix
is non-obvious. **200 characters total**, across as many lines as the
line-length limit requires. Past that, it belongs as a doc comment (which
may run long-form), or is trying to explain too much -- cut to the single
sentence that would confuse a reader most if missing, even if that loses
nuance. Mechanically enforced (`comment-too-long`) for `//` and `#` alike.
Place above the nesting level the code spans.

**Line width is separate, and applies to every comment line**, doc comments
included: no comment line may exceed `.editorconfig`'s `max_line_length`
(98 columns today), counting a tab as `tab_width` columns.
`comment-hygiene` reads those two values from `.editorconfig` itself, so the
limit can never drift from the one the rest of the repo follows. Enforced as
`comment-line-too-long`; a local run re-wraps the line for you, CI only
reports it.

**The budget rises to 600 characters in dense branching code.** A comment
introducing a region whose decision-point count reaches 10 (McCabe
complexity 11 -- the classic "high" threshold) gets the larger budget
automatically, because a reader there needs the invariants spelled out and
200 characters buys about two sentences. `comment-hygiene` measures the code
the comment introduces, stopping at the end of the enclosing block or 40
lines. This fires on roughly 2% of the comments already over 200 characters,
and is meant to stay that rare -- if a comment in ordinary straight-line code
will not fit, shorten it rather than looking for a way to qualify.

**Exemptions from the length cap:** a C#/C/C++/IDL `///` doc comment; a
PowerShell comment-based help block (`<# ... #>`); and, in a project file or
Avalonia view, the file's FIRST `<!-- -->` block, wherever it falls (before
the root element, or as its first child) -- XML has no separate doc-comment
syntax, so that first block is this format's equivalent of a `///` summary
and may run long-form. Every `<!-- -->` block after the first one is an
ordinary implementation comment, budgeted like any other.

In tests, two extra tells of noise: restating what the test method name
already says, and justifying a test to the coverage gate. Why a dependency
is faked, or fixture data is shaped a particular way, stays fine -- that is
reasoning the code cannot show.

## XML-doc placement gotchas

- One `<summary>` per member -- two consecutive doc blocks both attach to
  the NEXT declaration. Verify each summary sits directly above its own
  declaration.
- Public constructors with `<param>` docs also get a one-line `<summary>`.
- `<see cref>` resolves only inside `///`; in `//` it renders as literal
  text -- use the bare symbol path there.
- Escape `<` and `>` in doc text as `&lt;`/`&gt;`.
- String literals are not comments: never edit assertion messages,
  automation ids, or resx values during a comment pass.

## When auditing existing comments

Report every deletion and rewrite (before -> after) so a human can object;
when unsure whether a comment is a current-behavior contract or absence
narration, keep it and flag it. Never let a comment edit change behavior:
comment-only diffs, string literals untouched.
