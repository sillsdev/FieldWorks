# Lessons Ledger and Skill-Update Protocol

The fieldworks-* migration skills only stay useful if every migration
updates them. This file defines (a) the routing table that maps each kind
of discovery to the exact place to record it, and (b) an append-only ledger
of migration retrospectives so future agents can see how the skill set
evolved and why.

## Update protocol — where each discovery goes

Run this at the end of every migration (workflow step 10 in SKILL.md), and
immediately whenever you hit a stale pointer mid-task. Make the edits in
the same PR as the migration.

| You discovered… | Update |
| --- | --- |
| A new architectural pattern, or a refinement of an existing one | `architecture-patterns.md` — add/extend the numbered section (decision, why, canonical code, gotchas); add a row to the SKILL.md quick map if it is load-bearing |
| A new seam contract | `seam-catalog.md` §1/§2 plus its pivot trigger in §3 |
| A pivot trigger fired (decision re-evaluated) | Record the outcome inline in `seam-catalog.md` §3 and summarize in the ledger below |
| A new plugin for a custom slice class | `architecture-patterns.md` §5 canonical-code list; keep the burn-down test list current |
| A new gotcha / failure mode (interop, DPI, fonts, focus, threading, lifetime…) | The Gotchas paragraph of the matching `architecture-patterns.md` section; if it is a review smell, also add a red flag to the most relevant satellite skill |
| A new forbidden legacy symbol | `EngineIsolationAuditTests.cs` (the enforcement) and `parity-evidence.md` §4 (the documentation) — both in the same PR |
| A new evidence lane, artifact type, or evidence-language term | `parity-evidence.md` |
| A new mandatory step in the per-region process | `migration-checklist.md` (and the workflow list in SKILL.md if it is a new phase) |
| A trigger phrase that failed to invoke a skill when it should have | The `description` frontmatter of that skill — add the missing vocabulary; keep descriptions quoted (YAML colons) and third-person |
| A stale file pointer (file moved/renamed, openspec change archived) | Fix the pointer in whichever skill file holds it; prefer pointing at code and tests over change docs |
| Updated performance baselines | `DataTreeTimingBaselines.json` stays the source of truth; update budget notes in `parity-evidence.md` §5 only if the policy (not the numbers) changed |

Rules of thumb:

- **Skills point, references explain, openspec records provenance.** Do
  not paste large doc content into skills; capture the durable decision and
  point at code/tests, citing the openspec doc as provenance.
- **Generalize before writing.** Record the class of problem, not the
  one-off instance. If it only applies to one region, it goes in that
  region's openspec change, not here.
- **Prune as you add.** If a section no longer pays for its tokens
  (pattern superseded, gotcha fixed at the framework level), delete or
  collapse it. Skills are working memory, not an archive — the archive is
  git history and openspec.
- **Keep SKILL.md bodies under ~150 lines** and references one level deep
  from SKILL.md. If a reference outgrows ~300 lines, split it by domain
  and update the pointers.

## Ledger

Append one entry per completed migration (newest first). Keep entries to
~10 lines: link to the change, what was migrated, what was learned, which
skill files changed.

### 2026-06 — §19e remaining detail-editor field types to parity

- Change: `openspec/changes/lexical-edit-avalonia-migration/field-types-test-research.md`.
- Migrated: dedicated `RegionFieldKind` editors for enum closed-combo (closed
  `ComboBox`, rejects free text), integer (numeric `TextBox` that rejects non-numeric
  keystrokes + reject-and-restore), GenDate qualifiers (new `FwGenDateField`: year +
  precision Before/On/About/After + era AD/BC, composing a `GenDate.TryParse` long-string)
  and exact-date calendar picker (`CalendarDatePicker` beside the parse-on-commit text box),
  literal/"lit" (static `TextBlock`), jtview nested-layout recursion (`WalkEmbeddedView`),
  and per-field WS visibility (`visibleWritingSystems`). Touched
  `RegionFieldKind`/`RegionFieldControlFactory`/`EditorKindMap`(no change needed — already
  classified)/`XmlLayoutImporter`/`ViewNode`/`FullEntryRegionComposer` + strings + tests.
