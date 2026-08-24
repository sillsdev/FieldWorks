---
title: Getting an LLM to run code against your uploaded .flextext file
grounded_in: external product documentation for ChatGPT, Claude.ai, and Google AI Mode/Gemini (cited inline)
category: llm-tooling
when_to_use: your .flextext file is too large to reliably read inline, or you want structured, checkable extraction instead of the LLM's own reading of raw XML
---

## Why this matters

A `.flextext` export of even a modest text runs to hundreds or thousands of lines of nested XML
(see [`flextext-format.md`](flextext-format.md) for the structure). Asking an LLM to just "read"
that file and answer questions about it means trusting its own attention over a long, repetitive,
deeply-nested document — the same reliability problem as asking it to eyeball a large HermitCrab
grammar XML. The fix is the same: if the product can actually execute code against the uploaded
file, have it run a short, deterministic extraction script and work from that output instead of
its own reading of the raw file.

Not every AI product we checked can do this. Below is what we verified about each, from that
product's own documentation, as of the time this was written.

## ChatGPT — yes, Python, verified

ChatGPT's Code Interpreter / Advanced Data Analysis feature runs Python in a stateful sandboxed
Jupyter environment and can read files uploaded to the chat directly from that environment.
Per OpenAI's own help center: *"For some data-analysis tasks, ChatGPT writes and runs Python code
in a stateful Jupyter notebook environment... The environment can use files made available to the
session"* (["Data analysis with ChatGPT"](https://help.openai.com/en/articles/8437071-data-analysis-with-chatgpt),
["File Uploads FAQ"](https://help.openai.com/en/articles/8555545-code-interpreter)). Python's
`xml.etree.ElementTree` is standard library — no install step needed. Use the script below.

## Claude.ai — yes, Python, verified (superseded an earlier JS-only tool)

As of this writing, Claude.ai's code execution tool runs **Python and Bash** in a sandboxed
container, available by default across Free/Pro/Max/Team/Enterprise plans, and can read files
uploaded to the conversation directly
(["Create and edit files with Claude"](https://support.claude.com/en/articles/12111783-create-and-edit-files-with-claude);
the underlying tool is documented for API use at
["Code execution tool"](https://platform.claude.com/docs/en/agents-and-tools/tool-use/code-execution-tool),
which confirms a Python sandbox with pandas/numpy/matplotlib preinstalled and direct access to
uploaded files). This **replaces** an earlier (2024) version of the tool that only ran JavaScript
in an in-browser Web Worker with a much smaller set of libraries (lodash, Papa Parse, no XML/DOM
parsing capability at all) — if you've used Claude's analysis tool before and remember it as
JS-only, that has changed. The Python script below works identically here as in ChatGPT, since
`xml.etree.ElementTree` is standard library in both.

## Google AI Mode / Gemini — not verified; don't assume code execution here

We could not confirm, from Google's own documentation, that either Google Search's **AI Mode** or
the consumer **Gemini app** (gemini.google.com) exposes a code-execution sandbox to the end user
over an arbitrary uploaded file the way ChatGPT and Claude do:

- **AI Mode**'s file-upload feature is described only as reading the file directly and
  cross-referencing it with web results — *"AI Mode will analyze the contents of your file and
  cross-reference it with relevant information from the web to provide a helpful AI response"*
  ([Google's AI Mode update announcement](https://blog.google/products/search/ai-mode-updates-back-to-school/)).
  At the time of that announcement, supported upload types were images and PDFs specifically, with
  more types promised later — no mention of running code against the file.
- The consumer **Gemini Apps** help page describes uploading and getting "answers, summaries, and
  insights" about a file's content, but does not describe a code-execution step
  (["Upload & analyze files in Gemini Apps"](https://support.google.com/gemini/answer/14903178)).
- Google **does** document a Python code-execution tool, with stdlib and numpy/pandas/matplotlib
  available, that can process uploaded CSV/text files — but this is documented as a **Gemini API**
  / developer-facing tool (["Code execution | Gemini API"](https://ai.google.dev/gemini-api/docs/code-execution)),
  not something we could confirm is what powers file uploads in the consumer AI Mode or Gemini
  chat products.

If you're using AI Mode or the Gemini app, treat it as reading the raw XML directly rather than
running code against it — the same reliability caveat that motivates this page in the first place.
If you need the code-execution workflow, use ChatGPT or Claude for the extraction step; you can
still paste the *extracted, compact* summary those produce into a Gemini conversation afterward.

## The extraction script

Paste this into ChatGPT or Claude and ask it to run the script against your uploaded file
(adjust the filename to match what you uploaded):

```python
import xml.etree.ElementTree as ET

PATH = "YourText.flextext"  # change to your uploaded file's name

def text_of(elem, xpath):
    child = elem.find(xpath)
    return child.text if child is not None else None

tree = ET.parse(PATH)
root = tree.getroot()

approved = 0
guessed = 0

for itext in root.findall("interlinear-text"):
    title = text_of(itext, "./item[@type='title']") or "(untitled)"
    print(f"=== {title} ===")

    for para in itext.findall("./paragraphs/paragraph"):
        for phrase in para.findall("./phrases/phrase"):
            baseline = text_of(phrase, "./item[@type='txt']")
            free = text_of(phrase, "./item[@type='gls']")       # phrase-level = free translation
            literal = text_of(phrase, "./item[@type='lit']")    # phrase-level = literal translation
            print(f"\n{baseline!r}")
            print(f"  free: {free!r}   literal: {literal!r}")

            for word in phrase.findall("./words/word"):
                surface = text_of(word, "./item[@type='txt']")
                punct = text_of(word, "./item[@type='punct']")
                if punct is not None:
                    print(f"  [punct] {punct!r}")
                    continue

                word_gloss = text_of(word, "./item[@type='gls']")   # word-level gloss
                pos = text_of(word, "./item[@type='pos']")          # word-level category
                morphemes = word.find("./morphemes")
                status = morphemes.get("analysisStatus") if morphemes is not None else None
                if status is None:
                    approved += 1
                else:
                    guessed += 1

                morph_strs = []
                if morphemes is not None:
                    for morph in morphemes.findall("morph"):
                        m_txt = text_of(morph, "./item[@type='txt']")
                        m_gls = text_of(morph, "./item[@type='gls']")   # morph-level = sense gloss
                        m_msa = text_of(morph, "./item[@type='msa']")   # morph-level = category
                        morph_strs.append(f"{m_txt}({morph.get('type')})={m_gls}/{m_msa}")

                status_label = status or "approved"
                print(f"  {surface!r} [{pos}] '{word_gloss}' <{status_label}>  morphs: {morph_strs}")

print(f"\n--- {approved} word analyses approved, {guessed} guessed/unconfirmed ---")
```

This prints, per phrase: the baseline text, free and literal translations, and per word: surface
form, category, gloss, its `analysisStatus`, and its morpheme breakdown with each morpheme's own
gloss/category — with translation-vs-word-gloss-vs-morpheme-gloss kept separate, per the scoping
gotcha in [`flextext-format.md`](flextext-format.md#the-item-element-and-its-type-system). It
also tallies how many word analyses are approved vs. guessed, so you get the
[ground-truth caveat](analysis-status-and-ground-truth.md) as a number up front, not something you
have to remember to ask about separately.

Ask the LLM to adapt the script for your actual question — e.g. filtering to only guessed
analyses, tallying parts of speech, or listing every distinct morpheme with its gloss — rather
than treating it as fixed. The point is to have the LLM write and run a *targeted* query against
the structure, not to make it read the whole file itself.
