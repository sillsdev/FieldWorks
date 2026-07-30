---
name: fieldworks-code-commenting
description: The FieldWorks code-comment standard. Use whenever writing or editing code comments in this repository -- new code, refactors, or comment audits. Covers doc-comment contracts, banned content categories, legacy references, and XML-doc placement.
---

# FieldWorks Code Commenting

The standard for every comment an agent writes or audits in this repository.
Apply it while authoring -- do not write loose comments and clean them later.

## The standard

1. **Accuracy first, then brevity.** A comment that misstates behavior is
   worse than none. Target 3-4 sentences (max ~6 lines); the ticket reference
   carries the background.
2. **Doc comments state WHAT and WHY, never HOW.** The code is the how. A
   member's summary states only its OWN contract -- never narrate what
   callers do with it.
3. **Every comment must stand alone.** No reader has this conversation, the
   PR, or any design document open.
4. **Public/exported members require a summary.** Private members get a
   comment only when genuinely non-obvious.
5. **Delete restatements.** A comment that repeats what the code plainly says
   is noise. When a comment does not clearly earn its place, delete it.
6. **ASCII only.** No em-dashes, arrows, section signs, or smart quotes in
   comments or repo docs -- they render poorly in git tooling. Use "--",
   "->", plain quotes.

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

## Legacy references

Naming legacy code is allowed ONLY as a behavioral-parity WHY that justifies
current behavior: "matches the legacy MatchingObjectsBrowser multi-column
list" stays. Temporal migration framing goes: not "the replacement for X",
not "until we build Y", not the history of how the code got here. Pointers to
real legacy source (`DataTree.cs:2455`) are acceptable parity evidence but
prefer symbol names over line numbers -- lines rot.

## XML-doc placement gotchas

- One `<summary>` per member; two consecutive doc blocks both attach to the
  NEXT declaration -- the first lands on the wrong member. Verify each
  summary sits directly above its own declaration.
- Public constructors with `<param>` docs also get a one-line `<summary>`.
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
- A 37-line dialog VM summary enumerating implementation steps becomes ~10
  lines: purpose, the LCModel-free rule, and the two non-obvious contracts
  (commit-on-select single-stage; opt-in two-stage auxiliary).

## When auditing existing comments

Report every deletion and rewrite (before -> after) so a human can object;
when unsure whether a comment is a current-behavior contract or absence
narration, KEEP it and flag it. Never let a comment edit change behavior:
comment-only diffs, string literals untouched.