- Learned: (1) probe LCModel string grammars from the DLL before composing — `GenDate.ToLongString`
  emits "About AD 1985"/"After 500 BC"/"AD 1990" and `TryParse` rejects "circa"/"about 1990",
  so the structured editor must emit the canonical word order (precision word + AD-prefix /
  year + BC-suffix), NOT free synonyms. (2) Avalonia `CalendarDatePicker.SelectedDate`
  raises `SelectedDateChanged` for the INITIAL programmatic seed (deferred, during headless
  layout) — guard with a remembered last-date value-compare, not a timing flag, so only a
  genuine user pick stages. (3) Changing an editor's returned control shape (date text box →
  text+calendar panel; GenDate → structured editor) breaks every test that cast
  `Build(...)` straight to `TextBox` — search `(TextBox)` / `InstanceOf<TextBox>` on the
  affected kind first and extract via `GetVisualDescendants`. (4) Enum/integer were already
  functionally safe as Chooser/Text in the composer; the §19e value was a DEDICATED kind so
  the rejection is visible at the editor and the dispatch is explicit (no free-form regression).
- Skill files changed: this ledger entry. Architecture/seam patterns unchanged (new kinds
  ride the existing one-switch `RegionFieldControlFactory` dispatch; importer/composer seams
  extended, not restructured).

### 2026-06 — §19d audio (voice WS) + pictures (CmPicture) editable parity

