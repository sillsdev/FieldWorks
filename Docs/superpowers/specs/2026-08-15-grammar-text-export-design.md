# Grammar and Text Export for AI Analysis

Status: Approved
Branch: `grammar-text-export`

## Goal

Add a new export option, "Export Grammar and Texts for AI Analysis," that writes
two things into a single folder the user picks:

1. The project's full **HC grammar** (HermitCrab-format grammar XML: phonology,
   morphology, features, categories — the same shape HC itself consumes to
   parse). Called "HC grammar," never bare "grammar," throughout this design
   and the code, to keep it distinct from the Grammar Area and from the
   existing, unrelated "Grammar Sketch" export (see Terminology below).
2. Every selected text in the project as a FLExText XML file (the same format
   the existing Interlinear export already produces per text).

The intent is a one-click bundle a user can hand to an LLM for linguistic
analysis, without having to run two separate exports and hunt for HC's
console tool.

## Where it appears

`Src\xWorks\ExportDialog.cs` (the generic Export dialog) builds its option
list by scanning every `.xml` file in
`DistFiles\Language Explorer\Export Templates\` for an `FxtDocumentDescription`
node. `RecordClerk.OnExport` opens this exact dialog from every tool area
except Notebook, Texts & Words, and Discourse Chart (each of which has its own
specialized export dialog pointed at its own template folder).

Consequence: dropping one new template file into that shared folder is
sufficient to surface "Export Grammar and Texts for AI Analysis" in the
Lexicon, Grammar, and other RecordClerk-based areas' Export dialogs, with **no
per-area command wiring**. It will not appear inside the Interlinear, Notebook,
or Discourse Chart dialogs — those are narrow, single-purpose dialogs for
their own formats, and neither "Grammar Sketch" nor "LIFT" appear there
either, so this is consistent with existing precedent.

### New template file

`DistFiles\Language Explorer\Export Templates\GrammarAndTextsForAI.xml`.
`dataLabel`/`formatLabel` populate the dialog's "Data"/"Format" columns; the
`FxtDocumentDescription` element's inner text is the description shown in the
panel below the list when the row is selected. It opens with the requested
wording ("Export Grammar and Texts for AI Analysis") verbatim, then a short
explanation of what gets written and two links: the raw-URL form of the new
`Docs/ai-parser-help/README.md` (see below) for pasting into an LLM chat, and
a normal `github.com/blob/...` URL to `getting-started.md` for a human reading
the dialog. See "AI Parser Help reference" below for where those docs and
links come from.

## AI Parser Help reference (`Docs/ai-parser-help/`)

The raw `HCGrammar.xml`/`.flextext` output alone isn't very interpretable by
an LLM without guidance — HermitCrab and FLExText both have real,
non-obvious gotchas (silent-misconfiguration defaults, performance cliffs,
the FLExText `analysisStatus` ground-truth caveat, etc.) that an LLM has no
way to know about on its own. The `sillsdev/machine` repo (which implements
HermitCrab) already has exactly this reference, authored on its
`docs/hc-llm-guide` branch as `docs/ai-parser-help/` — a self-contained,
44-file Markdown tree (`broken/`, `speed/`, `workflow/`, and `texts/`
sub-references, the last one specifically about FLExText and grounded in
FieldWorks' own exporter source) with no dependency on any code in the repo
it lives in. Every internal cross-reference is a same-tree relative link;
verified with a link-target-existence pass after copying (92 relative links,
4 anchor links, all resolved) — so it required zero link surgery to relocate.

Copied verbatim into `Docs\ai-parser-help\` in this repo (preserving the
`machine` repo's own copy, which stays as-is on its branch) so it ships
alongside the code that produces the files it explains, and so the export
dialog's description can point at a real, human-readable URL — an installed
FLEx has no access to this repo's dev-only `Docs/` folder at all, so the
description's references are necessarily GitHub URLs, not local paths.

## Dialog and export flow (`ExportDialog.cs`)

1. New `FxtTypes.kftGrammarTextsAI` enum value; `ConfigureItem`'s `type`
   switch gets a `case "grammarTextsAI"` mapping to it (same pattern as
   `kftGrammarSketch`, `kftPhonology`, etc.).
2. Selecting this row and clicking Export first opens a **new text-selection
   dialog** (see below), then a `FolderBrowserDialogAdapter` (the same idiom
   the existing LIFT export already uses for folder-based output), persisting
   the chosen folder via `PropertyTable` the same way `ExportDir` already is.
3. `DoExport`'s switch gets a `case FxtTypes.kftGrammarTextsAI` that runs a new
   `ExportGrammarAndTextsForAI(progress, outPath, selectedTexts)` task under
   the existing `ProgressDialogWithTask`/`RunTask` mechanism, one step per
   selected text plus one for the grammar file.

### Text-selection dialog

A new small dialog (not a reuse of the tree-based `FilterTextsDialog`, since
this needs a flat checkable list with numeric columns) listing every text in
the project:

| ☑ | Text | Words | Analyses |
|---|------|------:|---------:|

- **Words**: count of all word-token analyses in the text (`IAnalysis` items
  with `HasWordform == true`, across all paragraphs, i.e. excluding
  punctuation) — every token, whether it has been identified/analyzed or
  not, and including tokens whose wordform is unrecognized ("???"). Gives a
  raw "how much text is this" signal.
- **Analyses**: count of those same tokens whose `IAnalysis` is actually an
  `IWfiAnalysis`/`IWfiGloss` rather than a bare, unanalyzed `IWfiWordform` —
  whether *an* analysis has been attached (including an unreviewed parser
  guess), regardless of whether that analysis is linguistically correct.
  Gives a "how much of this have I actually analyzed" signal.
- Both counts are computed directly from each text's paragraphs/analyses
  (the same underlying data `StatisticsView.cs` already walks), independent
  of any rendering — cheap to compute for every text up front.
- Checkbox state defaults to the last-used selection (persisted via
  `PropertyTable` as a new key, same idiom as `ExportDir`); on first use,
  defaults to all checked.
- Canceling the dialog cancels the whole export.

## Output layout

Flat folder — no subfolder for texts:

```
<chosen folder>/
  HCGrammar.xml
  <text-title-1>.flextext
  <text-title-2>.flextext
  ...
