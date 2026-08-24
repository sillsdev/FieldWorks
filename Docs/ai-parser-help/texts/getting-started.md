# Get help from ChatGPT or Claude with your interlinear texts

Part of the [FLExText-for-LLMs reference](README.md). This page is for anyone with a FieldWorks
Language Explorer (FLEx) project who wants an LLM's help reasoning about **connected texts** —
real corpus sentences with word-by-word glossing and morpheme breakdowns — rather than the
grammar. Typical questions: "does my corpus actually support the paradigm I think it does," "how
many of these analyses are actually human-confirmed versus the parser guessing," "summarize what
part-of-speech categories appear in this text and how often."

If your question is about the *grammar itself* (why a word won't parse, why parsing is slow, how
to model something), see the top-level [`getting-started.md`](../getting-started.md) instead —
this page is specifically about exported texts.

## Step 1 — Export your text(s) as FLExText

FieldWorks exports interlinear texts via the FLEx UI itself — there is no separate command-line
tool for this (unlike `GenerateHCConfig.exe` for grammar export). The export walks the
Interlinear Text tool's own rendered view, so **the fields and writing systems you see on screen
are exactly what ends up in the file** — configure that first.

1. **Configure what you want included.** In FLEx, go to **Tools > Configure > Interlinear** and
   make sure the interlinear lines you want (baseline, word gloss, word category, morpheme
   breakdown, lexical gloss, free translation, literal translation, notes, ...) and the writing
   systems you care about are turned on. The export pulls from this configured view, not directly
   from the underlying database — anything not currently displayed won't be exported.
2. **Open the text.** In the **Texts & Words** area, use the **Interlinear Texts** tool to open
   the text you want to export, and go to its Gloss, Analyze, Tagging, or Print View tab.
3. **Start the export.** Choose **File > Export Interlinear** from the main menu.
4. **Pick the FLExText option.** In the Export Interlinear dialog, click **"ELAN, SayMore, FLEx
   FLEXTEXT"**, then click **Export**.
5. **Choose which text(s).** A Choose Texts dialog opens with the current text preselected — pick
   any others you want in the same file, then click OK.
6. **Save the file.** In the "Export to FLEXTEXText" dialog, pick a directory and filename, then
   click **Save**. This produces a `.flextext` file — an XML file that may contain one or more
   `interlinear-text` elements, one per text you selected.

This procedure (and the underlying FLExText format) is documented by SIL in Ken Zook's
[*Technical Notes on FLEx Text Interlinear*](https://downloads.languagetechnology.org/fieldworks/Documentation/Technical%20Notes%20on%20FLEx%20Text%20Interlinear.pdf)
(May 2026), sections 2 and 4 — that document is the authoritative source for the export/import
workflow and for the schema itself; [`flextext-format.md`](flextext-format.md) in this reference
summarizes the parts most relevant to getting an LLM to reason about the file correctly.

## Step 2 — Upload the file into ChatGPT or Claude

Attach the `.flextext` file directly as a file upload (don't paste the XML inline — a text with
more than a few sentences quickly runs into thousands of lines, and ChatGPT/Claude's own reading
of a large raw XML file inline is unreliable — see
[`llm-code-execution.md`](llm-code-execution.md) for why, and for commands that have the LLM
extract a compact summary itself instead of trying to read the whole file at once).

## Step 3 — Point it to the FLExText reference

Paste this URL into the same chat:

```
https://raw.githubusercontent.com/sillsdev/machine/master/docs/ai-parser-help/texts/README.md
```

This tells the LLM where to find documentation of the FLExText format itself — its structure, its
`analysisStatus` ground-truth caveats, and (if the product supports it) code it can run directly
against your uploaded file.

## Step 4 — Ask your question

Some examples, once your `.flextext` file and the reference URL are in the chat:

- "Extract every free translation and its corresponding baseline text from this file."
- "How many of the word-level analyses in this text are human-approved versus guessed? List the
  guessed ones."
- "What parts of speech appear in this text, and how often does each occur?"
- "Does this text contain any words with more than N morphemes? List them with their breakdowns."
- "Summarize this text's morpheme inventory — which roots and affixes actually occur, and how
  often."

## A note on privacy

Unlike a grammar file (a set of rules that happen to describe a language), a `.flextext` export
**is the real corpus text itself** — actual sentences, actual translations, actual speaker/note
data if present, word for word. Uploading it to a third-party chat service sends that data to that
provider. This is a stronger privacy consideration than for a grammar file, not a weaker one —
check your project's data-sensitivity policy before sharing an unpublished, restricted, or
community-sensitive text this way, independently of the fact that real corpus data must never be
committed to this repository (see [`README.md`](README.md#what-belongs-here)).
