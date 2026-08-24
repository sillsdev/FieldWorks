# AI Parser Help — FLExText reference for LLMs

Part of the [AI Parser Help reference](../README.md). Where [`broken/`](../broken/README.md),
[`speed/`](../speed/README.md), and [`workflow/`](../workflow/README.md) cover the HermitCrab
**grammar** — the rules a linguist authors — this directory covers a different kind of upload
entirely: **FLExText**, the XML interchange format FieldWorks Language Explorer (FLEx) uses to
export **connected texts** — real corpus sentences with word-by-word analysis — so an LLM can help
reason about actual usage data, not just the grammar that's supposed to account for it.

- **[`flextext-format.md`](flextext-format.md)** — what FLExText actually is: the
  document/paragraph/phrase/word/morph structure, the `item` element's type system, the guid
  linkage back to FieldWorks objects, and a synthetic worked example.
- **[`analysis-status-and-ground-truth.md`](analysis-status-and-ground-truth.md)** — how the
  `analysisStatus` attribute distinguishes a human-approved analysis from a guess, what FLEx's own
  exporter and importer actually do with it, and why that distinction matters before you treat
  anything in a `.flextext` file as ground truth.
- **[`llm-code-execution.md`](llm-code-execution.md)** — which AI products can actually run code
  against your uploaded file (and which can't, as far as we could verify), plus ready-to-paste
  Python for the ones that can, so the LLM extracts structured data instead of trying to read
  potentially thousands of lines of raw XML itself.

Each reference file starts with a metadata header:

```yaml
---
title: <short name of the topic>
grounded_in: <FieldWorks and/or machine source file(s), or external doc(s), this is grounded in>
category: <format | provenance | llm-tooling>
when_to_use: <the question or moment this file answers>
---
```

## Got .flextext files and a question?

Send that person **this link instead**: [`getting-started.md`](getting-started.md) — it walks
through exporting texts from FLEx as FLExText and getting ChatGPT/Claude to reason about them
using this reference. The rest of this README is the reference material itself (for the LLM to
read), not the human-facing walkthrough.

## How to use this with an LLM

Paste the **raw** URL of the relevant topic file into your chat, e.g.:

```
https://raw.githubusercontent.com/sillsdev/machine/master/docs/ai-parser-help/texts/flextext-format.md
```

Use the raw URL (`raw.githubusercontent.com`), not the normal `github.com/.../blob/...` page — see
the top-level [README.md](../README.md) for why. If you're not sure which topic file is relevant,
paste this README's raw URL first and describe what you're trying to do (e.g. "I have a .flextext
export and don't know what the fields mean," "how do I tell which analyses are actually
confirmed," "how do I get you to actually read this file instead of skimming it").

## What belongs here

- FLExText format documentation, grounded in the FieldWorks schema and exporter source that
  produces the format.
- Synthetic example texts only — an invented language, an invented sentence, invented glosses.
  **Never real corpus data.** The privacy concern here is sharper than for the grammar sections:
  a real `.flextext` file *is* the real corpus text of a real language project, word for word,
  sentence for sentence — not a set of abstract rules that happen to describe a language. Treat it
  accordingly: never commit one to this repo, and see the note on sharing it with a third-party AI
  service in [`getting-started.md`](getting-started.md).
- **Not** real interlinear text data for any specific language or project (e.g. Sena, Amharic,
  Indonesian, Aweti). If a question requires reasoning about a real text, describe its structure
  abstractly (how many words, whether analyses are approved or guessed, what item types are
  present) instead of pasting the real text.

## Source grounding

Claims here are grounded in the FieldWorks source that defines and produces FLExText —
`DistFiles/Language Explorer/Export Templates/Interlinear/FlexInterlinear.xsd`,
`Src/LexText/Interlinear/InterlinearExporter.cs`, `Src/LexText/Interlinear/InterlinVc.cs`, and
`Src/LexText/Interlinear/FlexInterlinModel/FlexInterlinear.cs` — as of the commit each file's
metadata header cites, plus external documentation cited inline by URL. Unlike `broken/`/`speed/`/
`workflow/`, which avoid citing other repositories, this section's whole job is explaining an
interchange format — citing the FieldWorks source that defines it, and legitimate external
documentation of it, is expected and required here, not something to avoid. File/line references
may drift as FieldWorks evolves; if something looks stale, check the live source at the paths
cited.
