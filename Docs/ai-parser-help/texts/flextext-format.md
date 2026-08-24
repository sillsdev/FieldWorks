---
title: What FLExText is and how it's structured
grounded_in: FieldWorks DistFiles/Language Explorer/Export Templates/Interlinear/FlexInterlinear.xsd, Src/LexText/Interlinear/InterlinearExporter.cs, Src/LexText/Interlinear/InterlinVc.cs, Src/LexText/Interlinear/FlexInterlinModel/FlexInterlinear.cs
category: format
when_to_use: you have a .flextext file and don't know what the elements mean, or need to know which item type holds which piece of data
---

## What it is

FLExText (file extension `.flextext`) is FieldWorks Language Explorer's XML interchange format for
interlinear texts — connected corpus sentences with word-by-word glossing and morpheme
breakdowns, as opposed to the grammar (rules) that HermitCrab uses. It's consumed by FLEx itself
(round-trip export/import) and by other tools, notably [ELAN](https://archive.mpi.nl/tla/elan)
(ELAN → FLEx → ELAN "round-trip" workflows are a documented use case) and
[SayMore](https://software.sil.org/saymore/). A single `.flextext` file may hold multiple texts.

Internally, FieldWorks' generated C# serialization model renames the schema's root `<document>`
type to `BIRDDocument` — "the names have been changed to protect the innocent developers who
wouldn't like the looooong generated type names... document was changed to BIRDDocument"
(`FlexInterlinModel/FlexInterlinear.cs:11-13`). Other FieldWorks source files call the format "BIRD
format" in comments and class names (`BIRDInterlinearImporter.cs:65`, `LinguaLinksImport.cs:372`).
Nothing in the source names what the acronym expands to, so treat "BIRD format" as an internal
synonym for FLExText rather than a documented backronym.

The authoritative external documentation is SIL's own Ken Zook,
[*Technical Notes on FLEx Text Interlinear*](https://downloads.languagetechnology.org/fieldworks/Documentation/Technical%20Notes%20on%20FLEx%20Text%20Interlinear.pdf)
(May 2026) — it documents the same structure as the XSD below, plus the export/import UI workflow
and sample files from FLEx, ELAN, and SayMore. The format is also registered in CLARIN's Standards
Information System as
["SIL FieldWorks Language Explorer Interlinear Text"](https://standards.clarin.eu/sis/views/view-format.xq?id=fFLExText)
(media type `text/xml`, extension `.flextext`).

**Important:** exporting to FLExText does not read the FieldWorks project database directly. It
renders the currently *configured* Interlinear Text view (`Tools > Configure > Interlinear`) and
serializes that — see [`getting-started.md`](getting-started.md). A field you haven't enabled to
display won't appear in the export, even if it exists in the project.

## Document structure

The schema (`FlexInterlinear.xsd`, 231 lines) defines this element hierarchy:

```
document (version="3")
└─ interlinear-text (guid)           — one per text; a file may hold several
   ├─ item (type="title", ...)       — the text's title, one per writing system
   ├─ paragraphs
   │  └─ paragraph (guid)
   │     └─ phrases
   │        └─ phrase (guid, speaker, media-file, begin/end-time-offset)
   │           ├─ item (type="segnum"|"txt"|"gls"|"lit"|"note")
   │           └─ words
   │              └─ word (guid)
   │                 ├─ item (type="txt"|"gls"|"pos"|"punct")
   │                 └─ morphemes (analysisStatus)
   │                    └─ morph (type, guid)
   │                       └─ item (type="txt"|"cf"|"hn"|"gls"|"msa")
   ├─ languages
   │  └─ language (lang, encoding, font, vernacular)
   └─ media-files (offset-type)
      └─ media (guid, location)
```

(`FlexInterlinear.xsd:2-138` for `document`/`interlinear-text`/`paragraphs`/`phrases`/`words`/
`word`/`morphemes`/`morph`; `:95-126` for `languages`/`media-files`.)

A `word` element is one of three things depending on how far its analysis got:

- **Unanalyzed** — just `item type="txt"` (the surface wordform). No `morphemes`.
- **Partially analyzed** — has `morphemes`/`morph` breakdown, but usually no word-level `gls`.
- **Fully analyzed** — has `morphemes`, plus word-level `gls` (gloss) and `pos` (category).

This mirrors three different FieldWorks LCM object types the `word` guid can point to —
`WfiWordform`, `WfiAnalysis`, or `WfiGloss` respectively — though the flextext file itself doesn't
label which one applies; you infer it from which children are present. A fourth pseudo-word form,
punctuation, has no morphemes or gloss at all — just `item type="punct"` — and its `word` element
has no `guid`, even though the underlying FieldWorks `PunctuationForm` object does have one.

Word elements themselves are optional in phrases: if a phrase has a `txt` item but no `word`
children at all, FLEx will parse the baseline text into unanalyzed wordforms on import — one word
per whitespace-delimited token, which breaks for any wordform that legitimately contains a space
(FLEx supports multi-word wordforms like idioms — "kick the bucket" as one lexical unit — but only
if word elements are present to say so explicitly). (Zook's Technical Notes section 2.5 "Word.")

## The `item` element and its type system

Nearly every piece of actual data — text, gloss, category, translation, note — is carried by a
generic `item` element with a required `type` attribute (what kind of data) and a required `lang`
attribute (which writing system), e.g. `<item type="gls" lang="en">boy</item>`
(`FlexInterlinear.xsd:139-161`).

**The schema's enumerated list of item types is not exhaustive, and this is easy to miss.** The
XSD defines a `knownItemTypes` enumeration (`txt`, `cf`, `hn`, `variantTypes`, `gls`, `msa`, `pos`,
`title`, `title-abbreviation`, `source`, `comment`, `text-is-translation`, `description`, `punct`
— `:162-179`) but then unions it with plain `xs:string`
(`itemTypes = knownItemTypes ∪ xs:string`, `:180-182`) — so *any* string is schema-valid as a
`type` value. Item types FLEx actually emits that aren't in that enumeration include `segnum`
(segment number), `lit` (literal translation), and `note` — all three are real, used constantly,
and documented in Zook's Technical Notes section 2.4, but a validator or a naive reader treating
`knownItemTypes` as the complete list will miss them.

**The same type value means something different depending on which element it's nested under.**
This is the single most important gotcha for anyone (human or LLM) writing an XPath-style query
against a flextext file: `type="txt"` and `type="gls"` both recur at multiple structural levels
with different referents:

| Interlinear line (FLEx UI label) | Element | Item type | Meaning |
|---|---|---|---|
| Word | `word` | `txt` | the surface wordform |
| Word Gloss | `word` | `gls` | the whole word's gloss |
| Word Cat. | `word` | `pos` | the whole word's part of speech |
| Morphemes | `morph` | `txt` | one morpheme's surface form |
| Lex. Entries | `morph` | `cf` / `hn` | the lexeme (citation) form / homograph number |
| Lex. Gloss | `morph` | `gls` | that morpheme's *sense* gloss |
| Lex. Gram. Info. | `morph` | `msa` | that morpheme's grammatical category |
| Free Translation | `phrase` | `gls` | the sentence's free translation |
| Literal Translation | `phrase` | `lit` | the sentence's literal translation |
| Note | `phrase` | `note` | an annotator's note on the sentence |

(Table per Zook's Technical Notes section 3 "Mapping FLEx interlinear to flextext fields," verified
against the exporter: `InterlinearExporter.cs:185` writes `gls` for `WfiGlossTags.kflidForm`
(word gloss); `:433-435` and `:554` open a `gls` item for `InterlinLineChoices.kflidLexGloss` and
`WfiMorphBundleTags.kflidSense` respectively (morpheme sense gloss) — same type string, two
different source fields depending on nesting.)

A query like "get every `gls` item in the document" conflates three unrelated things: the
sentence's free translation, each word's gloss, and each morpheme's sense gloss. Always scope the
XPath to the level you mean — see [`llm-code-execution.md`](llm-code-execution.md) for concrete
scoped queries.

The `cf` type is a naming fossil worth knowing about if you're wondering why "citation form" isn't
called `cit` or `citform`: Zook's notes explicitly say *"the abbreviation was probably for citation
form at some point, but it is really the lexeme form, so should probably be 'lf'. However it's too
hard to change at this point"* (section 2.6) — `cf` holds the FieldWorks lexeme form
(`LexEntry LexemeForm MoForm Form`), not a distinct "citation form" concept.

## The `morph` element's `type` attribute

A `morph` element's own `type` attribute (distinct from any `item type=` inside it) names the
morpheme's structural class, taken directly from the FieldWorks `MoMorphType` object's `Name`
property (`InterlinearExporter.cs:152-154` writes `GetText(...get_MultiStringAlt(...))` for
whichever `MoMorphType` applies — i.e. it emits that object's actual configured name string, not a
fixed code). The XSD's `morphTypes` enumeration (`FlexInterlinear.xsd:192-214`) lists FieldWorks'
standard set: `particle`, `infix`, `prefix`, `simulfix`, `suffix`, `suprafix`, `circumfix`,
`clitic`, `enclitic`, `proclitic`, `bound root`, `root`, `bound stem`, `stem`, `infixing interfix`,
`prefixing interfix`, `suffixing interfix`, `phrase`, `discontiguous phrase`.

## guid linkage back to FieldWorks

Elements carry `guid` attributes that map back to specific FieldWorks LCM classes (Zook's Technical
Notes section 3):

| Flextext element | FieldWorks class |
|---|---|
| `interlinear-text` | `Text` |
| `paragraph` | `StTxtPara` |
| `phrase` | `Segment` |
| `word` | `WfiWordform` / `WfiAnalysis` / `WfiGloss` (depending on analysis depth) |
| `morph` (`type` attribute's guid) | `MoMorphType` |

Flextext deliberately omits the guids that would link a word's analysis back to specific lexicon
entries (`WfiMorphBundle`, `LexEntry`, `LexSense`, `MoStemAllomorph`) — per Zook's notes, "the
WfiMorphBundle guid and lexical guids are not included in the flextext file. As a result... current
imports of flextext files cannot provide these linkages during import in a blank FLEx project."
Treat a flextext file as recording *what* a word's analysis is (breakdown, glosses, category), not
a queryable link to *which specific lexicon entry* produced it.

**No engine provenance of any kind is recorded.** There is no field anywhere in the schema for
which parsing engine, rule, or trace produced a given analysis — a manually-typed analysis, an
analysis produced by FLEx's own HermitCrab-backed parser, and a statistically-guessed analysis are
structurally indistinguishable in a flextext file except via the coarse `analysisStatus` attribute
covered in [`analysis-status-and-ground-truth.md`](analysis-status-and-ground-truth.md) — which
records *whether a human confirmed it*, not *how it was produced*.

## Media and time-alignment fields

`phrase` elements can carry `begin-time-offset`/`end-time-offset` (millisecond strings) and a
`media-file` guid referencing a `media` element in the enclosing text's `media-files` block
(`FlexInterlinear.xsd:77-82, 113-126`). FLEx has no UI for this data but stores and round-trips it,
because ELAN and SayMore populate it for audio/video-aligned transcription — a flextext file
originating from ELAN or SayMore commonly has this, one from FLEx's own UI typically doesn't.

## A synthetic example

Invented language, invented words, invented sentence — not any real project's text (see this
directory's [README](README.md#what-belongs-here) for why that matters here specifically):

```xml
<?xml version="1.0" encoding="UTF-8"?>
<document version="3">
  <interlinear-text guid="00000000-0000-0000-0000-000000000001">
    <item type="title" lang="en">Toy Story 1</item>
    <paragraphs>
      <paragraph guid="00000000-0000-0000-0000-000000000002">
        <phrases>
          <phrase guid="00000000-0000-0000-0000-000000000003">
            <item type="segnum" lang="en">1</item>
            <item type="txt" lang="inv">Mirusi dabo.</item>
            <words>
              <word guid="00000000-0000-0000-0000-000000000004">
                <item type="txt" lang="inv">Mirusi</item>
                <morphemes analysisStatus="humanApproved">
                  <morph type="root" guid="00000000-0000-0000-0000-000000000005">
                    <item type="txt" lang="inv">miru</item>
                    <item type="cf" lang="inv">miru</item>
                    <item type="gls" lang="en">run</item>
                    <item type="msa" lang="en">v</item>
                  </morph>
                  <morph type="suffix" guid="00000000-0000-0000-0000-000000000006">
                    <item type="txt" lang="inv">-si</item>
                    <item type="cf" lang="inv">-si</item>
                    <item type="gls" lang="en">3sg.pst</item>
                    <item type="msa" lang="en">infl</item>
                  </morph>
                </morphemes>
                <item type="gls" lang="en">ran</item>
                <item type="pos" lang="en">v</item>
              </word>
              <word guid="00000000-0000-0000-0000-000000000007">
                <item type="txt" lang="inv">dabo</item>
                <morphemes analysisStatus="guess">
                  <morph type="root" guid="00000000-0000-0000-0000-000000000008">
                    <item type="txt" lang="inv">dabo</item>
                    <item type="cf" lang="inv">dabo</item>
                    <item type="gls" lang="en">quickly</item>
                    <item type="msa" lang="en">adv</item>
                  </morph>
                </morphemes>
                <item type="gls" lang="en">quickly</item>
                <item type="pos" lang="en">adv</item>
              </word>
              <word>
                <item type="punct" lang="inv">.</item>
              </word>
            </words>
            <item type="gls" lang="en">She ran quickly.</item>
            <item type="lit" lang="en">Ran quickly.</item>
            <item type="note" lang="en">Subject is understood from prior context.</item>
          </phrase>
        </phrases>
      </paragraph>
    </paragraphs>
    <languages>
      <language lang="inv" font="Charis SIL" vernacular="true" />
      <language lang="en" font="Times New Roman" />
    </languages>
  </interlinear-text>
</document>
```

Reading this against the tables above: the sentence's free translation is "She ran quickly." (the
phrase-level `gls`), the first word is fully analyzed and human-approved (root `miru` "run" + a
past-tense suffix), the second word's morpheme breakdown is only a guess (`analysisStatus="guess"`
on its `morphemes` element) even though it happens to be monomorphemic, and the final `word` is
punctuation with no morphology at all.
