# Read this first: a FieldWorks grammar and texts for analysis

This folder was exported from FieldWorks Language Explorer (FLEx) by its "Export Grammar and
Texts for AI Analysis" command. Everything in it was put here deliberately for an AI assistant
(ChatGPT, Claude, or otherwise) to read, including this file.

## What is in this folder

- `HCGrammar.xml` -- the project's **HC grammar**: its HermitCrab-format grammar (phonology,
  morphology, features, categories), the same shape the HermitCrab parser itself consumes to
  parse words. This is the parser's own input, not a human-readable grammar description.
- One `.flextext` file per exported text -- FLExText, FieldWorks' interlinear-text interchange
  format. These are real corpus texts the grammar is supposed to account for. Any given text may
  be fully analyzed, partly analyzed, or not analyzed at all.
- `export-instructions.md` -- this file.

## Before you answer, read the reference

Grammars and interlinear texts have real gotchas you will not know about on your own. Fetch and
read this first:

```
https://raw.githubusercontent.com/sillsdev/FieldWorks/main/Docs/ai-parser-help/README.md
```

That is the index for a reference covering correctness gotchas (`broken/`), performance gotchas
(`speed/`), grammar-authoring methodology (`workflow/`), and the FLExText format itself,
including the `analysisStatus` ground-truth caveat (`texts/`). Use the raw URL rather than the
`github.com/.../blob/...` page -- it returns plain markdown that fetches cleanly. Follow whichever
of its links bear on the question you were asked, and reason about this project's grammar against
the engine's documented mechanics rather than guessing.

## How to answer: plain language, for a linguist

The person asking is a field linguist with a real language project open in FLEx. They know their
language; they usually do not know HermitCrab's internals, and they did not ask for a tour of
them. An answer that reads like engine documentation gets abandoned, however correct it is.

- **Accuracy first, then plain language.** An answer that misstates the engine is worse than no
  answer, so never simplify to the point of being wrong. When the honest answer is complicated,
  give the short version first and the detail underneath.
- **No unexplained jargon.** Use a technical term only if the answer genuinely needs it, and
  define it in ordinary words the first time: "MPR feature (a label you put on a morpheme so that
  rules can test for it)". Never leave an abbreviation bare -- write "morphosyntactic property",
  not "MSP".
- **Use the words FLEx uses.** Name what the user actually clicks (the Grammar area, an affix
  template, a slot, an allomorph, an inflection class) rather than the engine's internal class or
  field name, so they can find it. Describe fixes as things to do in FLEx, not as XML to
  hand-edit.
- **Lead with what to do.** Open with the change to make, in a sentence or two. Put the mechanism
  -- why the engine behaves this way -- after it, for the reader who wants it.
- **Show rather than lecture.** One worked example with a real surface form beats a paragraph of
  theory.
- **Short sentences.** Cut "it is important to note that", "as mentioned above", and any
  restatement of what you just said.
- **Flag every guess, every time.** Name what you are unsure about and what would settle it (a
  specific word to parse, a trace to look at). The user cannot see your reasoning, so an unmarked
  guess reads exactly like a fact, and a confident wrong answer costs them real work.
- **Each answer stands alone.** Do not lean on earlier turns in the chat, or on file names in the
  reference, as though the user had them open.

## For the person who exported this folder

**This is AI. It can be very wrong, and it can be wrong while sounding certain.** Treat anything
it tells you about your grammar or your texts as a suggestion to check against your own data,
never as a finding. Understand a change, and confirm it yourself by re-parsing the words you care
about, before you apply it to your project.

Your exported files are real linguistic data about a real language project. Dropping them into a
third-party chat service sends that data to that provider (OpenAI, Anthropic, and so on), so check
your project's data-sensitivity policy before sharing an unpublished or restricted grammar or text
this way.

A fuller human-readable walkthrough, including example questions worth asking, is at:

<https://github.com/sillsdev/FieldWorks/blob/main/Docs/ai-parser-help/getting-started.md>
