---
title: analysisStatus distinguishes confirmed analyses from guesses
grounded_in: FieldWorks DistFiles/Language Explorer/Export Templates/Interlinear/FlexInterlinear.xsd, Src/LexText/Interlinear/InterlinVc.cs, Src/LexText/Interlinear/InterlinearExporter.cs, Src/LexText/Interlinear/ITextDllTests/ImportInterlinearAnalysesTests.cs
category: provenance
when_to_use: before treating any word's morpheme breakdown, gloss, or category in a .flextext file as an established fact about the language
---

## What it is

Every word in a FieldWorks interlinear text has gone through some amount of analysis -- assigning
it a morpheme breakdown, a gloss, a part of speech -- but not all of that analysis has necessarily
been reviewed by a person. FLEx's own automatic analysis-guesser can populate a word with its best
guess before a linguist has confirmed anything. The `analysisStatus` attribute is how a flextext
file records which is which.

## Where it appears

The XSD defines `analysisStatusTypes` as an enumeration of four values --
`humanApproved`, `guess`, `guessByHumanApproved`, `guessByStatisticalAnalysis`
(`FlexInterlinear.xsd:222-229`) -- and allows the attribute in two places:

- On a word's `morphemes` element (`:64`, `:912-913`) -- status of the morpheme breakdown as a
  whole.
- On any `item` element (`:159`, `:1018`) -- in practice, only on `gls` and `pos` items
  (`InterlinearExporter.cs:351, 617`: `WriteAnalysisStatus()` is called specifically when
  `itemType == "gls" || itemType == "pos"`), i.e. on a word's gloss and category, not on arbitrary
  item types.

## What FLEx's own exporter actually emits

Reading `InterlinVc.cs` (the view constructor that both renders the Interlinear window and drives
the exporter) shows only **one** of the four enumerated values is ever written by FLEx's own
export path: the literal string `"guess"` -- six call sites, all of the same shape:

```csharp
// InterlinVc.cs:2562 (one of six near-identical sites: lines 2562, 2574, 2597, 2636, 2659, 2677)
m_this.SetGuessing(m_vwenv, m_this.GetGuessColor(m_defaultObj));
// Let the exporter know that this is a guessed analysis.
m_vwenv.set_StringProperty(ktagAnalysisStatus, "guess");
```

This fires when the interlinear view is displaying a *default* analysis that hasn't actually been
selected/approved by the user -- the same guessed-analysis state FLEx's UI renders with a distinct
color to flag it as unconfirmed. When a word's analysis **has** been approved, no
`analysisStatus` attribute is written at all -- **absence of the attribute means human-approved (or
the only candidate), not "no information."** So on a file exported directly from FLEx's own UI,
you should only expect to see two states in practice: attribute absent (approved) and
`analysisStatus="guess"` (FLEx's own guesser, unconfirmed).

The other two enumerated values, `guessByHumanApproved` and `guessByStatisticalAnalysis`, are not
emitted by this exporter -- but they are real, meaningful values the *importer* understands, and
they do appear in FieldWorks' own test fixtures for round-tripping data from other sources (e.g.
`ITextDllTests/FlexTextImport/FlexTextExportOutput.flextext:11,15,19,23,27`, which uses
`analysisStatus="guessByStatisticalAnalysis"` and `="guessByHumanApproved"` on `gls` items). A
flextext file from a different tool or pipeline -- say, one that ran a statistical
tagger/analyzer over the text -- may use these two more granular "guessed, but derived from [some
other confirmed source]" states. Don't assume every flextext file you see only has the two states
FLEx's own UI produces.

## Why it matters: the importer doesn't trust guesses either

FieldWorks' own import logic treats this distinction the same way you should. A test named
`SkipNewGuessedWordGloss` (`ImportInterlinearAnalysesTests.cs:208-224`) imports a word gloss marked
`analysisStatus="guessByHumanApproved"` and confirms FLEx does **not** create a new approved
analysis from it -- contrast with the neighboring `ImportNewHumanApprovedWordGloss` test
(`:115-131`), which imports the identical structure but with `analysisStatus="humanApproved"` and
confirms it **does** get created as approved data. Same shape, same fields -- the only difference
that changes the outcome is this attribute.

The practical implication for reasoning about a `.flextext` file with an LLM: if you (or the LLM)
are trying to draw a linguistic conclusion from the corpus -- "this language's plural suffix is
always `-X`," "this root never co-occurs with that affix" -- restrict that reasoning to analyses
that are actually `humanApproved` (attribute absent) or explicitly marked `humanApproved`. A
conclusion drawn from `guess`-flagged data is only as reliable as FLEx's automatic guesser, which
is exactly the caveat FieldWorks' own import code encodes by refusing to promote a guess to
approved status on its own. Extraction code that reports counts of confirmed vs. guessed analyses
(see [`llm-code-execution.md`](llm-code-execution.md)) exists specifically so this distinction
isn't silently lost when handing the file to an LLM.