- Change: `openspec/changes/lexical-edit-avalonia-migration/media-pictures-test-research.md`.
- Migrated: pictures from read-only thumbnail to insert/replace/delete + caption/
  description/license/creator metadata, plus the picture ORC into rich text
  (closes §19c's deferral); audio (IsVoice) from a blanket read-only placeholder to
  play/record/clear. New seam methods on `IRegionEditContext`
  (`TryInsertPicture`/`TryReplacePictureFile`/`TryDeletePicture`/`TrySetPictureMetadata`/
  `TryInsertPictureOrc`); a new LCModel-free media seam `IRegionMediaServices`
  (file pick / picture-properties dialog / audio play+record) implemented in xWorks
  as `LcmRegionMediaServices`; picture LCModel writes centralized in
  `RegionPictureEditor`; audio rides the existing text setter (a voice alternative is
  a multistring alt whose text is the filename).
- Learned: (1) audio play/record uses libpalaso `SIL.Media.AudioFactory`/
  `ISimpleAudioSession` (the same device the legacy `ShortSoundFieldControl` uses;
  NAudio underneath, Linux via Alsa) — prefer it over hand-rolled NAudio; cross-platform
  record availability is reported by `ISimpleAudioSession.CanRecord`, gated in the UI.
  (2) `PalasoImage`/ClearShare `Metadata` (`Creator`/`CopyrightNotice`/
  `SaveUpdatedMetadataIfItMakesSense`) live in `SIL.Windows.Forms.ImageToolbox` —
  caption/description are real `ICmPicture` LCModel multistrings (testable on an
  in-memory cache with no file); license/creator are file metadata (best-effort, real
  file only). (3) Adding a method to `IRegionEditContext` breaks ~6 direct implementers
  (incl. test fakes) — net48/C#7.3 has no default interface methods, so budget for
  touching every implementer. (4) Picture insert into rich text is cleanest via
  `ICmPicture.InsertORCAt(tss, ich)` (canonical guid/ORC encoding) then write the TsString
  back through the existing rich-text setter — never hand-encode `ktptObjData`.
- Skill files changed: this ledger entry. Architecture/seam patterns unchanged (the new
  picture/audio methods extend the existing edit-context seam; `IRegionMediaServices` is a
  host-side media seam analogous to the existing dialog-launcher service seam).

### 2026-06 — Entries browse-table rendering cutover + headless integration harness

- Change: `openspec/changes/shared-editable-virtualized-table/`
  (`rendering-cutover-design.md`, `headless-integration-harness.md`).
- Migrated: the lexicon Entries table off the native C++ Views rendering for its
  surface — owned WS-aware cell renderer (`BrowseCellRenderer`), rich-cell value
  source via `RegionRichTextAdapter.FromTsString`, and clerk-routed sort/filter
  (`BrowseViewer.MakeColumnSorter`/`MakeColumnFilter` → `Clerk.OnSorterChanged`/
  `OnChangeFilter`) replacing the lossy string mirror and the client-side filter
  projection. Legacy `BrowseViewer` still constructed underneath (F1); its
  retirement is F2/Stage-13.
- Key lessons now encoded: **headless integration scenario tests are the
  front-and-center verification style** (new architecture-patterns.md §13;
  parity-evidence.md §2a + the "live-verification-only" downgrade;
  migration-checklist.md Phase 7 gate; SKILL.md workflow step 7 + quick map).
  A read-only grid needs **neither the C++ engine** (cell/sort/filter extraction
  runs through the managed `CollectorEnv : IVwEnv`, no `RootBox`) **nor live
  verification** (real `RecordClerk` narrowing is provable headlessly). Two-layer
  harness: surface-workflow drivers in an Avalonia-headless assembly + real-clerk
  layer in `xWorksTests`. Gotchas: never put `[AvaloniaTestApplication]` in
  `xWorksTests` (~1400 tests share the host); the restored test base holds the
  undoable task open (no nested `NonUndoableUnitOfWorkHelper`); `OnChangeFilter`
  takes an (added, removed) delta that `RecordList` composes into its `AndFilter`.
- Skill files changed: `references/architecture-patterns.md` (§13),
  `references/parity-evidence.md` (§2a, §3), `references/migration-checklist.md`
  (Phase 7), `SKILL.md` (quick map + workflow step 7), this ledger.

### 2026-06 — Lexical Edit (full entry view), phases 1–2 (seed entry)

- Change: `openspec/changes/lexical-edit-avalonia-migration/` (plus
  `avalonia-migration-roadmap`, `lexical-edit-avalonia-poc-spike`).
- Migrated: first Avalonia lexical-edit region — typed IR pipeline, region
  composer, owned field controls (`FwMultiWsTextField`, `FwOptionPicker`,
  menus/flyouts), plugin registry, surface selection service, seam
  contracts, Path 3 parity harness.
- Key lessons now encoded: boundary above DataTree (don't extract
  internals); owned dense controls over stock grids; explicit
  unsupported rows over silent fallback; one global undo stack; WinForms
  dialogs own all modality during coexistence; measured (not estimated)
  performance budgets; StringTable + `.resx` dual localization lanes;
  `<RootNamespace>` required for Crowdin satellite builds.
- Skill set restructured (this commit): skills moved from
  `.github/skills/` to `.claude/skills/` per AI_GOVERNANCE no-mirror rule;
  hub skill rewritten with references/ (architecture-patterns, seam-catalog,
  parity-evidence, migration-checklist, this ledger); satellite skill
  descriptions rewritten for triggering; fixed YAML-colon bug that broke
  `fieldworks-ui-wiring-review` triggering.


### 2026-06 - Browse functional remainders (browse 19f)

- Shipped the last browse-table parity items as **view events + a thin
  capability seam on the row source + product-edge routing**, reusing the
  established pattern (view raises -> LexicalBrowseHostControl re-raises ->
  RecordBrowseView owns LCModel/dialogs/commands and routes back). New
  optional seams: IBrowseRowMenuSource (data-row context menu),
  IBrowseRdeSource (Rapid-Data-Entry new-row). No new infra.
- **Reuse over reinvent paid off twice:** header drag-reorder routed through
  the *existing* ApplyConfiguredColumns (the Configure-Columns path), and
  in-cell picture editing needed *no* new editor - the editable cell already
  realizes whatever RegionFieldKind GetEditField returns, and the factory
  already builds Image. Item collapsed to "return an Image-kind field".
- **icu layering gotcha:** icu.net is referenced by **xWorks, not
  FwAvalonia**. So the diacritic/WS-collation matcher (Find/Replace P2) lives
  in ClerkBrowseRowSource.ComputeReplaced (xWorks), keeping the view
  icu-free. Diacritic-insensitive match = NFD-decompose + strip combining
  marks + an index-map to splice the replacement back into the *original*
  string (untouched diacritics survive). WS-collation = primary-strength
  Icu.Collation.Collator.Compare whole-cell equality.
- **Mediator.SendMessage is obsolete-as-error.** Broadcast commands via
  Publisher.Publish(new PublisherParameterObject(key)) instead.
- **Parallel-agent worktree sharing:** field-types (19e) and browse (19f)
  ran in the SAME worktree concurrently. It worked because the file-ownership
  split was clean and both built green together - but only edit the lines in
  shared files (tasks.md, this ledger) that are unambiguously yours, never
  touch the other agent's files, and verify the COMBINED build.
- **Section-sign / em-dash break exact-match Edits.** Anchor Edit old_string
  on ASCII-only lines (or use a Python byte-replace) when nearby code carries
  those chars; hit this several times this session.
