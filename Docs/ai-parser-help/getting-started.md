# Get help from ChatGPT or Claude with your HermitCrab grammar

Part of the [AI Parser Help reference](README.md). This page is for anyone with a FieldWorks
Language Explorer (FLEx) project who wants an LLM's help with their HermitCrab parser/grammar —
whether it's broken ("why won't this word parse," "why do I get a wrong analysis"), slow ("why
is this so slow," "why do I get 500 analyses for one word"), or a modeling question ("how should
I represent this in my grammar").

Want help with real **interlinear texts** (`.flextext` files) instead of, or alongside, the
grammar itself? See [`texts/getting-started.md`](texts/getting-started.md) — extracting and
reasoning about connected corpus texts is a different workflow from the one below.

## Step 1 — Extract your grammar as HermitCrab XML

FieldWorks ships a tool, `GenerateHCConfig.exe`, that exports your project's grammar as the
HermitCrab XML format (the same format HermitCrab itself parses from).

**First, close the project in FLEx.** The tool loads the project file directly and fails
with "currently open in another application" if FLEx (or anything else) still has it open.

**Find the tool.** It's installed next to `FieldWorks.exe` itself, not on your PATH, and the
exact folder depends on your FieldWorks version. Easiest way to find it — paste this into
PowerShell:

```powershell
Get-ChildItem "C:\Program Files\SIL\FieldWorks*\GenerateHCConfig.exe", `
              "C:\Program Files (x86)\SIL\FieldWorks*\GenerateHCConfig.exe" `
    -ErrorAction SilentlyContinue
```

That prints the full path (e.g. `C:\Program Files\SIL\FieldWorks 9\GenerateHCConfig.exe`).
If it prints nothing, your project's `RootCodeDir` registry override points somewhere else —
search for `GenerateHCConfig.exe` under wherever FieldWorks itself is installed.

**Run it** against your project's `.fwdata` file:

```powershell
& "C:\Program Files\SIL\FieldWorks 9\GenerateHCConfig.exe" "C:\path\to\YourProject.fwdata" "C:\path\to\YourProject-hc.xml"
```

(use the actual path `Get-ChildItem` printed above, and your own project's `.fwdata` path —
typically under `Documents\My FieldWorks\<project name>\`)

This produces the second file, `YourProject-hc.xml` — that's your grammar.

## Step 2 — Copy the XML into ChatGPT or Claude

Open the exported XML file, copy its contents, and paste them into your chat with ChatGPT
or Claude as the first message (or attach the file directly if your chat supports file
uploads — for a large grammar this is more reliable than pasting inline).

## Step 3 — Point it to the HermitCrab reference

Paste this URL into the same chat:

```
https://raw.githubusercontent.com/sillsdev/machine/master/docs/ai-parser-help/README.md
```

This tells the LLM where to find the (non-proprietary) documentation of how the HermitCrab
engine itself works — rule ordering, affix templates, features, correctness pitfalls, complexity
pitfalls, and grammar-authoring methodology — so it can reason about *your* grammar against the
actual engine mechanics rather than guessing. If you already know your question is about
wrongness, speed, or modeling approach specifically, you can paste the more specific
`broken/README.md`, `speed/README.md`, or `workflow/README.md` URL instead.

## Step 4 — Ask your question

Some examples of what to ask, once both your grammar and the reference URL are in the chat:

- "Why does the word `<surface form>` fail to parse / parse with the wrong analysis?"
- "Why is parsing this word so slow? Is there a combinatorial-explosion pattern in my
  affix templates like the optional-slot one described in the reference?"
- "I have a slot with N optional prefixes, most of them null — is there a better way to
  model this in my grammar?"
- "How should I model a class of nouns whose plural is irregular, given HermitCrab's
  mechanisms?"
- "Walk through how stratum `<name>` would apply to the stem `<form>`."

## A note on privacy

Your grammar file is real linguistic data about a real language project. Pasting it into a
third-party chat service sends that data to that provider (OpenAI, Anthropic, etc.). Check
your project's data-sensitivity policy before sharing an unpublished or restricted grammar
this way — this is a separate concern from (and in addition to) the fact that real grammars
must never be committed to this repository.
