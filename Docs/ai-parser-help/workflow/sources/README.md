# Primary sources

Verbatim primary-source material this guide's `workflow/` section distills from and cites. Kept
here as full text (not just summarized) so an LLM reading this guide has the original to check
claims against, not just a paraphrase.

- **`black-flex-conceptual-intro-fulltext.txt`** — "A Conceptual Introduction to Morphological
  Parsing for FieldWorks Language Explorer," H. Andrew Black (SIL), 3 July 2025. Ships as a Help
  file with FieldWorks 9 (`Helps/WW-ConceptualIntro/ConceptualIntroFLEx.pdf` under the FieldWorks
  install directory). Extracted verbatim via `pdftotext -layout`; page markers are PDF page
  numbers.
- **`black-parser-workshop-2026-L02-fulltext.txt`** — SIL 2026 Parser Workshop, session L02
  (H. Andrew Black). Same underlying mechanisms as the conceptual intro, but framed around the
  order a person actually works through them and FLEx's UI/parse-state semantics.

**Provenance note:** this material properly belongs alongside FieldWorks itself (it already ships
there as a Help file) — it's included in this repo for now as a practical staging point, not
because `machine` is its long-term home. If/when it's relocated to the FieldWorks repo, update the
citations in `workflow/` to point there instead.

Nothing in `workflow/`'s own pages is a copy of this text — those pages are original guidance,
independently checked against the actual HermitCrab engine source, that cites specific sections
here for a reader who wants Black's own words.