```

Text filenames are the sanitized text title (invalid filesystem characters
stripped/replaced) with a numeric suffix on collision. `HCGrammar.xml` is
named to be self-disambiguating (not just "Grammar.xml") so it reads
unambiguously even sitting alone in a folder of `.flextext` files, distinct
from both the Grammar Area and the unrelated Grammar Sketch export.

## Grammar export mechanism

Reuses exactly what the existing `GenerateHCConfig` console tool
(`Src\GenerateHCConfig\Program.cs`) already does for a live project:

```csharp
Language language = HCLoader.Load(cache, logger);
XmlLanguageWriter.Save(language, Path.Combine(outPath, "HCGrammar.xml"));
```

Both types come from `SIL.Machine.Morphology.HermitCrab`, reached via
`Src\LexText\ParserCore\HCLoader.cs`. This is the real HC grammar XML — not
the human-readable "Grammar Sketch" document, which is an unrelated
publishing format produced by a different code path
(`ExportDialog.ExportGrammarSketch` → `SaveAsWebpage` publisher event).
`HCLoader.Load` is a pure, synchronous, read-only transform over the cache —
it has no interaction with any live/running HC engine. The in-app parser
calls this exact same function itself (`HCParser.cs:157`) whenever it needs
to reload its own grammar, so this export is simply one more caller of an
existing pure function, not a new kind of interaction with HC.

`xWorks.csproj` needs a new `ProjectReference` to
`Src\LexText\ParserCore\ParserCore.csproj` to reach `HCLoader`/`Language`/
`XmlLanguageWriter`. Verified no circular dependency: `ParserCore.csproj`
does not reference `xWorks.csproj` (only `ParserUI.csproj`, one layer up,
references both).

`HCLoader.Load` internally catches per-item linguistic problems (invalid
phonemes, invalid environments, invalid affix processes, etc.) and routes
them to its `IHCLoadErrorLogger` argument, skipping the bad item and
continuing — so it always returns *some* `Language` object for a
structurally valid project. A new minimal `IHCLoadErrorLogger`
implementation collects these into a list instead of surfacing them modally
mid-export; they're shown in a single summary after the whole export
completes (a partial/imperfect HC grammar is still useful for AI analysis).

An *unhandled* exception out of `HCLoader.Load`/`XmlLanguageWriter.Save` is a
different matter — since per-item problems are already caught internally,
anything that escapes indicates a real bug, not messy linguistic data.
**Decision: an unhandled exception here aborts the entire export** (grammar
and texts both) rather than being swallowed so the texts still get written —
producing a folder of `.flextext` files with a silently-missing
`HCGrammar.xml` and no clear explanation would be worse than failing loudly.
This is asymmetric with the per-text tolerance policy below, deliberately:
per-text failures are independent, isolated operations where partial success
is meaningful; a crash out of the grammar step is not.

## Text export mechanism

For each checked text, reuses the existing headless FLExText path already
exercised by `InterlinearExporterTests.cs` (no live view/rootsite required):

```csharp
using (var vc = new InterlinVc(cache))
using (var writer = XmlWriter.Create(path, settings))
{
	vc.LineChoices = InterlinLineChoices.DefaultChoices(cache.LangProject, cache.DefaultVernWs, cache.DefaultAnalWs);
	var exporter = InterlinearExporter.Create("xml", cache, writer, stText, vc.LineChoices, vc);
	exporter.WriteBeginDocument();
	exporter.ExportDisplay();
	exporter.WriteEndDocument();
}
```

A per-text failure is caught, logged, and skipped (does not abort the whole
export) — consistent with the tolerant, best-effort spirit of the grammar
export above; failures are named in the same final summary.

### Architecture constraint: this code cannot live in `ExportDialog.cs` directly

`InterlinVc`/`InterlinLineChoices`/`InterlinearExporter` live in
`Src\LexText\Interlinear\ITextDll.csproj`. That project already has a
`ProjectReference` to `xWorks.csproj` (it subclasses `ExportDialog` for
`InterlinearExportDialog`), so `xWorks.csproj` cannot add a `ProjectReference`
back to `ITextDll.csproj` — that would be a genuine build-breaking cycle, not
just a style preference.

The codebase already has an established, precedented way around exactly this
shape of problem: a **globally-registered `IxCoreColleague` listener**,
declared in `DistFiles\Language Explorer\Configuration\Main.xml`'s
`<listeners>` section and loaded by assembly/class name (so no compile-time
reference is needed), answering a `Publisher`/`Subscriber` event synchronously
via a mutable collector object passed as the published parameter.
`ExportDialog.EnsureViewInfo()` already uses this exact idiom (publishing
`EventConstants.GetContentControlParameters`, answered by `AreaListener` in
`LexTextDll.dll`, registered the same way) to reach cross-DLL data without a
direct reference. This export reuses the same idiom:

1. A new constant `EventConstants.ExportTextsAsFlexText` and a new small
   request/result class `ExportTextsAsFlexTextRequest` (holding the texts to
   export, the output folder, and a mutable failures list) both live in
   `Src\Common\FwUtils\` — the common project both `xWorks` and `ITextDll`
   already reference, same as `EventConstants` itself does today.
2. `ExportDialog`'s new export task publishes that event with a populated
   request object, then reads back `request.Failures` once `Publish` returns
   (synchronous, same call stack — not the deferred `PublishAtEndOfAction`
   variant).
3. A new listener class, `FlexTextAIExportListener`, lives in `ITextDll`
   (`Src\LexText\Interlinear\`) where `InterlinVc`/`InterlinearExporter` are
   natively reachable, subscribes to that event in `Init`, and does the
   actual per-text export shown above.
4. `Main.xml`'s `<listeners>` section gets one new line registering it,
   exactly like the existing `AreaListener`/`FLExBridgeListener` entries.

This keeps `xWorks.csproj` free of any new `ProjectReference` for the text
half of the feature (only the grammar half needs one, to `ParserCore`, which
has no such cycle).

## Error handling / progress

- Runs under `ProgressDialogWithTask` like the other custom export types
  (`kftLift`, `kftPhonology`, `kftGrammarSketch`), with `AllowCancel = true`.
- HC-grammar-load warnings and any per-text export failures are collected and
  shown as one summary `MessageBox` after the run, rather than interrupting
  it — matches how the codebase already tolerates partial/imperfect grammars
  elsewhere (e.g., the parser itself runs against incomplete grammars). An
  unhandled exception from the HC-grammar step is the one thing that aborts
  the whole export (see above).
- The existing "Show in folder" checkbox on `ExportDialog` opens the chosen
  folder afterward (wired the same way the LIFT export already wires it).

## Concurrency with the live parser (accepted, documented risk)

`ProgressDialogWithTask.RunTask` shows a **modal** progress dialog on the UI
thread while running the actual export work on a separate `BackgroundWorker`
thread. That modal dialog blocks the *user* from starting new edits, but it
does **not** block FieldWorks' own in-app parser: `ParserConnection` /
`ParserScheduler` (`Src\LexText\ParserUI\`) runs in-process against the same
`LcmCache`, processing its queue via an `IdleQueue` tied to the UI thread's
`Application.Idle` event — and modal `ShowDialog()` loops still raise
`Application.Idle` in WinForms. So if the parser's queue isn't empty
(ordinary interlinear editing queues wordforms continuously, and "Parse All
Words"/"Reparse All Words" run for a while), it can be actively mutating
wordform analyses in the same cache this export's background thread is
concurrently reading — for both the HC-grammar step and, more
consequentially, the per-text FLExText export.

**Decision: do not add a guard.** This exact exposure already exists,
unmitigated, for every sibling export in this codebase — LIFT, Phonology,
Grammar Sketch, and the existing per-text FLExText export in
`InterlinearExportDialog` all read the live cache from a background thread
with no check for parser activity, and nothing in the codebase locks or
guards against it today. Adding a bespoke guard to only this export would be
inconsistent with that precedent and out of proportion with the actual
(apparently long-accepted) risk. This rationale is called out explicitly
here — and will be restated in the PR description — specifically because a
reviewer could reasonably expect an "AI analysis" export to be more
defensive about data consistency than a casual one; the answer is that it
inherits the same level of protection (none) as every export beside it,
by deliberate choice, not by oversight.

## Out of scope

- No XSLT/format customization for this export type — the FLExText and HC
  XML shapes are fixed, matching what HC/Interlinear already produce
  elsewhere.
- No incremental/diffing re-export.
- No zipping/compressing the output folder.
- Scripture-linked texts are simply rows in the selection list like any
  other text (no special-casing) — the user controls inclusion via the
  checkbox, so no separate Scripture policy is needed.

## Testing plan

- Unit tests for the new word/analysis counting helper against a small
  in-memory text fixture (mirrors existing `ITextDllTests` patterns).
- Unit test for filename sanitization/collision handling.
- Integration-style test that runs the grammar+text export against an
  existing test project and asserts: `HCGrammar.xml` is well-formed HC XML,
  one `.flextext` file per selected text is written and is well-formed,
  unchecked texts are excluded.
- Manual verification in the running app (`fieldworks-winapp` skill): confirm
  the new row appears in the Lexicon and Grammar areas' Export dialogs, the
  selection dialog's counts look sane against a known test project, and the
  folder picker/export/"show in folder" flow works end-to-end.
