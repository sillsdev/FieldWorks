# AI Parser Help — HermitCrab reference for LLMs

This is a living reference for asking an LLM (ChatGPT, Claude, or otherwise) questions about
**HermitCrab**, the rule-based morphological parser/generator implemented in this repository
(`sillsdev/machine`, namespace `SIL.Machine.Morphology.HermitCrab`), and about **FLExText**,
FieldWorks' interlinear-text interchange format for the connected corpus texts a grammar is
supposed to account for. It covers three kinds of question a grammar author actually asks about
HermitCrab itself, plus a fourth section for a different kind of upload entirely — real corpus
texts rather than the grammar:

- **[`broken/`](broken/README.md)** — "why is this wrong / missing / crashing?" Correctness
  gotchas: wrong parses, missing parses, crashes, and silent misconfigurations.
- **[`speed/`](speed/README.md)** — "why is this slow?" Performance gotchas: combinatorial
  blowups and other parse-time costs.
- **[`workflow/`](workflow/README.md)** — "how should I approach modeling this?" Authoring
  guidance for building a grammar well in the first place, grounded in HermitCrab's actual
  mechanics and in H. Andrew Black's FLEx parsing methodology (primary source included verbatim
  under `workflow/sources/`).
- **[`texts/`](texts/README.md)** — "I also have real corpus texts, not just a grammar." A
  reference for FLExText, FieldWorks' interlinear-text interchange format: what it is, how to
  extract it, and how to get an LLM to reason over it correctly, including the `analysisStatus`
  ground-truth caveat and which AI products can actually run code against your uploaded file.

Each file covers one topic in enough depth to answer questions about that topic without needing
local access to the repo — code excerpts, mechanisms, and worked examples are inlined.

## Got a FieldWorks grammar and a question?

Send that person **this link instead**: [`getting-started.md`](getting-started.md) — it walks
through extracting your grammar as HermitCrab XML and getting ChatGPT/Claude to reason about it
using this reference. The rest of this README is the reference material itself (for the LLM to
read), not the human-facing walkthrough.

Got interlinear **texts** (`.flextext` files) instead of, or in addition to, a grammar? See
[`texts/getting-started.md`](texts/getting-started.md) instead — extracting and reasoning about
connected corpus texts is a different workflow from the grammar one above.

## How to use this with an LLM

Paste the **raw** URL of the relevant topic file into your chat, e.g.:

```
https://raw.githubusercontent.com/sillsdev/machine/master/docs/ai-parser-help/speed/affix-template-optional-slots.md
```

Then ask your question. Use the raw URL (`raw.githubusercontent.com`), not the normal
`github.com/.../blob/...` page — the raw URL returns plain markdown text with no site chrome,
which fetches cleanly for both ChatGPT (web browsing) and Claude (WebFetch) without JS rendering
or auth. If you're not sure which topic file is relevant, paste this README's raw URL first, or
whichever of `broken/README.md`, `speed/README.md`, `workflow/README.md`, `texts/README.md` best
matches your question ("why is this wrong" vs. "why is this slow" vs. "how should I model this"
vs. "I have corpus texts, not just a grammar") — an LLM that can follow links will use it as an
index; otherwise, browse the lists yourself.

Do not use these guides as a source of real grammar or text data — see "What belongs here" below.

## What belongs here

- General HermitCrab engine mechanics: how rules, strata, templates, features, and the
  analysis/synthesis engines work. This is documentation of the open-source parser itself.
- FLEx/HermitCrab grammar-authoring methodology, grounded in the engine's actual behavior.
- FLExText format documentation, grounded in the FieldWorks schema/exporter source that produces
  it — see [`texts/`](texts/README.md).
- Synthetic/toy grammar snippets used purely to illustrate a mechanism (e.g. `p1`..`p12`,
  `sg`/`pl` × invented cases) are fine, as are synthetic/invented interlinear-text examples.
- **Not** real grammar or text data for any specific language (e.g. Sena, Amharic, Indonesian,
  Aweti). Those grammars — and any real corpus texts from those projects — are private and must
  never be committed to this repo — see the project's existing grammar-privacy constraints. If a
  question requires reasoning about a real grammar or text, describe the relevant structure
  abstractly instead of pasting the real rules or sentences.

## Source grounding

Claims in `broken/`, `speed/`, and `workflow/` are grounded in the actual engine source under
`src/SIL.Machine.Morphology.HermitCrab/` and `src/SIL.Machine/` as of the commit each file was
last updated — each file's metadata header names the specific source file(s) it's grounded in.
Claims in `texts/` are grounded in the FieldWorks source that defines and produces FLExText, plus
cited external documentation — see [`texts/README.md`](texts/README.md#source-grounding) for
specifics; unlike the other three sections, `texts/` cites other repositories and external sources
directly, since explaining an interchange format requires it. File/line references may drift as
the code evolves; if something looks stale, check the live source at the paths cited.
